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

	[Header("Laravel ダッシュボード")]
	[SerializeField] private bool sendToLaravel = true;

	// Laravelを仮想環境で動かすならこれ
	[SerializeField] private string laravelBaseUrl = "http://192.168.0.200:8000";

	#endregion

	#region Unityイベント

	void Start()
	{

		PlayMetrics.LogCurrent();
		Debug.Log(
		$"[ResultData] session={ResultData.sessionId}, " +
		$"player={ResultData.playerName}, " +
		$"room={ResultData.roomId}, " +
		$"time={ResultData.elapsedTime:F2}, " +
		$"missions={ResultData.missionCount}, " +
		$"m1={ResultData.mission1Done}, " +
		$"m2={ResultData.mission2Done}, " +
		$"m3={ResultData.mission3Done}, " +
		$"death={ResultData.deathCount}, " +
		$"punch={ResultData.punchCount}, " +
		$"chat={ResultData.chatCount}, " +
		$"stamina={ResultData.staminaItemCount}, " +
		$"sneak={ResultData.sneakTime:F2}"
	);
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

		bool isHost = wsClient != null && wsClient.IsHostPlayer();

		if (isHost)
		{
			StartCoroutine(PostAndFetchRanking());
		}
		else
		{
			StartCoroutine(FetchRankingOnlyDelayed());
		}

		if (sendToLaravel)
		{
			StartCoroutine(PostPlayLogToLaravel());
		}


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

	IEnumerator FetchRankingOnly()
	{
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

	IEnumerator FetchRankingOnlyDelayed()
	{
		yield return new WaitForSeconds(0.8f);
		yield return StartCoroutine(FetchRankingOnly());
	}

	IEnumerator PostPlayLogToLaravel()
	{
		var payload = new PlayLogPayload
		{
			session_id = string.IsNullOrEmpty(ResultData.sessionId)
				? System.Guid.NewGuid().ToString()
				: ResultData.sessionId,

			name = ResultData.playerName,
			clear_time = ResultData.elapsedTime,
			mission_count = ResultData.missionCount,

			mission1_done = ResultData.mission1Done,
			mission2_done = ResultData.mission2Done,
			mission3_done = ResultData.mission3Done,

			death_count = ResultData.deathCount,
			punch_count = ResultData.punchCount,
			chat_count = ResultData.chatCount,
			stamina_item_count = ResultData.staminaItemCount,
			sneak_time = ResultData.sneakTime,

			room_id = string.IsNullOrEmpty(ResultData.roomId)
				? "unknown"
				: ResultData.roomId
		};

		string json = JsonUtility.ToJson(payload);

		using (UnityWebRequest req = new UnityWebRequest($"{laravelBaseUrl}/api/plays", "POST"))
		{
			req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Content-Type", "application/json");

			yield return req.SendWebRequest();

			if (req.result == UnityWebRequest.Result.Success)
			{
				Debug.Log("[Laravel] プレイログ送信成功: " + req.downloadHandler.text);
			}
			else
			{
				Debug.LogWarning("[Laravel] プレイログ送信失敗: " + req.responseCode + " / " + req.error + "\n" + req.downloadHandler.text);
			}
		}
	}

	IEnumerator PostPlayResult()
	{
		if (string.IsNullOrEmpty(ResultData.sessionId))
		{
			Debug.LogWarning("[PlayResult] session_id が空のため送信を中止しました");
			yield break;
		}

		var data = new PlayResultRequest
		{
			session_id = ResultData.sessionId,
			name = ResultData.playerName,
			clear_time = ResultData.elapsedTime,
			mission_count = ResultData.missionCount,

			mission1_done = ResultData.mission1Done,
			mission2_done = ResultData.mission2Done,
			mission3_done = ResultData.mission3Done,

			death_count = ResultData.deathCount,
			punch_count = ResultData.punchCount,
			chat_count = ResultData.chatCount,
			stamina_item_count = ResultData.staminaItemCount,
			sneak_time = ResultData.sneakTime,

			room_id = ResultData.roomId
		};

		string json = JsonUtility.ToJson(data);

		Debug.Log($"[PlayResult送信] {json}");

		using (UnityWebRequest req =
			new UnityWebRequest($"{serverBaseUrl}/play-result", "POST"))
		{
			byte[] bodyRaw =
				System.Text.Encoding.UTF8.GetBytes(json);

			req.uploadHandler =
				new UploadHandlerRaw(bodyRaw);

			req.downloadHandler =
				new DownloadHandlerBuffer();

			req.SetRequestHeader(
				"Content-Type",
				"application/json"
			);

			req.SetRequestHeader(
				"ngrok-skip-browser-warning",
				"true"
			);

			yield return req.SendWebRequest();

			if (req.result == UnityWebRequest.Result.Success)
			{
				Debug.Log(
					$"[PlayResult送信成功] {req.downloadHandler.text}"
				);
			}
			else
			{
				Debug.LogError(
					$"[PlayResult送信失敗] {req.responseCode} {req.error}\n" +
					req.downloadHandler.text
				);
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

[System.Serializable]
public class PlayResultRequest
{
	public string session_id;
	public string name;
	public float clear_time;
	public int mission_count;

	public bool mission1_done;
	public bool mission2_done;
	public bool mission3_done;

	public int death_count;
	public int punch_count;
	public int chat_count;
	public int stamina_item_count;
	public float sneak_time;

	public string room_id;
}

[System.Serializable]
public class PlayLogPayload
{
	public string session_id;
	public string name;
	public float clear_time;
	public int mission_count;

	public bool mission1_done;
	public bool mission2_done;
	public bool mission3_done;

	public int death_count;
	public int punch_count;
	public int chat_count;
	public int stamina_item_count;
	public float sneak_time;

	public string room_id;
}