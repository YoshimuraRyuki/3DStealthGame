using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Benjathemaker;

public class PowerItem : MonoBehaviour
{
    float dropForce = 5f;
    Rigidbody rb;
    BoxCollider bc;
    SimpleGemsAnim sg;

    #region Unityイベント
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        bc = GetComponent<BoxCollider>();
        sg = GetComponent<SimpleGemsAnim>();

        if (gameObject.tag == "PowerItem")
        {
            sg.enabled = false;
            rb.velocity = Vector3.zero;
            rb.useGravity = true;
            bc.isTrigger = false;
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1f), Random.Range(-1f, 1f)).normalized;

            // 瞬間的な力を加える
            rb.AddForce(randomDirection * dropForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (LayerMask.LayerToName(collision.gameObject.layer) == "Ground")
        {
            //sg.enabled = true;
            rb.useGravity = false;
            bc.isTrigger = true;
        }
    }
    #endregion

}
