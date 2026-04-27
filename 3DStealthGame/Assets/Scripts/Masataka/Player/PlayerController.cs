using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
	[Header("Movement Settings")]
	public float walkSpeed = 5.0f;
	public float crouchSpeed = 2.5f;

	private Rigidbody _rb;
	private Vector2 _moveInput;
	private bool _isCrouching;

	void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		_rb.constraints = RigidbodyConstraints.FreezeRotation; // 全回転固定
	}

	void Update()
	{
		// エンジンの「Action」機能を使わず、生の入力を取得
		CaptureInput();

		// 攻撃などはUpdate（フレーム単位）で検知
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Attack();
		}
	}

	private void CaptureInput()
	{
		// GetAxisRawを使うとエンジンの補間が入らないため、より低レイヤーに近い挙動になる
		float x = 0;
		if (Input.GetKey(KeyCode.D)) x += 1;
		if (Input.GetKey(KeyCode.A)) x -= 1;

		float z = 0;
		if (Input.GetKey(KeyCode.W)) z += 1;
		if (Input.GetKey(KeyCode.S)) z -= 1;

		_moveInput = new Vector2(x, z).normalized;
		_isCrouching = Input.GetKey(KeyCode.LeftControl);
	}

	void FixedUpdate()
	{
		ApplyMovement();
	}

	private void ApplyMovement()
	{
		// 入力がない時は水平速度のみリセット
		if (_moveInput.sqrMagnitude < 0.01f)
		{
			_rb.velocity = new Vector3(0, _rb.velocity.y, 0);
			return;
		}

		float speed = _isCrouching ? crouchSpeed : walkSpeed;
		Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);

		// 物理演算（速度ベクトル）を直接計算
		Vector3 targetVelocity = moveDir * speed;
		_rb.velocity = new Vector3(targetVelocity.x, _rb.velocity.y, targetVelocity.z);

		// クォータニオン（回転）も自前で計算して適用
		if (moveDir != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation(moveDir);
		}
	}

	private void Attack() => Debug.Log("Attack!");
}