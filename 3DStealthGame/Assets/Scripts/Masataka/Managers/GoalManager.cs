using UnityEngine;

/// <summary>
/// 自分のプレイヤーがゴールに入ったら、サーバーへ通知する。
/// 全員ゴールしたかどうかはサーバー側で判定する。
/// </summary>
public class GoalScript : MonoBehaviour
{
	#region フィールド

	private WebSocketClient _wsClient;
	private bool _hasSentGoal = false;

	#endregion

	#region Unityイベント

	private void Start()
	{
		_wsClient = FindObjectOfType<WebSocketClient>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_hasSentGoal) return;
		if (!IsPlayer(other)) return;

		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null || other.gameObject != _wsClient.myPlayer) return;

		_hasSentGoal = true;

		MissionManager.Instance?.OnGoal();
		_wsClient.SendGoal();
	}

	#endregion

	#region 判定

	private bool IsPlayer(Collider other)
	{
		return other.CompareTag("Player1") || other.CompareTag("Player2");
	}

	#endregion
}