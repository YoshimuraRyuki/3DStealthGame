using Benjathemaker;
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
        Debug.Log("ドロップ処理到達");

        // 敵を殴ったらアイテムをドロップさせる
        var wsClient3 = FindObjectOfType<WebSocketClient>();
        if (wsClient3 != null)
        {
            int dropType = Pc.CompareTag("Player1") ? 1 : 2;
            Vector3 dropPos = transform.position + Vector3.up * 2f;

            if (wsClient3.IsHostPlayer())
            {
                if (dropType == 1) SpawnGreenItem(dropPos);
                else SpawnBlueItem(dropPos);

                wsClient3.SendStaminaItemDrop(dropType, dropPos);
            }
            else
            {
                wsClient3.SendStaminaItemDropRequest(dropType, dropPos);
            }
        }

        enemy.PlayAnimationEnemy();
        enemy.ResetPatrolState();
        enemy.reactionText.text = "×";
        Pc.isAnimationStart = false;
        isPlayerInRange = false;
        isEndAction = true;
        if (actionText != null) actionText.gameObject.SetActive(false);
        _stunSent = true; // フラグを立てる
        PlayMetrics.AddPunch();
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

        if (gameObject.layer == blueLayer && CompareTag("Player2") || gameObject.layer == greenLayer && CompareTag("Player1"))
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
        if (CompareTag("Enemy") && isPlayerInRange == false && isEndAction)
            Debug.Log($"[調査] 敵{targetEnemyID}: isEndAction残留で殴れない状態");

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
            if (StaminaManager.Instance == null)
            {
                Debug.LogWarning("[Stamina] StaminaManager が見つかりません");
                LogManager.Instance?.AddLog("スタミナ管理が見つかりません", "#ff4444");
                return;
            }

            if (!StaminaManager.Instance.UseStamina(5))
            {
                return;
            }
            isActionEnemy = true;
            Pc.isPlayerMoveStop = true;
            enemy.TextCancel();
            actionText.gameObject.SetActive(false);
            Pc.PunchEnemy();
        }

        if (CompareTag("Switch"))
        {
            if (StaminaManager.Instance == null)
            {
                Debug.LogWarning("[Stamina] StaminaManager が見つかりません");
                LogManager.Instance?.AddLog("スタミナ管理が見つかりません", "#ff4444");
                return;
            }

            if (!StaminaManager.Instance.UseStamina(5))
            {
                return;
            }

            isActionSwitch = true;
            Pc.isAction = true;
            Pc.isPlayerMoveStop = true;

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
        if (Eg.gimmickWallPosDic.ContainsKey(targetID))
        {

            foreach (Vector2Int pos in Eg.gimmickWallPosDic[targetID])
            {
                Eg.ChangeRoadColor(pos.x, pos.y);
            }
            foreach (GameObject wall in Eg.gimmickWallDic[targetID])
            {
                Destroy(wall);
            }
            Eg.gimmickWallPosDic.Remove(targetID);
        }
    }

    #endregion

    #region アイテム関連

    public Vector3 GreenItemDrop()
    {
        Vector3 basePos = transform.position + Vector3.up * 2f;
        SpawnGreenItem(basePos);
        return basePos;
    }

    public Vector3 BlueItemDrop()
    {
        Vector3 basePos = transform.position + Vector3.up * 2f;
        SpawnBlueItem(basePos);
        return basePos;
    }

    /// <summary>
    /// 緑アイテムを生成する（相手からの受信時にも使う）
    /// </summary>
    public void SpawnGreenItem(Vector3 basePos)
    {
        int count = 2;
        float scatterRadius = 1.2f; // 散らばる距離
        for (int i = 0; i < count; i++)
        {
            // 番号から方向を決める（乱数を使わないので両画面で一致する）
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector3 landPos = new Vector3(
                basePos.x + Mathf.Cos(angle) * scatterRadius,
                0.5f,
                basePos.z + Mathf.Sin(angle) * scatterRadius);

            var obj = Instantiate(Eg.powerItemGreen[0], basePos, transform.rotation);
            obj.tag = "PowerItem";

            // PowerItemの物理演出はオンラインだと同期ズレするので無効化し、
            // ItemDropEffectの決定的な放物線に置き換える
            var pi = obj.GetComponent<PowerItem>();
            if (pi != null) Destroy(pi);

            var sg = obj.GetComponent<SimpleGemsAnim>();
            if (sg != null) sg.enabled = false;

            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
            foreach (var c in obj.GetComponentsInChildren<Collider>())
                c.isTrigger = true;

            obj.AddComponent<ItemDropEffect>().Init(basePos, landPos);
        }
    }

    /// <summary>
    /// 青アイテムを生成する（相手からの受信時にも使う）
    /// </summary>
    public void SpawnBlueItem(Vector3 basePos)
    {
        int count = 2;
        float scatterRadius = 1.2f;
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector3 landPos = new Vector3(
                basePos.x + Mathf.Cos(angle) * scatterRadius,
                0.5f,
                basePos.z + Mathf.Sin(angle) * scatterRadius);

            var obj = Instantiate(Eg.powerItemBlue[0], basePos, transform.rotation);
            obj.tag = "PowerItem";

            var pi = obj.GetComponent<PowerItem>();
            if (pi != null) Destroy(pi);

            var sg = obj.GetComponent<SimpleGemsAnim>();
            if (sg != null) sg.enabled = false;

            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
            foreach (var c in obj.GetComponentsInChildren<Collider>())
                c.isTrigger = true;

            obj.AddComponent<ItemDropEffect>().Init(basePos, landPos);
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


    private void ForceCancelEnemyStun()
    {
        if (!CompareTag("Enemy")) return;

        if (!_stunSent && !isEnemyMoveStop) return;

        isEnemyMoveStop = false;
        currentStanTime = 0f;
        isActionEnemy = false;
        isEndAction = false;
        _stunSent = false;

        if (enemy != null)
        {
            enemy.StunCancel();
        }

        var wsClient = FindObjectOfType<WebSocketClient>();
        if (wsClient != null)
        {
            wsClient.SendEnemyStunCancel(targetEnemyID);
        }

        Debug.Log($"[SwitchManager] リスポーンにより敵スタンを強制解除 enemyID={targetEnemyID}");
    }

    /// <summary>
    /// リスポーン時にアクション状態をリセットする
    /// </summary>
    public void ResetActionState()
    {
        ForceCancelEnemyStun();

        isActionSwitch = false;
        isActionEnemy = false;

        // --- ここから修正：自分のリスポーン時のみ範囲内フラグをリセットする ---
        var wsClient = FindObjectOfType<WebSocketClient>();
        if (wsClient != null && wsClient.myPlayer != null)
        {
            // 現在、このスイッチに触れているコライダーをチェック
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

        if (isPlayerInRange)
        {
            if (actionText != null && !Pc.isPlayerMoveStop)
            {
                if (CompareTag("Switch"))
                {
                    actionText.gameObject.SetActive(true);
                }
                else if (CompareTag("Enemy"))
                {
                    if (enemy != null && enemy.reactionText != null)
                    {
                        actionText.gameObject.SetActive(true);
                        enemy.reactionText.gameObject.SetActive(false);
                    }
                    else
                    {
                        actionText.gameObject.SetActive(false);
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

    /// <summary>
    /// 当たり判定内に留まっている間の処理。
    /// 殴った後にプレイヤーが範囲内に立ったままだとOnTriggerEnterが再発火しないため、
    /// スタン明けにここで範囲内状態を復帰させる
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (isPlayerInRange) return; // すでに範囲内なら何もしない
        if (isEndAction) return;     // スタン中・押下済みスイッチは対象外
        OnTriggerEnter(other);       // 入り直しと同じ判定を通す
    }

    #endregion

}
