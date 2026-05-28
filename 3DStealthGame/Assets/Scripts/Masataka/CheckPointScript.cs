using UnityEngine;

public class CheckPointScript : MonoBehaviour
{
	[Header("チェックポイント番号（小さい順に更新）")]
	public int checkPointIndex = 0;

	private bool _isActivated = false;

	private void OnTriggerEnter(Collider other)
	{
		if (_isActivated) return;
		if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;

		var wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient == null) return;
		if (other.gameObject != wsClient.myPlayer) return;

		var respawnManager = RespawnManager.Instance;
		if (respawnManager == null) return;

		// 番号が大きい場合だけ更新（逆戻り無効）
		if (checkPointIndex > respawnManager.CurrentCheckPointIndex)
		{
			respawnManager.SetCheckPoint(checkPointIndex, transform.position);
			_isActivated = true;
			Debug.Log($"チェックポイント {checkPointIndex} 記録");
		}
	}
}