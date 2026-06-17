using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        LogManager.Instance?.AddLog("‹­‚¢“G‚ª‚¢‚Äæ‚Éi‚ß‚È‚¢", "#ff4444");
    }
}
