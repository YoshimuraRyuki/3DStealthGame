using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using static UnityEngine.GraphicsBuffer;

public class EnemyManager : MonoBehaviour
{
    #region 宣言

    public enum EnemyState
    {
        Patrol,
        FocusPlayer
    }
    EnemyState currentState;

    ElementGenerator Eg;
    Light Sl;

    GameObject startPoint;   // 最初のポイント
    GameObject nextPoint;    // 近くのポイント
    GameObject targetPoint;  // 今向かっているポイント
    [Header("巡回ポイント")]
    public GameObject[] movePoints;

    [Header("移動速度")]
    public float speed = 3f;
    [Header("回転速度")]
    public float rotateSpeed = 3f;
    public float stoprotateSpeed = 50f;
    [Header("停止時間 最小")]
    public float stopMoveCooldownMin = 5f;
    [Header("停止時間 最大")]
    public float stopMoveCooldownMax = 10f;
    float stopMoveCooldown;
    float currentTime = 0f;

    Transform targetPlayer; // プレイヤー検知
    [Header("検知する距離")]
    public float viewRadius = 10f;
    [Header("検知する角度")]
    public float viewAngle = 90f;

    [Header("警戒カウント")]
    public float alertCount = 6;
    public float currentAlertCount;

    bool isFoundPlayer = false;
    bool isCountStop = false;
    bool isStopMove = false; // 敵が止まっているときに動きを完全に止める
    #endregion

    #region 敵移動遷移

    /// <summary>
    /// 一番近いポイントを取得
    /// </summary>
    /// <returns></returns>
    GameObject StartPoint()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Point");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;

        Vector3 currentPos = transform.position;

