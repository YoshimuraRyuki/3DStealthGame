using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// プレイヤーのリスポーン処理を管理するクラス。
/// 敵に捕まったときにフェードアウト→チェックポイントに移動→フェードインを行う。
/// </summary>
public class RespawnManager : MonoBehaviour
{
	#region インスペクター設定

	public static RespawnManager Instance;

	[Header("UI")]
	public Text caughtText;        // 「見つかってしまいました...」テキスト
	public Image fadePanel;        // 暗転用のパネル（ImageコンポーネントのColor Aを0に）

	[Header("設定")]
	public float respawnDelay = 2f;   // リスポーンまでの秒数
	public float fadeDuration = 0.5f; // フェードの秒数

	#endregion

	#region フィールド

	private Vector3 _checkPointPosition;
	private bool _hasCheckPoint = false;
	private int _currentCheckPointIndex = -1;
	private bool _isRespawning = false;

	public int CurrentCheckPointIndex => _currentCheckPointIndex;

	#endregion

	#region Unityイベント

	void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }

		// UI初期化
		if (caughtText != null) caughtText.gameObject.SetActive(false);
		if (fadePanel != null)
		{
			var c = fadePanel.color;
			c.a = 0;
			fadePanel.color = c;
			fadePanel.gameObject.SetActive(false);
		}
	}

	#endregion

	#region 公開メソッド

	/// <summary>
	/// チェックポイントを記録する。CheckPointScriptから呼ぶ。
	/// </summary>
	public void SetCheckPoint(int index, Vector3 position)
	{
		_currentCheckPointIndex = index;
		_checkPointPosition = position;
		_hasCheckPoint = true;
	}

	/// <summary>
	/// 敵に捕まったときに呼ぶ。リスポーン処理を開始する。
	/// </summary>
	public void OnCaught()
	{
		if (_isRespawning) return;
		StartCoroutine(RespawnCoroutine());
	}

	#endregion

	#region リスポーン処理

	/// <summary>
	/// フェードアウト→移動→フェードインの一連のリスポーン処理
	/// </summary>
	private IEnumerator RespawnCoroutine()
	{
		_isRespawning = true;

		// 「見つかってしまいました...」表示
		if (caughtText != null)
		{
			caughtText.gameObject.SetActive(true);
			caughtText.text = "見つかってしまいました...";
		}

		// フェードアウト
		yield return StartCoroutine(Fade(0, 1));

		// リスポーン位置に移動
		var wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient != null && wsClient.myPlayer != null)
		{
			Vector3 respawnPos;
			if (_hasCheckPoint)
			{
				respawnPos = _checkPointPosition;
			}
			else
			{
				// チェックポイントがなければスタート地点
				respawnPos = wsClient.GetSpawnPosition();
			}
			wsClient.myPlayer.transform.position = respawnPos;
		}

		yield return new WaitForSeconds(respawnDelay);

		// フェードイン
		yield return StartCoroutine(Fade(1, 0));

		// テキストを非表示
		if (caughtText != null) caughtText.gameObject.SetActive(false);
		if (fadePanel != null) fadePanel.gameObject.SetActive(false);

		_isRespawning = false;
	}

	/// <summary>
	/// 画面のフェード処理（alpha from → to）
	/// </summary>
	private IEnumerator Fade(float from, float to)
	{
		if (fadePanel == null) yield break;

		fadePanel.gameObject.SetActive(true);
		float elapsed = 0f;

		while (elapsed < fadeDuration)
		{
			elapsed += Time.deltaTime;
			float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
			var c = fadePanel.color;
			c.a = alpha;
			fadePanel.color = c;
			yield return null;
		}

		var finalColor = fadePanel.color;
		finalColor.a = to;
		fadePanel.color = finalColor;

		if (to == 0) fadePanel.gameObject.SetActive(false);
	}

	#endregion
}