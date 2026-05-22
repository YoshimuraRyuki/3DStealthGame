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


    void MakeSound(Vector3 position, float volume)
    {
        // 敵に音の発生源と音量を伝える
        if (OnMakeSound != null)
        {
            OnMakeSound(position, volume);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(x, 0, z);
        transform.position += move * speed * Time.deltaTime;

        // まずプレイヤーが実際に移動しているかチェック
        bool isMoving = (x != 0 || z != 0);

        if (isMoving)
        {
            // 移動中に左シフトが押されているかチェック
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // 音を消す
                print("音消してます");
            }
            else
            {
                // 通常移動時の大きな音を出す
                MakeSound(transform.position, walkVolume);
                print("音出てます");
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            // 殴る処理
        }
    }
}
