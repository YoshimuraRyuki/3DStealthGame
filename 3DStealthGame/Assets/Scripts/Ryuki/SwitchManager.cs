using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SwitchManager : MonoBehaviour
{
    #region コンポーネント参照

    Renderer rd;
    EnemyManager em;
    EnemyManager enemy;
    PlayerController Pc;
    ElementGenerator Eg;

    private TextMeshProUGUI actionText;
    private Transform cameraTransform;

    #endregion

    #region プレイヤー接触管理

    private bool isPlayerInRange = false;

    #endregion

    #region アクション状態管理フラグ

    bool isActionEnemy = false;
    bool isActionSwitch = false;
    bool isEndAction = false;

    public bool isPressed = false; // ミニマップアイコンんを変更するためのフラグ

    public bool isEnemyMoveStop = false;
    private bool _stunSent = false;

    #endregion

    #region スタン設定

    [Header("スタン時間"), SerializeField]
    float stanTime = 3f;
    float currentStanTime;

    #endregion

    #region ギミック連携

    public int targetEnemyID;

    #endregion

    #region 敵アクション処理

    /// <summary>
    /// 敵を殴った時の処理
    /// </summary>    
    void DoActionEnemy()
    {
        currentStanTime += Time.deltaTime;
        if (stanTime <= currentStanTime)
        {
            isEnemyMoveStop = false;
            currentStanTime = 0f;
            isActionEnemy = false;
            isEndAction = false;
         

            _stunSent = false; // リセット
            enemy.StunCancel();

            var wsClient = FindObjectOfType<WebSocketClient>();
            if (wsClient != null) wsClient.SendEnemyStunCancel(targetEnemyID);
            return;
        }

        if (!Pc.isAnimationStart) return;
        if (_stunSent) return; // 二重送信防止

        SoundManager.Instance?.PlayPunch();

        // 敵を殴ったらアイテムをドロップさせる
        if (Pc.CompareTag("Player1"))
        {
            GreenItemDrop();
        }
        else if (Pc.CompareTag("Player2"))
        {
            BlueItemDrop();
        }

        enemy.PlayAnimationEnemy();
        enemy.ResetPatrolState();
        enemy.reactionText.text = "×";
        Pc.isAnimationStart = false;
        isPlayerInRange = false;
        isEndAction = true;
        if (actionText != null) actionText.gameObject.SetActive(false);
        _stunSent = true; // フラグを立てる
        currentStanTime = 0f;

        var wsClient2 = FindObjectOfType<WebSocketClient>();
        if (wsClient2 != null) wsClient2.SendEnemyStun(targetEnemyID);
    }

    #endregion

    #region スイッチアクション処理

    /// <summary>
    /// スイッチを押したら対応した強化敵の遮る壁を出す
    /// </summary>
    void DoActionSwitch()
    {
        //if (Pc.isPlayerMoveStop) return;
        if (Pc.isAction) return;
        isEndAction = true;
        isPlayerInRange = false;
        rd.material.color = Color.green;
        isActionSwitch = false;
        isPressed = true;

        var wsClient = FindObjectOfType<WebSocketClient>();
        if (wsClient != null) wsClient.SendSwitchActivated(targetEnemyID);

        int blueLayer = LayerMask.NameToLayer("Blue");
        int greenLayer = LayerMask.NameToLayer("Green");

        if (gameObject.layer == blueLayer || gameObject.layer == greenLayer)
        {
            // 同じtargetEnemyIDを持つ相手スイッチが押されているか確認
            bool partnerPressed = false;
            foreach (var sw in FindObjectsOfType<SwitchManager>())
            {
                if (sw == this) continue;
                if (sw.targetEnemyID == targetEnemyID && sw.isPressed)
                {
                    partnerPressed = true;
                    break;
                }
            }
            // 両方押されたら鳴らす
            if (partnerPressed)
                SoundManager.Instance?.PlayGimmickClear();
        }
        else
        {
            // 通常スイッチはすぐに鳴らす
            SoundManager.Instance?.PlayGimmickClear();
        }
    }

    #endregion

    #region プレイヤー入力された時の処理

    void TryAction()
    {
        //if (!isPlayerInRange) return;
        if (!isPlayerInRange || isEndAction || isActionSwitch || isActionEnemy) return;

        int blueLayer = LayerMask.NameToLayer("Blue");
        int greenLayer = LayerMask.NameToLayer("Green");
        if ((gameObject.layer == blueLayer && Pc.CompareTag("Player1")) || (gameObject.layer == greenLayer && Pc.CompareTag("Player2")))
        {
            LogManager.Instance?.AddLog("対応する色じゃないと押せないようだ", "#ffcc44");
            return;
        }

        if (CompareTag("Enemy"))
        {
            if (enemy.reactionText.text == "!") return;

            isActionEnemy = true;
            Pc.isPlayerMoveStop = true;
            enemy.TextCancel();
            actionText.gameObject.SetActive(false);
            Pc.PunchEnemy();
        }

        if (CompareTag("Switch"))
        {
            if (!StaminaManager.Instance.CanUseStamina(2)) return; // 足りなければ押せない
            StaminaManager.Instance.UseStamina(2);
            isActionSwitch = true;
            Pc.isAction = true;
            Pc.isPlayerMoveStop = true;
            // 1回だけ実行
            if (em != null)
            {
                em.SwitchCountValue(1);
                em.PlayAnimationWall();
                OpenGimmickWall(targetEnemyID);
            }
            actionText.gameObject.SetActive(false);
            Pc.PunchSwitch();
        }
    }

    #endregion

    #region ギミック連携用関数

    public void SetTarget(EnemyManager enemy)
    {
        em = enemy;
    }

    /// <summary>
    /// スイッチ起動時に呼ばれる処理。対応するIDの透明壁を消去する
    /// </summary>
    public void OpenGimmickWall(int targetID)
    {
        // 指定されたIDの壁が存在するかチェック
        if (Eg.gimmickWallDic.ContainsKey(targetID))
        {
            foreach (GameObject wall in Eg.gimmickWallDic[targetID])
            {
                // オブジェクトごと破壊する場合
                Destroy(wall);
            }
            Eg.gimmickWallDic.Remove(targetID);
        }
    }

    #endregion

    #region アイテム関連

    public void GreenItemDrop()
    {
        float heightOffset = 2f;
        float spacing = 0.5f;

        for (int i = 0; i < 2; i++)
        {
            Vector3 spawnPosition = transform.position + new Vector3(i * spacing, heightOffset, 0);
            var obj = Instantiate(Eg.powerItemGreen[0], spawnPosition, transform.rotation);
            Eg.activeGreenItems.Add(obj);
            obj.tag = "PowerItem";
        }
    }
    
    public void BlueItemDrop()
    {
        float heightOffset = 2f;
        float spacing = 0.5f;

        for (int i = 0; i < 2; i++)
        {
            Vector3 spawnPosition = transform.position + new Vector3(i * spacing, heightOffset, 0);
            var obj = Instantiate(Eg.powerItemBlue[0], spawnPosition, transform.rotation);
            Eg.activeBlueItems.Add(obj);
            obj.tag = "PowerItem";
        }
    }

    #endregion

    #region 澤田作：サーバ関連

    /// <summary>
    /// 受信用
    /// </summary>
    public void OnSwitchActivated()
    {
        isEndAction = true;
        isPlayerInRange = false;
        if (em != null) em.PlayAnimationWall();
        OpenGimmickWall(targetEnemyID);
        rd.material.color = Color.green;
        isPressed = true;
    }

    /// <summary>
    /// リスポーン時にアクション状態をリセットする
    /// </summary>
    public void ResetActionState()
    {
        isActionSwitch = false;
        isActionEnemy = false;
        var wsClient = FindObjectOfType<WebSocketClient>();
        if (wsClient != null && wsClient.myPlayer != null)
        {
            // もし自分のプレイヤーが範囲内に残っているなら、isPlayerInRange は true のままにする
            Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.localScale / 2);
            bool myPlayerStillInside = false;

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject == wsClient.myPlayer)
                {
                    myPlayerStillInside = true;
                    break;
                }
            }

            // 自分が範囲内にいない場合のみ false にする
            if (!myPlayerStillInside)
            {
                isPlayerInRange = false;
                if (actionText != null) actionText.gameObject.SetActive(false);
            }
        }
        else
        {
            // ネットワークが繋がっていない（シングルテストなど）の場合は一応リセット
            isPlayerInRange = false;
            if (actionText != null) actionText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Unityイベント

    void Start()
    {
        Invoke("DelayedStart", 0.5f);
    }

    void DelayedStart()
    {
        var p = GameObject.FindWithTag("Player1") ?? GameObject.FindWithTag("Player2");

        Pc = p.GetComponent<PlayerController>();
        rd = GetComponent<Renderer>();
        enemy = GetComponent<EnemyManager>();
        Eg = GameObject.Find("StageMake").GetComponent<ElementGenerator>();
        Pc.OnPunchInput += TryAction;

        currentStanTime = 0f;
        // メインカメラの向きを取得用
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Transform child = transform.Find("Model/ActionCanvas/ActionText");
        if (child != null)
        {
            actionText = child.GetComponent<TextMeshProUGUI>();
        }

        if (actionText != null)
        {
            actionText.gameObject.SetActive(false); // 最初は非表示
        }
    }

    void Update()
    {
        if (isActionEnemy)
        {
            DoActionEnemy();  // 敵を殴った時
        }
        if (isActionSwitch)
        {
            DoActionSwitch(); // スイッチを殴った時
        }

        // テキストが表示されている間、常にカメラの方を向かせる
        if (isPlayerInRange && actionText != null && cameraTransform != null)
        {
            // テキストの親（Canvasなど）をカメラに向ける
            actionText.transform.rotation = Quaternion.LookRotation(actionText.transform.position - cameraTransform.position);
        }

        if(isPlayerInRange)
        {
            if (actionText != null && !Pc.isPlayerMoveStop)
            {
                if (CompareTag("Switch"))
                {
                    actionText.gameObject.SetActive(true);
                }
                else if (CompareTag("Enemy"))
                {
                    if (enemy != null && enemy.reactionText != null && enemy.reactionText.text == "!")
                    {
                        actionText.gameObject.SetActive(false);
                    }
                    else
                    {
                        actionText.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    #endregion

    #region 当たり判定処理

    private void OnTriggerEnter(Collider other)
    {
        if (isEndAction) return;
        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
        {
            var wsClient = FindObjectOfType<WebSocketClient>();
            if (wsClient == null) return;
            if (other.gameObject != wsClient.myPlayer) return;

            var pc = other.GetComponent<PlayerController>();
            if (pc != null) Pc = pc;

            //int blueLayer = LayerMask.NameToLayer("Blue");
            //int greenLayer = LayerMask.NameToLayer("Green");
            //if (gameObject.layer == greenLayer && !wsClient.IsHostPlayer()) return;
            //if (gameObject.layer == blueLayer && !wsClient.IsGuestPlayer()) return;

            isPlayerInRange = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
        {
            isPlayerInRange = false;
            if (actionText != null)
            {
                actionText.gameObject.SetActive(false);
            }
        }
    }

    #endregion

}
