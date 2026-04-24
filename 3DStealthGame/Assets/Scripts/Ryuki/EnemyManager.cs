using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    #region 宣言

    ElementGenerator Eg;
    Transform targetPoint;          // 巡回ポイント


    bool isMove;                    // 移動
    #endregion

    #region 敵移動遷移

    // 一番近いポイントを探す
    // ポイントに向かって移動

    /// <summary>
    /// 自分から一番近いポイントを取得
    /// </summary>
    /// <returns></returns>
    GameObject NextPoint()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Point");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;

        Vector3 currentPos = transform.position; // ←敵の位置
        
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

        GameObject nearest = NextPoint();
        targetPoint = nearest.transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion
}
