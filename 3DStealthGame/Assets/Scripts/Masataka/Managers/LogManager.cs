using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ゲーム中のログ表示を管理する。
/// 通常ログは最大4件まで表示し、待機中ログは点滅表示する。
/// </summary>
public class LogManager : MonoBehaviour
{
	public static LogManager Instance;

	#region インスペクター設定

	[Header("UI")]
	[SerializeField] private Text logText;

	[Header("1件あたりの表示時間（秒）")]
	[SerializeField] private float displayTime = 4f;

	#endregion

	#region 内部状態

	private const int MaxLogs = 4;

	private readonly Queue<string> _logs = new Queue<string>();
	private Coroutine _waitingLogCoroutine;
	private string _waitingLog = "";

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
	}

	#endregion

	#region 公開メソッド

	public void AddLog(string message, string color = "#c0a0ff")
	{
		string line = $"<color={color}>{message}</color>";

		_logs.Enqueue(line);

		if (_logs.Count > MaxLogs)
		{
			_logs.Dequeue();
		}

		RefreshUI();
	}

	public void AddWaitingLog(string message, string color = "#aadd44")
	{
		StopWaitingLog();

		_waitingLogCoroutine = StartCoroutine(WaitingLogCoroutine(message, color));
	}

	public void StopWaitingLog()
	{
		if (_waitingLogCoroutine != null)
		{
			StopCoroutine(_waitingLogCoroutine);
			_waitingLogCoroutine = null;
		}

		_waitingLog = "";
		RefreshUI();
	}

	#endregion

	#region UI更新

	private void RefreshUI()
	{
		if (logText == null) return;

		var lines = new List<string>();

		if (!string.IsNullOrEmpty(_waitingLog))
		{
			lines.Add(_waitingLog);
		}

		int normalLogLimit = MaxLogs - lines.Count;

		var normalLogs = new List<string>(_logs);

		int startIndex = Mathf.Max(0, normalLogs.Count - normalLogLimit);

		for (int i = startIndex; i < normalLogs.Count; i++)
		{
			lines.Add(normalLogs[i]);
		}

		logText.text = string.Join("\n", lines);
	}

	private IEnumerator WaitingLogCoroutine(string message, string color)
	{
		string[] dots = { ".", "..", "..." };
		int index = 0;

		while (true)
		{
			_waitingLog = $"<color={color}>{message}{dots[index % dots.Length]}</color>";
			RefreshUI();

			index++;
			yield return new WaitForSeconds(0.5f);
		}
	}

	#endregion
}