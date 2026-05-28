using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
	public static RespawnManager Instance;

	[Header("UI")]
	public Text caughtText;        // 「みつかってしまった...」テキスト
	public Image fadePanel;        // 暗転用の黒いパネル（ImageコンポーネントのColor Aを0に）

	[Header("設定")]
	public float respawnDelay = 2f; // リスポーンまでの秒数
	public float fadeDuration = 0.5f; // フェードの秒数

	private Vector3 _checkPointPosition;
	private bool _hasCheckPoint = false;
	private int _currentCheckPointIndex = -1;
	private bool _isRespawning = false;

	public int CurrentCheckPointIndex => _currentCheckPointIndex;

	void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }

		if (caughtText != null) caughtText.gameObject.SetActive(false);
		if (fadePanel != null)
		{
			var c = fadePanel.color;
			c.a = 0;
			fadePanel.color = c;
			fadePanel.gameObject.SetActive(false);
		}
	}

	public void SetCheckPoint(int index, Vector3 position)
	{
		_currentCheckPointIndex = index;
		_checkPointPosition = position;
		_hasCheckPoint = true;
	}

	public void OnCaught()
	{
		if (_isRespawning) return;
		StartCoroutine(RespawnCoroutine());
	}

	private IEnumerator RespawnCoroutine()
	{
		_isRespawning = true;

		// 「みつかってしまった...」表示
		if (caughtText != null)
		{
			caughtText.gameObject.SetActive(true);
			caughtText.text = "みつかってしまった...";
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

		// テキスト非表示
		if (caughtText != null) caughtText.gameObject.SetActive(false);
		if (fadePanel != null) fadePanel.gameObject.SetActive(false);

		_isRespawning = false;
	}

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
}