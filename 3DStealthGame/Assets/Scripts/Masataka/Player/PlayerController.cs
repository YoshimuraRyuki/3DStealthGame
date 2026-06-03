using UnityEngine;

/// <summary>
/// プレイヤーの移動・アニメーション・足音を管理するクラス。
/// isLocalPlayerがtrueのときだけキー入力を受け付ける。
/// リモートプレイヤーはWebSocketClientから位置を受け取って動く。
/// </summary>
public class PlayerController : MonoBehaviour
{
	#region インスペクター設定

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

	#endregion

	#region フィールド

	private Rigidbody _rb;
	private Vector2 _moveInput;

	Animator Am;
	public bool isAnimationStart = false;

	public bool isAction = false;
	public bool isPlayerMoveStop = false; // 移動停止フラグ（スイッチ操作中など）

	public string lastTrigger = ""; // 最後に発火したアニメーショントリガー（同期用）

	#endregion

	#region Unityイベント

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

	void FixedUpdate()
	{
		if (!isLocalPlayer) return;
		ApplyMovement();
	}

	#endregion

	#region 入力処理

	/// <summary>
	/// キー入力を取得してアニメーション・足音を制御する
	/// </summary>
	private void CaptureInput()
	{
		float x = 0;
		float z = 0;

		// 移動停止中
		if (isPlayerMoveStop)
		{
			_moveInput = Vector2.zero;

			if (_rb != null)
			{
				_rb.velocity = Vector3.zero;
			}

			// アニメーション停止
			Am.SetBool("Run", false);
			Am.SetBool("Sneak", false);

			return;
		}

		// 通常入力
		if (Input.GetKey(KeyCode.D)) x += 1;
		if (Input.GetKey(KeyCode.A)) x -= 1;

		if (Input.GetKey(KeyCode.W)) z += 1;
		if (Input.GetKey(KeyCode.S)) z -= 1;

		_moveInput = new Vector2(x, z).normalized;

		bool isMoving = (x != 0 || z != 0);

		// スニーク
		if (isMoving && Input.GetKey(KeyCode.LeftShift))
		{
			Am.SetBool("Sneak", true);
			Am.SetBool("Run", false);
			Debug.Log("音消してます。");
		}
		// 走り
		else if (isMoving)
		{
			MakeSound(transform.position, walkVolume);

			Am.SetBool("Run", true);
			Am.SetBool("Sneak", false);
			//Debug.Log("音出てます");
		}
		// 待機
		else
		{
			Am.SetBool("Run", false);
			Am.SetBool("Sneak", false);
		}
	}

	#endregion

	#region 移動処理

	/// <summary>
	/// Rigidbodyを使って実際の移動・回転・足音発生を行う
	/// </summary>
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

	#endregion

	#region アクション処理

	/// <summary>
	/// 敵を殴るアニメーションを再生する
	/// </summary>
	public void PunchEnemy()
	{
		if (isAction) return;
		isAction = true;
		Am.SetTrigger("PunchEnemy");
		lastTrigger = "PunchEnemy";
	}

	public void StartAnimationEnemy()
	{
		isAnimationStart = true;
	}

	/// <summary>
	/// スイッチを操作するアニメーションを再生する
	/// </summary>
	public void PunchSwitch()
	{
		//if (isAction) return;
		print("スイッチアニメーション起動");

		// 移動系を止める
		Am.SetBool("Run", false);
		Am.SetBool("Sneak", false);

		Am.SetTrigger("PunchSwitch");
		lastTrigger = "PunchSwitch";
	}

	/// <summary>
	/// 現在のアニメーション状態を文字列で返す（同期用）
	/// </summary>
	public string GetAnimState()
	{
		if (Am.GetBool("Run")) return "run";
		if (Am.GetBool("Sneak")) return "sneak";
		return "idle";
	}

	/// <summary>
	/// アクション終了時にアニメーションイベントから呼ばれる
	/// </summary>
	public void EndAction()
	{
		isAction = false;
	}

	/// <summary>
	/// 移動停止フラグを解除する
	/// </summary>
	public void EndMove()
	{
		isPlayerMoveStop = false;
	}

	#endregion

	#region 足音処理

	void MakeSound(Vector3 position, float volume)
	{
		OnMakeSound?.Invoke(position, volume);
	}

	public bool IsSneaking => Input.GetKey(KeyCode.LeftShift);

	#endregion
}