        foreach (GameObject obj in objs)
        {
            float distance = Vector3.SqrMagnitude(obj.transform.position - currentPos);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = obj;
            }
        }
        return nearest;
    }

    /// <summary>
    /// ランダムで次の近くのポイントを探す
    /// </summary>
    /// <param name="currentPoint"></param>
    /// <returns></returns>
    GameObject GetNearPoint(GameObject currentPoint)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Point");

        List<GameObject> nearPoints = new List<GameObject>();

        foreach (GameObject obj in objs)
        {
            if (obj == currentPoint) continue;

            float distance = Vector3.Distance(currentPoint.transform.position, obj.transform.position);

            // 一定距離以内だけ候補にする
            if (distance <= 10f)
            {
                nearPoints.Add(obj);
            }
        }

        // 候補が無い場合
        if (nearPoints.Count == 0)
        {
            return currentPoint;
        }

        // 候補からランダム選択
        return nearPoints[Random.Range(0, nearPoints.Count)];
    }

    /// <summary>
    /// ポイントに向かって移動
    /// </summary>
    void MoveEnemy()
    {
        if (targetPoint == null) return;

        if (!isStopMove)
        {
            MoveToPoint(targetPoint); // 実際に移動させる処理
        }

        // 近づいたら切り替え
        if (Vector3.Distance(transform.position, targetPoint.transform.position) < 0.1f)
        {
            isStopMove = true;
            currentTime += Time.deltaTime; // 目的の位置に着いたら少し止める
            SearchPlayer();                // 止まっている間視点を動かす

            if (currentTime >= stopMoveCooldown)
            {
                GameObject oldPoint = targetPoint;

                // 次のポイントをランダム取得
                targetPoint = GetNearPoint(oldPoint);

                // 停止時間を再抽選
                stopMoveCooldown = Random.Range(stopMoveCooldownMin, stopMoveCooldownMax);

                currentTime = 0;
                isStopMove = false;
            }

        }
    }

    void MoveToPoint(GameObject point)
    {
        // 目的地方向
        Vector3 direction = point.transform.position - transform.position;

        direction.y = 0;

        // 向きたい回転
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // なめらか回転
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);

        // 角度差を取得
        float angle = Quaternion.Angle(transform.rotation, lookRotation);

        // ある程度向けたら移動
        if (angle < 3f)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }
    #endregion



    #region 敵を見つける処理

    /// <summary>
    /// 敵があたりを見渡す処理
    /// </summary>
    void SearchPlayer()
    {
        if (!isStopMove)
        {
            return;
        }
        if (currentTime >= 3)
        {
            return;
        }
        // 見回り動作
        transform.Rotate(0, stoprotateSpeed * Time.deltaTime, 0); // 右回転
    }

    /// <summary>
    /// プレイヤーを発見するさせる時の処理
    /// </summary>
    void PlayerFound()
    {
        isFoundPlayer = false;
        // ターゲット方向計算
        Vector3 dirToTarget = (targetPlayer.position - transform.position).normalized;
        // 距離計算
        float dstToTarget = Vector3.Distance(transform.position, targetPlayer.position);

        // 距離が範囲内かチェック
        if (dstToTarget < viewRadius)
        {
            // 正面方向となす角度を計算（内積を利用）
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            // 角度が扇型の範囲内かチェック
            if (angleToTarget < viewAngle / 2f)
            {
                // (オプション)間に障害物がないかレイキャストで確認
                RaycastHit hit;
                if (Physics.Raycast(transform.position, dirToTarget, out hit, dstToTarget))
                {
                    // 障害物に当たった場合
                    if (hit.transform == targetPlayer)
                    {
                        Debug.Log("プレイヤー発見");
                        isFoundPlayer = true;

                        currentState = EnemyState.FocusPlayer;
                    }
                }
            }
        }
    }

    void AlertColor()
    {
        if (currentAlertCount <= 1)
        {
            Sl.color = Color.red;
        }
        else if (currentAlertCount < 3)
        {
            Sl.color = new Color(1f, 0.5f, 0f);
        }
        else
        {
            Sl.color = new Color(0.827f, 0.851f, 0.439f);
        }
    }

    // 範囲デバック用
    private void OnDrawGizmos()
    {
        // 視界範囲の色
        Gizmos.color = Color.yellow;

        // 視界距離（円）
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // 左右の視界線
        Vector3 leftBoundary =
            Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;

        Vector3 rightBoundary =
            Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        // 線を描画
        Gizmos.color = Color.blue;

        Gizmos.DrawLine(
            transform.position,
            transform.position + leftBoundary * viewRadius
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position + rightBoundary * viewRadius
        );

        // 正面方向
        Gizmos.color = Color.green;

        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.forward * viewRadius
        );

        // ターゲットへの線
        if (targetPlayer != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                transform.position,
                targetPlayer.position
            );
        }
    }

    #endregion

    #region 音でプレイヤーの方向に向ける

    void FocusPlayer()
    {
        Vector3 direction = targetPlayer.position - transform.position;

        // 高さ無視
        direction.y = 0;

        // 向きたい回転
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // なめらか回転
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);

        if (!isFoundPlayer)
        {
            currentState = EnemyState.Patrol;
        }
    }

    #endregion

    #region Unityイベント
    // Start is called before the first frame update
    void Start()
    {
        currentState = EnemyState.Patrol;

        currentAlertCount = alertCount;

        Eg = GetComponent<ElementGenerator>();
        Sl = GetComponentInChildren<Light>();

        // 一番近いポイント（＝スタート地点）
        startPoint = StartPoint();

        // 最初の目的地をランダムに決定
        targetPoint = GetNearPoint(startPoint);

        // 最初の停止時間をランダム化
        stopMoveCooldown = Random.Range(stopMoveCooldownMin, stopMoveCooldownMax);

        // 見つけるプレイヤー取得
        targetPlayer = GameObject.FindWithTag("playerTest").transform; // 結合する際にこの処理を消して将貴のプレイヤープレファブを入れる
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                MoveEnemy();   // 敵が移動する処理
                PlayerFound(); // プレイヤーを見つける処理
                break;
            case EnemyState.FocusPlayer:
                FocusPlayer();
                PlayerFound();
                break;
        }


        if (isFoundPlayer)
        {
            currentAlertCount -= Time.deltaTime;
        }
        else
        {
            currentAlertCount += Time.deltaTime;
        }

        currentAlertCount = Mathf.Clamp(currentAlertCount, 0, alertCount);

        AlertColor();
    }
    #endregion
}
