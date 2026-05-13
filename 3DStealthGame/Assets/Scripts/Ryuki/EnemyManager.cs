using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    #region 宣言

    ElementGenerator Eg;

    GameObject startPoint;   // 最初のポイント
    GameObject nextPoint;    // 近くのポイント
    GameObject targetPoint;  // 今向かっているポイント
    [Header("巡回ポイント")]
    public GameObject[] movePoints;
    
    [Header("移動速度")]
    public float speed = 3f;
    [Header("停止時間")]
    public float stopMoveCooldown = 5f;
    float currentTime = 0f;

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
            currentTime += Time.deltaTime; // 目的の位置に着いたら少し止める
            isStopMove = true;
            
            if (currentTime >= stopMoveCooldown)
            {
                GameObject oldPoint = targetPoint;

                // 次のポイントをランダム取得
                targetPoint = GetNearPoint(oldPoint);

                currentTime = 0;
                isStopMove = false;
            }

        }
    }

    void MoveToPoint(GameObject point)
    {
        Vector3 direction = (point.transform.position - transform.position).normalized;

        transform.position += direction * speed * Time.deltaTime;
    }

    // 索敵中(敵が左右に方向転換する)

    // 次のポイントに向かって移動

    #endregion

    #region 敵を見つける処理


    #endregion

    #region Unityイベント
    // Start is called before the first frame update
    void Start()
    {
        Eg = GetComponent<ElementGenerator>();

        // 一番近いポイント（＝スタート地点）
        startPoint = StartPoint();

        // 最初の目的地をランダムに決定
        targetPoint = GetNearPoint(startPoint);
    }

    // Update is called once per frame
    void Update()
    {
        // 敵が移動する処理
        MoveEnemy();
    }
    #endregion
}
