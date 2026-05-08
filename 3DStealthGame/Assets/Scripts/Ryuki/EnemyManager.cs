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

    float stopMoveCooldown = 5f;
    float currentTime = 0f;
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
    /// 一番近いポイントの次に近いポイントを取得
    /// </summary>
    /// <returns></returns>
    GameObject NextPoint(GameObject exclude)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Point");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;

        Vector3 currentPos = exclude.transform.position;

        foreach (GameObject obj in objs)
        {
            if (obj == exclude) continue;

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
    /// ポイントに向かって移動
    /// </summary>
    void MoveEnemy()
    {
        MoveToPoint(targetPoint); // 実際に移動させる処理

        // 近づいたら切り替え
        if (Vector3.Distance(transform.position, targetPoint.transform.position) < 0.5f)
        {
            currentTime += Time.deltaTime; // 目的の位置に着いたら少し止める
            if (targetPoint == startPoint && currentTime >= stopMoveCooldown)
            {
                targetPoint = nextPoint;
                currentTime = 0;
            }
            else if(targetPoint == nextPoint && currentTime >= stopMoveCooldown)
            {
                targetPoint = startPoint;
                currentTime = 0;
            }
        }
    }

    void MoveToPoint(GameObject point)
    {
        float speed = 3f;

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

        // startPoint以外で一番近いポイントを探す
        nextPoint = NextPoint(startPoint);

        // 最初は nextPoint に向かう
        targetPoint = nextPoint;
    }

    // Update is called once per frame
    void Update()
    {
        // 敵が移動する処理
        MoveEnemy();
    }
    #endregion
}
