using UnityEngine;

/// <summary>
/// 1プレイ中の行動回数を一時的に保持する。
/// 現段階では送信せず、動作確認用のログ出力だけを行う。
/// </summary>
public static class PlayMetrics
{
	public static int DeathCount { get; private set; }
	public static int PunchCount { get; private set; }
	public static int ChatCount { get; private set; }
	public static int StaminaItemCount { get; private set; }

	public static void Reset()
	{
		DeathCount = 0;
		PunchCount = 0;
		ChatCount = 0;
		StaminaItemCount = 0;

		Debug.Log("[PlayMetrics] 計測値をリセットしました");
	}

	public static void AddDeath()
	{
		DeathCount++;
		Debug.Log($"[PlayMetrics] death_count = {DeathCount}");
	}

	public static void AddPunch()
	{
		PunchCount++;
		Debug.Log($"[PlayMetrics] punch_count = {PunchCount}");
	}

	public static void AddChat()
	{
		ChatCount++;
		Debug.Log($"[PlayMetrics] chat_count = {ChatCount}");
	}

	public static void AddStaminaItem()
	{
		StaminaItemCount++;
		Debug.Log($"[PlayMetrics] stamina_item_count = {StaminaItemCount}");
	}

	public static void LogCurrent()
	{
		Debug.Log(
			$"[PlayMetrics] death={DeathCount}, " +
			$"punch={PunchCount}, " +
			$"chat={ChatCount}, " +
			$"stamina={StaminaItemCount}"
		);
	}
}