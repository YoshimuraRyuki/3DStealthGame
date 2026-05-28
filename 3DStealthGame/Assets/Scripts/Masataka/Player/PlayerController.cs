using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("ëÄçÏê›íË")]
    public bool isLocalPlayer = false;

    [Header("à⁄ìÆë¨ìxê›íË")]
    public float walkSpeed = 5.0f;
    public float crouchSpeed = 2.5f;

    [Header("ë´âπê›íË")]
    public float sneakVolume = 5f;
    public float walkVolume = 15f;
    public delegate void SoundEventHandler(Vector3 position, float volume);
    public event SoundEventHandler OnMakeSound;

    private Rigidbody _rb;
    private Vector2 _moveInput;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        CaptureInput();
    }

    private void CaptureInput()
    {
        float x = 0;
        if (Input.GetKey(KeyCode.D)) x += 1;
        if (Input.GetKey(KeyCode.A)) x -= 1;
        float z = 0;
        if (Input.GetKey(KeyCode.W)) z += 1;
        if (Input.GetKey(KeyCode.S)) z -= 1;
        _moveInput = new Vector2(x, z).normalized;
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

        // ë´âπ
        if (isSneaking)
            MakeSound(transform.position, sneakVolume);
        else
            MakeSound(transform.position, walkVolume);
    }

    void MakeSound(Vector3 position, float volume)
    {
        OnMakeSound?.Invoke(position, volume);
    }

    public bool IsSneaking => Input.GetKey(KeyCode.LeftShift);
}