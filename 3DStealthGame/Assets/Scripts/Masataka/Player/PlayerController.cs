// 現在は挙動確認用のテスト実装。操作はWASD
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("移動速度設定")]
	public float walkSpeed = 5.0f;
	public float crouchSpeed = 2.5f;

	private Rigidbody _rb;
	private Vector2 _moveInput;
	private bool _isCrouching;
	public bool isLocalPlayer = false;

	void Awake()
	{
		_rb = GetComponent<Rigidbody>();

		// 物理演算による予期せぬ転倒を防ぐため、回転を固定
		_rb.constraints = RigidbodyConstraints.FreezeRotation;
	}

	void Update()
	{
		if (!isLocalPlayer) return;

		CaptureInput();

		if (Input.GetKeyDown(KeyCode.Space))
		{
			Attack();
		}
	}

	/// <summary>
	/// キーボード入力を取得し、移動ベクトルを正規化
	/// </summary>
	private void CaptureInput()
	{
		float x = 0;
		if (Input.GetKey(KeyCode.D)) x += 1;
		if (Input.GetKey(KeyCode.A)) x -= 1;

		float z = 0;
		if (Input.GetKey(KeyCode.W)) z += 1;
		if (Input.GetKey(KeyCode.S)) z -= 1;

		// 斜め移動で速度が速くならないよう正規化
		_moveInput = new Vector2(x, z).normalized;

		// しゃがみ状態の判定
		_isCrouching = Input.GetKey(KeyCode.LeftControl);
	}

	void FixedUpdate()
	{
		if (!isLocalPlayer) return;

		ApplyMovement();
	}

	/// <summary>
	/// 入力に基づいた移動速度と回転の適用
	/// </summary>
	private void ApplyMovement()
	{
		// 入力がない場合は、水平方向の速度を即座に停止
		if (_moveInput.sqrMagnitude < 0.01f)
		{
			_rb.velocity = new Vector3(0, _rb.velocity.y, 0);
			return;
		}

		// 状態に合わせて移動速度を切り替え
		float speed = _isCrouching ? crouchSpeed : walkSpeed;
		Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);

		// 重力(y軸)の影響を維持しつつ、水平方向の速度を設定
		Vector3 targetVelocity = moveDir * speed;
		_rb.velocity = new Vector3(targetVelocity.x, _rb.velocity.y, targetVelocity.z);

		// 移動方向にキャラクターの向きを合わせる
		if (moveDir != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation(moveDir);
		}
	}

	private void Attack() => Debug.Log("攻撃アクション実行");
}