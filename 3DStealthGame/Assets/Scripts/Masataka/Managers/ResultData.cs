/// <summary>
/// リザルト画面に渡すプレイ結果。
/// シーンをまたいで使うため、静的クラスとして保持する。
/// </summary>
public static class ResultData
{
	public static string playerName = "";
	public static string remotePlayerName = "";
	public static float elapsedTime = 0f;
	public static int missionCount = 0;
	public static float sneakTime = 0f;

	public static bool mission1Done = false;
	public static bool mission2Done = false;
	public static bool mission3Done = false;

	public static int deathCount = 0;
	public static int punchCount = 0;
	public static int chatCount = 0;
	public static int staminaItemCount = 0;

	public static string roomId = "";

	public static string sessionId = "";

	public static void Reset()
	{
		playerName = "";
		remotePlayerName = "";
		elapsedTime = 0f;
		missionCount = 0;
		sneakTime = 0f;

		mission1Done = false;
		mission2Done = false;
		mission3Done = false;

		deathCount = 0;
		punchCount = 0;
		chatCount = 0;
		staminaItemCount = 0;

		roomId = "";
		sessionId = "";
	}
}