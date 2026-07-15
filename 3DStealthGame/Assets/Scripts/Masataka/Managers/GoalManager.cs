using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 自分と相手の両方がゴール内にいる場合のみ、サーバーへゴール通知する。
/// 全員ゴールしたかどうかはサーバー側で最終判定する。
/// </summary>
public class GoalScript : MonoBehaviour
{
	#region フィールド

	private WebSocketClient _wsClient;
	private bool _hasSentGoal = false;

	private readonly HashSet<GameObject> _playersInGoal = new HashSet<GameObject>();

	#endregion

	#region Unityイベント

	private void Start()
	{
		_wsClient = FindObjectOfType<WebSocketClient>();
	}

	private void OnTriggerEnter(Collider other)
	{
		GameObject player = GetPlayerObject(other);
		if (player == null) return;

		_playersInGoal.Add(player);

		TrySendGoal();
	}

	private void OnTriggerStay(Collider other)
	{
		GameObject player = GetPlayerObject(other);
		if (player == null) return;

		_playersInGoal.Add(player);

		TrySendGoal();
	}

	private void OnTriggerExit(Collider other)
	{
		GameObject player = GetPlayerObject(other);
		if (player == null) return;

		_playersInGoal.Remove(player);
	}

	#endregion

	#region ゴール判定

	private void TrySendGoal()
	{
		if (_hasSentGoal) return;

		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null || _wsClient.myPlayer == null) return;

		if (!_playersInGoal.Contains(_wsClient.myPlayer)) return;

		bool hasPlayer1 = false;
		bool hasPlayer2 = false;

		foreach (GameObject player in _playersInGoal)
		{
			if (player == null) continue;

			if (player.CompareTag("Player1"))
			{
				hasPlayer1 = true;
			}
			else if (player.CompareTag("Player2"))
			{
				hasPlayer2 = true;
			}
		}

		if (!hasPlayer1 || !hasPlayer2) return;

		_hasSentGoal = true;

		MissionManager.Instance?.OnGoal();
		_wsClient.SendGoal();

	}

	private GameObject GetPlayerObject(Collider other)
	{
		if (other == null) return null;

		Transform current = other.transform;

		while (current != null)
		{
			if (current.CompareTag("Player1") || current.CompareTag("Player2"))
			{
				return current.gameObject;
			}

			current = current.parent;
		}

		return null;
	}

	#endregion
}