using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WebSocketClient
{
	#region ロビー受信

	private void HandleInitForLobby(string json)
	{
		InitMessage init = JsonUtility.FromJson<InitMessage>(json);
		myId = init.id;
		if (roomMemberPanel != null)
		{
			roomMemberPanel.RemoveMember("self");
			roomMemberPanel.AddOrUpdateMember(myId, playerName, false);
		}
	}


	private void HandlePlayerJoinedForLobby(string json)
	{
		var msg = JsonUtility.FromJson<PlayerJoinedMessage>(json);
		if (msg == null || msg.id == myId) return;
		if (roomMemberPanel != null) roomMemberPanel.AddOrUpdateMember(msg.id, msg.name, false);
	}


	private void HandleExistingPlayersForLobby(string json)
	{
		ExistingPlayersMessage msg = JsonUtility.FromJson<ExistingPlayersMessage>(json);
		if (msg == null || msg.players == null) return;
		foreach (var player in msg.players)
			if (player.id != myId && roomMemberPanel != null)
				roomMemberPanel.AddOrUpdateMember(player.id, player.name, player.is_ready);
	}


	private void HandlePlayerLeftForLobby(string json)
	{
		var msg = JsonUtility.FromJson<PlayerLeftMessage>(json);
		if (roomMemberPanel != null) roomMemberPanel.RemoveMember(msg.id);
	}


	private void HandlePlayerReadyForLobby(string json)
	{
		var msg = JsonUtility.FromJson<PlayerReadyMessage>(json);
		if (roomMemberPanel != null) roomMemberPanel.SetReady(msg.id, true);
	}


	private void HandlePlayerReadyMessage(string json)
	{
		var msg = JsonUtility.FromJson<PlayerReadyMessage>(json);

		if (roomMemberPanel != null)
		{
			roomMemberPanel.SetReady(msg.id, true);
		}
	}

	private void HandleStaminaStateMessage(string json)
	{
		var msg = JsonUtility.FromJson<StaminaStateMessage>(json);
		if (msg.sender_id == myId) return;

		_remoteCurrentStamina = msg.current;
		_remoteMaxStamina = msg.max;

		Debug.Log($"[RemoteStamina] current={_remoteCurrentStamina}/{_remoteMaxStamina}");
	}


	#endregion
}
