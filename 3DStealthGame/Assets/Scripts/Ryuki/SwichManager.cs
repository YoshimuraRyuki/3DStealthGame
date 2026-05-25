using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SwichManager : MonoBehaviour
{
    #region 宣言

    Renderer rd;

    private TextMeshProUGUI actionText;
    private Transform cameraTransform;
    private bool isPlayerInRange = false;

    public bool isEnemyMoveStop = false;
    bool isActionEnemy= false;
    bool isActionSwitch = false;

    [Header("スタン時間"), SerializeField]
    float stanTime = 3f;
    float currentStanTime;
    #endregion

    #region ボタン押下処理

    /// <summary>
    /// 
    /// </summary>    
    void DoActionEnemy()
    {
        Debug.Log("殴る開始");
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

    /// <summary>
    /// スイッチを押したら対応した強化敵の遮る壁を出す
    /// </summary>
    void DoActionSwitch()
    {
        rd.material.color = Color.red;
    }
    #endregion

    #region Unityイベント
    // Start is called before the first frame update
    void Start()
    {
        currentStanTime = 0f;

        rd = GetComponent<Renderer>();

        // メインカメラの向きを取得用
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        // ゲーム開始時に、シーン内から特定の名前のテキストを探す
        GameObject textObj = GameObject.Find("ActionText");

        // 子要素からTextMeshProUGUIを自動で見つける
        actionText = GetComponentInChildren<TextMeshProUGUI>(true);

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
                isActionSwitch = true;
            }
        }

        if(isActionEnemy)
        {
            DoActionEnemy();
        }
        if (isActionSwitch)
        {
            DoActionSwitch();
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
        if (other.CompareTag("Player1"))
        {
            print("プレイヤーが入った");
            isPlayerInRange = true;
            if (actionText != null)
            {
                actionText.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player1"))
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
