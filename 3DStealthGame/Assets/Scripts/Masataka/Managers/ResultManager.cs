using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// リザルト画面の表示とランキング送受信を管理するクラス。
/// ResultDataから取得したデータを画面に表示し、サーバーへランキングを送信する。
/// </summary>
public class ResultManager : MonoBehaviour
{
	#region インスペクター設定

	[Header("プレイヤー名")]
	public Text playerNameText;

	[Header("クリア結果")]
	public Text clearTimeText;
	public Text missionCountText;
	public Text gradeText;

	[Header("ランキング")]
	public Text rank1Text;
	public Text rank2Text;
	public Text rank3Text;

	[Header("サーバーURL")]
	public string serverBaseUrl = "http://localhost:8080";

	#endregion

	#region Unityイベント

	void Start()
	{
		WebSocketClient wsClient = FindObjectOfType<WebSocketClient>();

		if (wsClient != null)
		{
			serverBaseUrl = wsClient.GetHttpBaseUrl();
			Debug.Log($"[ResultManager] serverBaseUrl = {serverBaseUrl}");
		}
		else
		{
			Debug.LogWarning("[ResultManager] WebSocketClient が見つかりません。Inspector の serverBaseUrl を使用します。");
		}

		playerNameText.text = $"{ResultData.playerName} & {ResultData.remotePlayerName}";
		clearTimeText.text = $"{(int)ResultData.elapsedTime}秒";
		missionCountText.text = $"{ResultData.missionCount} / 3";

		string grade = CalcGrade(ResultData.missionCount, ResultData.elapsedTime);
		gradeText.text = grade;

		StartCoroutine(PostAndFetchRanking());
	}

	#endregion

	#region ランキング処理

	/// <summary>
	/// ミッション数とクリアタイムから評価を計算する。
	/// S: 3ミッション達成かつ120秒以内 / A: 3ミッション達成 / B: 2ミッション達成 / C: それ以下
	/// </summary>
	string CalcGrade(int mission, float time)
	{
		if (mission >= 3 && time <= 120f) return "S";
		if (mission >= 3) return "A";
		if (mission >= 2) return "B";
		return "C";
	}

	/// <summary>
	/// ランキングに表示・送信する名前を作る。
	/// 自分と相手の名前がある場合は「自分 & 相手」にする。
	/// </summary>
	private string GetRankingDisplayName()
	{
		string playerName = string.IsNullOrEmpty(ResultData.playerName)
			? "Player"
			: ResultData.playerName;

		string remoteName = string.IsNullOrEmpty(ResultData.remotePlayerName)
			? ""
			: ResultData.remotePlayerName;

		if (string.IsNullOrEmpty(remoteName))
		{
			return playerName;
		}

		if (playerName == remoteName)
		{
			return playerName;
		}

		return $"{playerName} & {remoteName}";
	}

	/// <summary>
	/// スコアをサーバーに送信し、ランキングを取得してUIに反映する。
	/// </summary>
	IEnumerator PostAndFetchRanking()
	{
		// ランキングに送信するデータを作成
		RankingPostRequest postData = new RankingPostRequest
		{
			name = GetRankingDisplayName(),
			clear_time = ResultData.elapsedTime,
			mission_count = ResultData.missionCount
		};

		string json = JsonUtility.ToJson(postData);

		// ランキング送信
		using (UnityWebRequest req = new UnityWebRequest($"{serverBaseUrl}/ranking", "POST"))
		{
			byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

			req.uploadHandler = new UploadHandlerRaw(bodyRaw);
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Content-Type", "application/json");
			req.SetRequestHeader("ngrok-skip-browser-warning", "true");

			yield return req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				Debug.LogWarning($"ランキング送信失敗: {req.error}");
			}
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
				{
					UpdateRankingUI(data.rankings);
				}
				else
				{
					Debug.LogWarning($"ランキングJSON解析失敗: {req.downloadHandler.text}");
					UpdateRankingUI(null);
				}
			}
			else
			{
				Debug.LogWarning($"ランキング取得失敗: {req.error}");
				UpdateRankingUI(null);
			}
		}
	}

	/// <summary>
	/// 取得したランキングデータをUIに反映する。
	/// 順位番号はUI側で表示しているため、ここでは名前・ミッション数・秒数・評価だけ表示する。
	/// </summary>
	void UpdateRankingUI(RankingItem[] rankings)
	{
		Text[] texts = { rank1Text, rank2Text, rank3Text };

		for (int i = 0; i < texts.Length; i++)
		{
			if (texts[i] == null) continue;

			if (rankings != null && i < rankings.Length)
			{
				var r = rankings[i];
				string g = CalcGrade(r.mission_count, (float)r.clear_time);

				texts[i].text =
					$"{r.name}　{r.mission_count}ミッション　{(int)r.clear_time}秒　{g}";
			}
			else
			{
				texts[i].text = "---";
			}
		}
	}

	#endregion

	#region ボタン処理

	/// <summary>
	/// もう一度プレイボタン押下時にゲームシーンへ遷移する。
	/// </summary>
	public void OnRetryButton()
	{
		SceneManager.LoadScene("MapTest");
	}

	/// <summary>
	/// タイトルボタン押下時にタイトルシーンへ遷移する。
	/// </summary>
	public void OnTitleButton()
	{
		SceneManager.LoadScene("Title");
	}

	#endregion
}

/// <summary>
/// ランキング送信用データ。
/// Goサーバー側のJSON形式に合わせる。
/// </summary>
[System.Serializable]
public class RankingPostRequest
{
	public string name;
	public float clear_time;
	public int mission_count;
}

/// <summary>
/// ランキングの1件分のデータ。
/// </summary>
[System.Serializable]
public class RankingItem
{
	public string name;
	public double clear_time;
	public int mission_count;
}

/// <summary>
/// ランキング取得時のデータ形式。
/// </summary>
[System.Serializable]
public class RankingResponse
{
	public RankingItem[] rankings;
}