using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// ミッション進捗とゲーム中のタイマーを管理する。
/// アイテム取得、ゴール、敵に見つかったかどうかを記録してUIに反映する。
/// </summary>
public class MissionManager : MonoBehaviour
{
	public static MissionManager Instance;

	#region インスペクター設定

	[Header("UI")]
	public Text missionText;
	public Text ClearText;

	[Header("制限時間")]
	public int timeLimitSeconds = 180;

	[Header("フェード")]
	public Image fadePanel;

	#endregion

	#region 内部状態

	private bool _hasPickedItem = false;
	private bool _mission1Done = false;
	private bool _mission2Done = false;
	private bool _mission3Done = false;
	private bool _mission3Failed = false;
	private bool _isGoalReached = false;
	private bool _isTimeUp = false;

	private bool _timerRunning = false;
	private bool _hasGameStarted = false;
	private bool _isTransitioningToResult = false;

	private float _elapsedSeconds = 0f;

	#endregion

	#region Unityイベント

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;

		if (fadePanel != null)
		{
			fadePanel.gameObject.SetActive(false);
		}

		if (ClearText != null)
		{
			ClearText.gameObject.SetActive(false);
		}
	}

	private void Start()
	{
		RefreshUI();
	}

	private void Update()
	{
		if (!_timerRunning) return;

		_elapsedSeconds += Time.deltaTime;

		if (!_isTimeUp && _elapsedSeconds >= timeLimitSeconds)
		{
			_isTimeUp = true;
		}

		RefreshUI();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	#endregion

	#region 公開メソッド

	public void OnGameStart()
	{
		if (_hasGameStarted) return;

		_hasGameStarted = true;
		_elapsedSeconds = 0f;
		_timerRunning = true;
		_isTimeUp = false;

		SoundManager.Instance?.PlayBGM();
		RefreshUI();
	}

	public void OnItemPicked()
	{
		if (_hasPickedItem) return;

		_hasPickedItem = true;
		RefreshUI();
	}

	public void OnGoal()
	{
		if (_isGoalReached) return;

		_isGoalReached = true;

		_mission1Done = _hasPickedItem;
		_mission2Done = !_isTimeUp;
		_mission3Done = !_mission3Failed;

		RefreshUI();
	}

	public void OnEnemyFound()
	{
		if (_mission3Failed) return;

		_mission3Failed = true;
		_mission3Done = false;

		RefreshUI();
	}

	public int GetClearedMissionCount()
	{
		if (!_isGoalReached) return 0;

		int count = 0;

		if (_mission1Done) count++;
		if (_mission2Done) count++;
		if (_mission3Done) count++;

		return count;
	}

	public float GetElapsedSeconds()
	{
		return _elapsedSeconds;
	}

	public bool Mission1Done => _mission1Done;
	public bool Mission2Done => _mission2Done;
	public bool Mission3Done => _mission3Done;

	public void StopTimer()
	{
		_timerRunning = false;
	}

	public void ShowClearMessage()
	{
		if (ClearText == null) return;

		ClearText.gameObject.SetActive(true);
		ClearText.text = "CLEAR！";
	}

	public IEnumerator FadeToResult()
	{
		if (_isTransitioningToResult) yield break;
		_isTransitioningToResult = true;

		yield return new WaitForSeconds(2f);

		if (fadePanel != null)
		{
			fadePanel.gameObject.SetActive(true);

			float elapsed = 0f;
			float duration = 0.5f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;

				var color = fadePanel.color;
				color.a = Mathf.Lerp(0, 1, elapsed / duration);
				fadePanel.color = color;

				yield return null;
			}
		}

		SceneManager.LoadScene("Result");
	}

	#endregion

	#region UI更新

	private void RefreshUI()
	{
		if (missionText == null) return;

		int elapsed = Mathf.FloorToInt(_elapsedSeconds);

		string mission1Line = GetMission1Text();
		string mission2Line = GetMission2Text(elapsed);
		string mission3Line = GetMission3Text();

		missionText.text =
			$"・{mission1Line}\n" +
			$"・{mission2Line}\n" +
			$"・{mission3Line}";
	}

	private string GetMission1Text()
	{
		if (_hasPickedItem)
		{
			return "✔ アイテムを取ってクリア";
		}

		if (_isGoalReached)
		{
			return "✘ アイテムを取ってクリア";
		}

		return "　アイテムを取ってクリア";
	}

	private string GetMission2Text(int elapsed)
	{
		if (_isGoalReached)
		{
			if (_mission2Done)
			{
				return $"✔ {timeLimitSeconds}秒以内にクリア（{elapsed}秒）";
			}

			return $"<color=#ffffff88>✘ {timeLimitSeconds}秒以内にクリア（{elapsed}秒）</color>";
		}

		if (_timerRunning)
		{
			if (_isTimeUp)
			{
				return $"<color=#ffffff88>✘ {timeLimitSeconds}秒以内にクリア（{elapsed}秒）</color>";
			}

			return $"　{timeLimitSeconds}秒以内にクリア（{elapsed}秒）";
		}

		return $"　{timeLimitSeconds}秒以内にクリア";
	}

	private string GetMission3Text()
	{
		if (_isGoalReached)
		{
			return $"{(_mission3Done ? "✔" : "✘")} 敵に見つからずクリア";
		}

		if (_mission3Failed)
		{
			return "✘ 敵に見つからずクリア";
		}

		return "　敵に見つからずクリア";
	}

	#endregion
}