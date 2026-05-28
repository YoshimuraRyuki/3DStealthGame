using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    float speed = 3f;

    // プレイヤー足音
    public float sneakVolume = 5f;
    public float walkVolume = 15f;
    public delegate void SoundEventHandler(Vector3 position, float volume);
    public event SoundEventHandler OnMakeSound;

    Animator Am;

    public bool isAction = false;

    void MakeSound(Vector3 position, float volume)
    {
        // 敵に音の発生源と音量を伝える
        if (OnMakeSound != null)
        {
            OnMakeSound(position, volume);
        }
    }

    public void PunchEnemy()
    {
        if (isAction) return;
        isAction = true;
        Am.SetTrigger("PunchEnemy");
    }

    public void PunchSwitch()
    {
        //if (isAction) return;
        print("スイッチアニメーション起動");

        // 移動系を止める
        Am.SetBool("Run", false);
        Am.SetBool("Sneak", false);

        Am.SetTrigger("PunchSwitch");
    }

    public void EndAction()
    {
        isAction = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        Am = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = 0;
        float z = 0;

        // アクション中は移動禁止
        if (!isAction)
        {
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");

            Vector3 move = new Vector3(x, 0, z);
            transform.position += move * speed * Time.deltaTime;
        }

        // まずプレイヤーが実際に移動しているかチェック
        bool isMoving = (x != 0 || z != 0);

        // Sneak
        if (isMoving && Input.GetKey(KeyCode.LeftShift))
        {
            Am.SetBool("Sneak", true);
            Am.SetBool("Run", false);
        }
        // Run
        else if (isMoving)
        {
            MakeSound(transform.position, walkVolume);

            Am.SetBool("Run", true);
            Am.SetBool("Sneak", false);
        }
        // Idle
        else
        {
            Am.SetBool("Run", false);
            Am.SetBool("Sneak", false);
        }
    }
}
