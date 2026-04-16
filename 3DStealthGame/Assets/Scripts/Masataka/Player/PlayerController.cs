using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("移動速度")]
	public float walkSpeed = 5.0f;
	public float crouchSpeed = 2.5f;

	void Update()
	{
		// ① 移動処理（左Shiftキーでしゃがみ歩き）
		bool isCrouching = Input.GetKey(KeyCode.LeftShift);
		float currentSpeed = isCrouching ? crouchSpeed : walkSpeed;

		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		Vector3 moveDir = new Vector3(h, 0, v).normalized;

		transform.Translate(moveDir * currentSpeed * Time.deltaTime, Space.World);

		if (moveDir != Vector3.zero)
		{
			transform.forward = moveDir;
		}

		// ② 攻撃処理（左クリック）
		if (Input.GetMouseButtonDown(0))
		{
			Attack();
		}
	}

	void Attack()
	{
		// プレイヤーの正面（視界の方向）に攻撃判定などを出す
		Debug.Log("正面に攻撃！方向: " + transform.forward);
	}
}