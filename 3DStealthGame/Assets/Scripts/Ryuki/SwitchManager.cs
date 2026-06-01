using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SwitchManager : MonoBehaviour
{
    #region 宣言

    Renderer rd;
    EnemyManager em;
    PlayerController Pc;

    private TextMeshProUGUI actionText;
    private Transform cameraTransform;
    private bool isPlayerInRange = false;

    public bool isEnemyMoveStop = false;
    bool isActionEnemy= false;
    bool isActionSwitch = false;
    bool isEndAction = false;

    [Header("スタン時間"), SerializeField]
    float stanTime = 3f;
    float currentStanTime;

    public int targetEnemyID;
    #endregion

    #region ボタン押下処理

    /// <summary>
    /// 敵を殴った時の処理
    /// </summary>    
    void DoActionEnemy()
    {
        // 敵の動きを止める処理
        isEnemyMoveStop = true;

        currentStanTime += Time.deltaTime;
        if (stanTime <= currentStanTime)
        {
            isEnemyMoveStop = false;
            currentStanTime = 0f;
            isActionEnemy = false;
        }

        // スタンしてるアニメーション
    }

    public void SetTarget(EnemyManager enemy)
    {
        em = enemy;
    }

    /// <summary>
    /// スイッチを押したら対応した強化敵の遮る壁を出す
    /// </summary>
    void DoActionSwitch()
    {
        if (em != null)
        {
            em.PlayAnimation();
        }
        
        if (Pc.isAction) return;
        isEndAction = true;
        isPlayerInRange = false;
        rd.material.color = Color.red;


		isActionSwitch = false;
		var wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient != null) wsClient.SendSwitchActivated(targetEnemyID);
	}

	/// <summary>
	/// 受信用
	/// </summary>
	public void OnSwitchActivated()
	{
		isEndAction = true;
		isPlayerInRange = false;
		rd.material.color = Color.red;
	}
	#endregion

	#region Unityイベント
	// Start is called before the first frame update
	void Start()
    {
		Invoke("DelayedStart", 0.5f);
	}

	void DelayedStart()
	{
		var p = GameObject.FindWithTag("Player1") ?? GameObject.FindWithTag("Player2");

		Pc = p.GetComponent<PlayerController>();
		rd = GetComponent<Renderer>();


		currentStanTime = 0f;
		// メインカメラの向きを取得用
		if (Camera.main != null)
		{
			cameraTransform = Camera.main.transform;
		}

		Transform child = transform.Find("ActionCanvas/ActionText");
		if (child != null)
		{
			actionText = child.GetComponent<TextMeshProUGUI>();
		}

		if (actionText != null)
		{
			actionText.gameObject.SetActive(false); // 最初は非表示
		}
	}
	// Update is called once per frame
	void Update()
    {
        // プレイヤーが範囲内でEキーを押した時
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (CompareTag("Enemy"))
            {
                isActionEnemy = true;
            }
            if (CompareTag("Switch"))
            {
                // 壁を生成して隠れる場所を作る
                Pc.isAction = true;
                isActionSwitch = true;
                Pc.PunchSwitch();
            }
        }

        if(isActionEnemy)
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
    }

	private void OnTriggerEnter(Collider other)
	{
		if (isEndAction) return;
		if (other.CompareTag("Player1") || other.CompareTag("Player2"))
		{
			var pc = other.GetComponent<PlayerController>();
			if (pc != null && pc.isLocalPlayer)
				Pc = pc;
			isPlayerInRange = true;
			if (actionText != null)
				actionText.gameObject.SetActive(true);
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
