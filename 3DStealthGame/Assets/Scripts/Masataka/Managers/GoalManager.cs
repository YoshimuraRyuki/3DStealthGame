using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 自分と相手の両方がゴール内にいる場合のみ、サーバーへゴール通知する。
/// 1人だけゴール内にいる場合は待機ログを表示する。
/// </summary>
public class GoalScript : MonoBehaviour
{
	#region フィールド

	private WebSocketClient _wsClient;
	private bool _hasSentGoal = false;
	private bool _isShowingWaitingLog = false;

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

		UpdateWaitingLog();
		TrySendGoal();
	}

	private void OnTriggerStay(Collider other)
	{
		GameObject player = GetPlayerObject(other);
		if (player == null) return;

		_playersInGoal.Add(player);

		UpdateWaitingLog();
		TrySendGoal();
	}

	private void OnTriggerExit(Collider other)
	{
		GameObject player = GetPlayerObject(other);
		if (player == null) return;

		_playersInGoal.Remove(player);

		UpdateWaitingLog();
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

		LogManager.Instance?.StopWaitingLog();

		MissionManager.Instance?.OnGoal();
		_wsClient.SendGoal();

		Debug.Log("[GoalScript] 2人がゴール内にいるため、ゴール通知を送信しました");
	}

	private void UpdateWaitingLog()
	{
		if (_hasSentGoal) return;

		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null || _wsClient.myPlayer == null) return;

		bool myPlayerInGoal = _playersInGoal.Contains(_wsClient.myPlayer);
		bool someoneInGoal = false;

		foreach (GameObject player in _playersInGoal)
		{
			if (player == null) continue;

			if (player != _wsClient.myPlayer)
			{
				someoneInGoal = true;
				break;
			}
		}

		if (myPlayerInGoal && !someoneInGoal)
		{
			if (!_isShowingWaitingLog)
			{
				LogManager.Instance?.AddWaitingLog("ゴール地点で味方を待っています", "#aadd44");
				_isShowingWaitingLog = true;
			}
		}
		else if (!myPlayerInGoal && someoneInGoal)
		{
			if (!_isShowingWaitingLog)
			{
				LogManager.Instance?.AddWaitingLog("味方がゴール地点で待っています", "#aadd44");
				_isShowingWaitingLog = true;
			}
		}
		else
		{
			if (_isShowingWaitingLog)
			{
				LogManager.Instance?.StopWaitingLog();
				_isShowingWaitingLog = false;
			}
		}
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