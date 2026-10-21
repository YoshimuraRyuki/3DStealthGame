using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WebSocketClient
{
	#region ゲーム進行

	private void HandlePlayerGoalMessage(string json)
	{
		var msg = JsonUtility.FromJson<GoalMessage>(json);

		if (msg.id == myId)
		{
			MissionManager.Instance?.OnGoal();
		//	LogManager.Instance?.AddWaitingLog("ゴールした！相手を待っています", "#aadd44");
		}
		else
		{
		//	LogManager.Instance?.AddLog("味方がゴールした！早くゴールへ向かおう！", "#aadd44");
		}
	}


	private void HandleAllGoalMessage(string json)
	{
		SoundManager.Instance?.PlayClear();
		var msg = JsonUtility.FromJson<GoalMessage>(json);
		SoundManager.Instance?.StopBGM();
		MissionManager.Instance?.StopTimer();
		MissionManager.Instance?.ShowClearMessage();
		LogManager.Instance?.StopWaitingLog();

		if (MissionManager.Instance != null)
		{
			ResultData.elapsedTime = msg.elapsed;

			ResultData.missionCount =
				MissionManager.Instance.GetClearedMissionCount();

			ResultData.mission1Done =
				MissionManager.Instance.Mission1Done;

			ResultData.mission2Done =
				MissionManager.Instance.Mission2Done;

			ResultData.mission3Done =
				MissionManager.Instance.Mission3Done;
		}

		ResultData.playerName = playerName;
		ResultData.roomId = currentRoomId;
		ResultData.sessionId = currentSessionId;

		ResultData.deathCount = PlayMetrics.DeathCount;
		ResultData.punchCount = PlayMetrics.PunchCount;
		ResultData.chatCount = PlayMetrics.ChatCount;
		ResultData.sneakTime = PlayMetrics.SneakTime;
		ResultData.staminaItemCount = PlayMetrics.StaminaItemCount;
		foreach (var obj in playerObjects.Values)
		{
			var nameTag = obj.GetComponentInChildren<NameTag>();
			if (nameTag != null)
				ResultData.remotePlayerName = nameTag.GetName();
		}

		MissionManager.Instance?.StartCoroutine(
			MissionManager.Instance.FadeToResult()
		);
	}


	private void LoadResultScene()
	{
		var msg_dummy = new GoalMessage();
		if (MissionManager.Instance != null)
		{
			ResultData.elapsedTime =
				MissionManager.Instance.GetElapsedSeconds();

			ResultData.missionCount =
				MissionManager.Instance.GetClearedMissionCount();

			ResultData.mission1Done =
				MissionManager.Instance.Mission1Done;

			ResultData.mission2Done =
				MissionManager.Instance.Mission2Done;

			ResultData.mission3Done =
				MissionManager.Instance.Mission3Done;
		}

		ResultData.playerName = playerName;
		ResultData.roomId = currentRoomId;

		ResultData.deathCount = PlayMetrics.DeathCount;
		ResultData.punchCount = PlayMetrics.PunchCount;
		ResultData.chatCount = PlayMetrics.ChatCount;
		ResultData.staminaItemCount = PlayMetrics.StaminaItemCount;
		foreach (var obj in playerObjects.Values)
		{
			var nameTag = obj.GetComponentInChildren<NameTag>();
			if (nameTag != null)
				ResultData.remotePlayerName = nameTag.GetName();
		}
		SceneManager.LoadScene("Result");
	}


	private void HandleTimerUpdate(string json)
	{
		var msg = JsonUtility.FromJson<TimerUpdateMessage>(json);
	}

	// Old respawn packets no longer teleport players or restore stamina.
	private void HandleRespawnMessage(string json) { }
	private void HandleRemoteRespawnMessage(string json) { }

	private void HandleEnemyFoundMessage(string json)
	{
		var msg = JsonUtility.FromJson<EnemyFoundMessage>(json);

		// 自分が送った通知なら無視
		if (msg != null && msg.sender_id == myId) return;

		MissionManager.Instance?.OnEnemyFound();
	}


	#endregion
}
