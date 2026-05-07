// テスト用　おそらくGlobalCamera.csを使用します。
using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
	[Header("追従する対象")]
	public Transform player;

	[Header("カメラの配置設定")]
	public float height = 10.0f;    
	public float distance = 5.0f;  
	public float angle = 60.0f;    

	void LateUpdate()
	{
		if (player == null) return;

		Vector3 targetPosition = new Vector3(
			player.position.x,
			player.position.y + height,
			player.position.z - distance
		);

		transform.position = targetPosition;

		
		transform.rotation = Quaternion.Euler(angle, 0, 0);
	}
}