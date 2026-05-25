using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
	[Header("プレイヤー情報")]
	public Text playerNameText;

	[Header("ステータス")]
	public Text clearTimeText;
	public Text missionCountText;
	public Text gradeText;

	[Header("ランキング")]
	public Text rank1Text;
	public Text rank2Text;
	public Text rank3Text;

	public string serverBaseUrl = "http://localhost:8080";

	void Start()
	{
		playerNameText.text = ResultData.playerName;
		clearTimeText.text = $"{(int)ResultData.elapsedTime}秒";
		missionCountText.text = $"{ResultData.missionCount} / 3";

		string grade = CalcGrade(ResultData.missionCount, ResultData.elapsedTime);
		gradeText.text = grade;

		StartCoroutine(PostAndFetchRanking());
	}

	string CalcGrade(int mission, float time)
	{
		if (mission >= 3 && time <= 120f) return "S";
		if (mission >= 3) return "A";
		if (mission >= 2) return "B";
		return "C";
	}

	IEnumerator PostAndFetchRanking()
	{
		// ランキングに送信
		string json = $"{{\"name\":\"{ResultData.playerName}\",\"clear_time\":{ResultData.elapsedTime},\"mission_count\":{ResultData.missionCount}}}";

		using (UnityWebRequest req = new UnityWebRequest($"{serverBaseUrl}/ranking", "POST"))
		{
			req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Content-Type", "application/json");
			req.SetRequestHeader("ngrok-skip-browser-warning", "true");
			yield return req.SendWebRequest();
		}

		// ランキング取得
		using (UnityWebRequest req = UnityWebRequest.Get($"{serverBaseUrl}/ranking"))
		{
			req.SetRequestHeader("ngrok-skip-browser-warning", "true");
			yield return req.SendWebRequest();

			if (req.result == UnityWebRequest.Result.Success)
			{
				var data = JsonUtility.FromJson<RankingResponse>(req.downloadHandler.text);
				if (data != null && data.rankings != null)
					UpdateRankingUI(data.rankings);
			}
		}
	}

	void UpdateRankingUI(RankingItem[] rankings)
	{
		Text[] texts = { rank1Text, rank2Text, rank3Text };
		for (int i = 0; i < texts.Length; i++)
		{
			if (i < rankings.Length)
			{
				var r = rankings[i];
				string g = CalcGrade(r.mission_count, (float)r.clear_time);
				texts[i].text = $"{r.name}　{r.mission_count}ミッション　{(int)r.clear_time}秒　{g}";
			}
			else
			{
				texts[i].text = "---";
			}
		}
	}

	public void OnRetryButton()
	{
		SceneManager.LoadScene("MapTest");
	}

	public void OnTitleButton()
	{
		SceneManager.LoadScene("Title");
	}
}

[System.Serializable]
public class RankingItem
{
	public string name;
	public double clear_time;
	public int mission_count;
}

[System.Serializable]
public class RankingResponse
{
	public RankingItem[] rankings;
}