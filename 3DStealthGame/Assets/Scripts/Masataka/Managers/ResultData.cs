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

	public static void Reset()
	{
		playerName = "";
		remotePlayerName = "";
		elapsedTime = 0f;
		missionCount = 0;
	}
}