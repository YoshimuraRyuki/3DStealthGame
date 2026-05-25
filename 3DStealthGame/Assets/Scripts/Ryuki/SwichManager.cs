using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SwichManager : MonoBehaviour
{
    #region 宣言
    private TextMeshProUGUI actionText;
    private Transform cameraTransform;
    private bool isPlayerInRange = false;

    public static bool isEnemyMoveStop = false;

    [Header("スタン時間"), SerializeField]
    float stanTime = 3f;
    float currentStanTime;
    #endregion

    #region ボタン押下処理
    // スイッチを押した時の処理
    private void DoAction()
    {
        Debug.Log("殴る開始");

        // 自分のタグによって行動を変更

        // スイッチだったらギミック処理
        
        if (CompareTag("Switch"))
        {
            // 壁を生成して隠れる場所を作る

        }

        // 敵だったら敵がスタン
        if (CompareTag("Enemy"))
        {
            // 敵の動きを止める処理
            isEnemyMoveStop = true;

            currentStanTime += Time.deltaTime;
            if(stanTime <= currentStanTime)
            {
                isEnemyMoveStop = false;
                currentStanTime = 0f;
            }

            // スタンしてるアニメーション

        }


    }
    #endregion

    #region Unityイベント
    // Start is called before the first frame update
    void Start()
    {
        currentStanTime = 0f;

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
            DoAction();
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
        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
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
