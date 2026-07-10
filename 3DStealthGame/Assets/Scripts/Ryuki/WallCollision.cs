using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        LogManager.Instance?.AddLog("スイッチを作動すれば開きそうだ", "#ff4444");
    }
}
