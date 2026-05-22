// 現在は挙動確認用のテスト実装。操作はWASD
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("移動速度設定")]
	public float walkSpeed = 5.0f;
	public float crouchSpeed = 2.5f;

	[Header("攻撃設定")]
	public float attackRange = 1.5f;      // 攻撃範囲
	public float attackCooldown = 1.0f;   // 攻撃クールダウン
	public float stunDuration = 3.0f;     // スタン時間

	private Rigidbody _rb;
	private Vector2 _moveInput;
	private bool _isCrouching;
	private float _lastAttackTime = -999f;
	public bool isLocalPlayer = false;

	void Awake()
	{
		_rb = GetComponent<Rigidbody>();
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

	private void CaptureInput()
	{
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
		if (!isLocalPlayer) return;
		ApplyMovement();
	}

	private void ApplyMovement()
	{
		if (_moveInput.sqrMagnitude < 0.01f)
		{
			_rb.velocity = new Vector3(0, _rb.velocity.y, 0);
			return;
		}
		float speed = _isCrouching ? crouchSpeed : walkSpeed;
		Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);
		Vector3 targetVelocity = moveDir * speed;
		_rb.velocity = new Vector3(targetVelocity.x, _rb.velocity.y, targetVelocity.z);
		if (moveDir != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation(moveDir);
		}
	}

	private void Attack()
	{
		Debug.Log("Attack()呼ばれた");
		// クールダウンチェック
		if (Time.time - _lastAttackTime < attackCooldown) return;
		_lastAttackTime = Time.time;

		// 前方のOverlapSphereで敵を検索
		Vector3 attackOrigin = transform.position + transform.forward * (attackRange * 0.5f);
		Collider[] hits = Physics.OverlapSphere(attackOrigin, attackRange * 0.5f);

		foreach (var hit in hits)
		{
			if (hit.gameObject == gameObject) continue;
			var enemy = hit.GetComponent<EnemyManager>();
			if (enemy != null)
			{
				//enemy.Stun(stunDuration);
				Debug.Log($"敵をスタンさせました: {stunDuration}秒");
			}
		}
	}

	/// <summary>警戒度計算用。スニーク中はtrue</summary>
	public bool IsSneaking => _isCrouching;

	// 攻撃範囲のデバッグ表示
	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Vector3 attackOrigin = transform.position + transform.forward * (attackRange * 0.5f);
		Gizmos.DrawWireSphere(attackOrigin, attackRange * 0.5f);
	}
}