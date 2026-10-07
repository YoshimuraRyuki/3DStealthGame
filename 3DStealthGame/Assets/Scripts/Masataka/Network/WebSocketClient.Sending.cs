using System.Collections;
using UnityEngine;

/// <summary>
/// WebSocketClientの送信処理。
/// メッセージ生成はSendMessageBuilder、接続状態の確認は本体に集約する。
/// </summary>
public partial class WebSocketClient
{
	public async void SendPosition()
	{
		if (myPlayer == null) return;

		var playerController = myPlayer.GetComponent<PlayerController>();
		string animationState = playerController != null ? playerController.GetAnimState() : "idle";
		string animationTrigger = playerController?.lastTrigger ?? "";

		if (!string.IsNullOrEmpty(animationTrigger) && playerController != null)
		{
			playerController.lastTrigger = "";
		}

		await SendTextIfConnectedAsync(SendMessageBuilder.PlayerMove(
			myId,
			myPlayer.transform.position,
			myPlayer.transform.rotation.eulerAngles,
			animationState,
			animationTrigger));
	}

	public async void SendRespawn(Vector3 position)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.Respawn(myId, position));
	}

	public async void SendGoal()
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.Goal());
	}

	public async void SendSwitchActivated(int switchIndex)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.SwitchActivated(switchIndex));
	}

	public async void SendEnemyStun(int enemyIndex)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.EnemyStun(enemyIndex, myId));
	}

	public async void SendEnemyStunCancel(int enemyIndex)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.EnemyStunCancel(enemyIndex, myId));
	}

	public async void SendItemPicked(Vector3 position)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.ItemPicked(position));
	}

	public async void SendStaminaItemPicked(Vector3 position)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.StaminaItemPicked(myId, position));
	}

	public async void SendStaminaItemDrop(int dropType, Vector3 position)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.StaminaItemDrop(myId, dropType, position));
	}

	public async void SendStaminaItemDropRequest(int dropType, Vector3 position)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.StaminaItemDropRequest(myId, dropType, position));
	}

	public async void SendChatMessage(string message)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.Chat(message, myId));
	}

	public async void SendEnemyFound()
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.EnemyFound(myId));
	}

	public async void SendRemoteRespawn()
	{
		if (_remoteRespawnSent || !IsSocketOpen) return;

		_remoteRespawnSent = true;
		await SendTextIfConnectedAsync(SendMessageBuilder.RemoteRespawn(myId));
		StartCoroutine(ResetRemoteRespawnFlagAfterDelay());
	}

	public async void SendStaminaState(int current, int max)
	{
		await SendTextIfConnectedAsync(SendMessageBuilder.StaminaState(myId, current, max));
	}

	private IEnumerator ResetRemoteRespawnFlagAfterDelay()
	{
		yield return new WaitForSeconds(4f);
		_remoteRespawnSent = false;
	}
}
