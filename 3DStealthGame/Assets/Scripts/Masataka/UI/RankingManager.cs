using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class RankingManager : MonoBehaviour
{
	public static RankingManager Instance;

	[Header("UI")]
	public GameObject rankingPanel;
	public Text rankingText;
	public Button returnButton;

	[Header("サーバー設定（WebSocketClient と同じIPにする）")]
	public string serverUrl = "http://192.168.56.102:8080";

	void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }

		if (rankingPanel != null) rankingPanel.SetActive(false);
		if (returnButton != null) returnButton.onClick.AddListener(OnReturnClicked);
	}

	/// <summary>
	/// ゴール時に WebSocketClient から呼ぶ。
	/// スコアをサーバーへ送信してからランキングを取得して表示する。
	/// </summary>
	public void SubmitAndShow(string playerName, float clearTime, int missionCount)
	{
		StartCoroutine(SubmitCoroutine(playerName, clearTime, missionCount));
	}

	private IEnumerator SubmitCoroutine(string playerName, float clearTime, int missionCount)
	{
		string json = JsonUtility.ToJson(new ScorePayload
		{
			name = playerName,
			clear_time = clearTime,
			mission_count = missionCount
		});

		using var postReq = new UnityEngine.Networking.UnityWebRequest(
			serverUrl + "/ranking",
			UnityEngine.Networking.UnityWebRequest.kHttpVerbPOST
		);
		byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
		postReq.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
		postReq.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
		postReq.SetRequestHeader("Content-Type", "application/json");
		yield return postReq.SendWebRequest();

		if (postReq.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
			Debug.LogWarning("ランキング送信失敗: " + postReq.error);

		yield return StartCoroutine(FetchAndShow());
	}

	private IEnumerator FetchAndShow()
	{
		using var getReq = UnityEngine.Networking.UnityWebRequest.Get(serverUrl + "/ranking");
		yield return getReq.SendWebRequest();

		if (getReq.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
		{
			Debug.LogWarning("ランキング取得失敗: " + getReq.error);
			ShowFallback();
			yield break;
		}

		var wrapper = JsonUtility.FromJson<RankingResponse>(getReq.downloadHandler.text);
		DisplayRanking(wrapper.rankings);
	}

	private void DisplayRanking(RankingEntry[] entries)
	{
		if (rankingText == null) return;

		string[] medals = { "??", "??", "??" };
		var sb = new System.Text.StringBuilder();
		sb.AppendLine(" ランキング ");

		if (entries == null || entries.Length == 0)
		{
			sb.AppendLine("（まだ記録なし）");
		}
		else
		{
			for (int i = 0; i < entries.Length; i++)
			{
				string medal = i < 3 ? medals[i] : $"{i + 1}位";
				string timeStr = FormatTime(entries[i].clear_time);
				sb.AppendLine($"{medal}  {entries[i].name}");
				sb.AppendLine($"     {timeStr}  ミッション {entries[i].mission_count}/3");
			}
		}

		rankingText.text = sb.ToString();
		if (rankingPanel != null) rankingPanel.SetActive(true);
	}

	private void ShowFallback()
	{
		if (rankingText != null) rankingText.text = "ランキング取得に失敗しました";
		if (rankingPanel != null) rankingPanel.SetActive(true);
	}

	private string FormatTime(float seconds)
	{
		int m = Mathf.FloorToInt(seconds / 60f);
		int s = Mathf.FloorToInt(seconds % 60f);
		return m > 0 ? $"{m}分{s:D2}秒" : $"{s}秒";
	}

	private void OnReturnClicked()
	{
		if (rankingPanel != null) rankingPanel.SetActive(false);
		UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
	}

	[System.Serializable]
	private class ScorePayload
	{
		public string name;
		public float clear_time;
		public int mission_count;
	}

	[System.Serializable]
	private class RankingResponse
	{
		public RankingEntry[] rankings;
	}
}

[System.Serializable]
public class RankingEntry
{
	public string name;
	public float clear_time;
	public int mission_count;
}