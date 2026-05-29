using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("操作設定")]
    public bool isLocalPlayer = false;

    [Header("移動速度設定")]
    public float walkSpeed = 5.0f;
    public float crouchSpeed = 2.5f;

    [Header("足音設定")]
    public float sneakVolume = 5f;
    public float walkVolume = 15f;
    public delegate void SoundEventHandler(Vector3 position, float volume);
    public event SoundEventHandler OnMakeSound;

    private Rigidbody _rb;
    private Vector2 _moveInput;

	Animator Am;

	public bool isAction = false;

	void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
		Am = GetComponent<Animator>();
	}

    void Update()
    {
        if (!isLocalPlayer) return;
        CaptureInput();
    }

    private void CaptureInput()
    {
		float x = 0;
		float z = 0;
		if (!isAction)
		{
			
			if (Input.GetKey(KeyCode.D)) x += 1;
			if (Input.GetKey(KeyCode.A)) x -= 1;
			
			if (Input.GetKey(KeyCode.W)) z += 1;
			if (Input.GetKey(KeyCode.S)) z -= 1;
			_moveInput = new Vector2(x, z).normalized;
		}

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

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        ApplyMovement();
    }

    private void ApplyMovement()
    {
        Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);
        bool isSneaking = Input.GetKey(KeyCode.LeftShift);
        float speed = isSneaking ? crouchSpeed : walkSpeed;

        if (_rb != null)
        {
            if (moveDir.sqrMagnitude < 0.01f)
            {
                _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
                return;
            }
            _rb.velocity = new Vector3(moveDir.x * speed, _rb.velocity.y, moveDir.z * speed);
        }
        else
        {
            transform.position += moveDir * speed * Time.fixedDeltaTime;
        }

        if (moveDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDir);

        // 足音
        if (isSneaking)
            MakeSound(transform.position, sneakVolume);
        else
            MakeSound(transform.position, walkVolume);
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

	void MakeSound(Vector3 position, float volume)
    {
        OnMakeSound?.Invoke(position, volume);
    }

    public bool IsSneaking => Input.GetKey(KeyCode.LeftShift);
}