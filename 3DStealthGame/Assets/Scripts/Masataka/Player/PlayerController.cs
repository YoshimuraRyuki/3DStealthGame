using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("移動速度")]
	public float walkSpeed = 5.0f;
	public float crouchSpeed = 2.5f;

	void Update()
	{
		//  移動処理（現在、左Shiftキーでしゃがみ歩き）
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

		if (Input.GetMouseButtonDown(0))
		{
			Attack();
		}
	}

	void Attack()
	{
		//あとで
	}
}