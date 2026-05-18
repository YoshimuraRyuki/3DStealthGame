using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ミッション管理。
/// 現在のミッション：
///   1. アイテムを取得してクリア   → item_picked 受信でフラグON、goal受信で達成
///   2. ○○秒以内にクリア          → ゴールまでの経過時間で判定（ローカルタイマー）
///   3. 敵に見つからずにクリア     → ★竜希待ち。フラグだけ用意。enemy_found 受信で失敗にする予定
///
/// 【Unityでの設定手順】
/// 1. ヒエラルキーに空のGameObjectを作り "MissionManager" とリネーム → アタッチ
/// 2. Canvas配下に以下を作る
///    ├── MissionPanel (GameObject)  ← 左上あたりに置く
///    │    └── MissionText (Text)    ← ミッション一覧テキスト
///    └── MissionClearText (Text)    ← 全達成時の演出テキスト
/// 3. WebSocketClient の start_game 受信時に MissionManager.Instance.OnGameStart() を呼ぶ
/// 4. WebSocketClient の各受信で対応メソッドを呼ぶ
/// </summary>
public class MissionManager : MonoBehaviour
{
	public static MissionManager Instance;

	[Header("UI")]
	public Text missionText;        // ミッション一覧
	public Text missionClearText;   // 全達成時の演出

	[Header("制限時間ミッションの秒数")]
	public int timeLimitSeconds = 180;     // デフォルト3分

	// ─── ミッションの内部状態 ───
	private bool _hasPickedItem = false;
	private bool _mission1Done = false;
	private bool _mission2Done = false;
	private bool _mission3Done = false;
	private bool _mission3Failed = false;
	private bool _isGoalReached = false;
	private bool _isTimeUp = false;

	// ─── ローカルタイマー ───
	private bool _timerRunning = false;
	private float _elapsedSeconds = 0f;   // ゲーム開始からの経過秒数

	void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }

		if (missionClearText != null) missionClearText.gameObject.SetActive(false);
	}

	void Start()
	{
		RefreshUI();
	}

	void Update()
	{
		if (!_timerRunning) return;

		_elapsedSeconds += Time.deltaTime;

		// 制限時間を超えたらタイムアップ
		if (!_isTimeUp && _elapsedSeconds >= timeLimitSeconds)
		{
			_isTimeUp = true;
			RefreshUI();
		}
		else
		{
			// 毎フレーム更新（経過時間の表示をリアルタイムに）
			RefreshUI();
		}
	}

	// ─────────────────────────────────────────
	// 外部から呼ぶメソッド（WebSocketClient から）
	// ─────────────────────────────────────────

	/// <summary>start_game 受信時に呼ぶ → タイマーをスタート</summary>
	public void OnGameStart()
	{
		_elapsedSeconds = 0f;
		_timerRunning = true;
		_isTimeUp = false;
		Debug.Log("★MissionManager: タイマースタート");
		RefreshUI();
	}

	/// <summary>item_picked 受信時に呼ぶ</summary>
	public void OnItemPicked()
	{
		if (_hasPickedItem) return;
		_hasPickedItem = true;
		Debug.Log("★アイテム取得済み");
		RefreshUI();
	}

	/// <summary>goal 受信時に呼ぶ（自分がゴールしたとき）</summary>
	public void OnGoal()
	{
		if (_isGoalReached) return;
		_isGoalReached = true;
		_timerRunning = false;   // タイマー停止

		// ミッション1：アイテム取得 & ゴール
		_mission1Done = _hasPickedItem;

		// ミッション2：制限時間内ゴール
		_mission2Done = !_isTimeUp;

		// ミッション3：敵に見つからずゴール（竜希待ち）
		_mission3Done = !_mission3Failed;

		RefreshUI();
		CheckAllClear();
		Debug.Log($"★ゴール！ 経過: {_elapsedSeconds:F1}秒 ミッション: アイテム={_mission1Done} 時間={_mission2Done} 敵={_mission3Done}");
	}

	/// <summary>
	/// ★竜希のEnemyManagerから呼ぶ（敵に発見されたとき）
	/// EnemyManagerが完成したらこのメソッドを繋いでください
	/// </summary>
	public void OnEnemyFound()
	{
		_mission3Failed = true;
		RefreshUI();
		Debug.Log("★敵に発見された → ミッション3失敗");
	}

	// ─────────────────────────────────────────
	// スコア計算（RankingManager に渡す）
	// ─────────────────────────────────────────

	/// <summary>達成したミッション数を返す</summary>
	public int GetClearedMissionCount()
	{
		if (!_isGoalReached) return 0;
		int count = 0;
		if (_mission1Done) count++;
		if (_mission2Done) count++;
		if (_mission3Done) count++;
		return count;
	}

	/// <summary>経過秒数を返す（ランキング送信用）</summary>
	public float GetElapsedSeconds() => _elapsedSeconds;

	public bool Mission1Done => _mission1Done;
	public bool Mission2Done => _mission2Done;
	public bool Mission3Done => _mission3Done;

	// ─────────────────────────────────────────
	// UI更新
	// ─────────────────────────────────────────

	private void RefreshUI()
	{
		if (missionText == null) return;

		string item1Status = GetStatusMark(_mission1Done, _isGoalReached);
		string item3Status = _isGoalReached
			? (_mission3Done ? "✔" : "✘")
			: (_mission3Failed ? "✘" : "…");

		// ─── ミッション2：経過時間の表示 ───
		// ゴール後は結果を固定表示
		string mission2Line;
		if (_isGoalReached)
		{
			string mark = _mission2Done ? "✔" : "✘";
			int elapsed = Mathf.FloorToInt(_elapsedSeconds);
			mission2Line = $"{mark} {timeLimitSeconds}秒以内にクリア（{elapsed}秒）";
		}
		else
		{
			// ゲーム中：経過秒数をリアルタイム表示。超えたら灰色テキストで示す
			int elapsed = Mathf.FloorToInt(_elapsedSeconds);

			if (_timerRunning)
			{
				if (_isTimeUp)
				{
					// 制限時間オーバー → 灰色っぽい表現（TextはRichTextが必要なので記号で代用）
					mission2Line = $"✘ {timeLimitSeconds}秒以内にクリア（{elapsed}秒 / 超過）";
				}
				else
				{
					// 計測中
					mission2Line = $"… {timeLimitSeconds}秒以内にクリア（{elapsed}秒 / {timeLimitSeconds}秒）";
				}
			}
			else
			{
				// まだゲームが始まっていない
				mission2Line = $"… {timeLimitSeconds}秒以内にクリア";
			}
		}

		missionText.text =
			$"【ミッション】\n" +
			$"{item1Status} アイテムを取ってクリア\n" +
			$"{mission2Line}\n" +
			$"{item3Status} 敵に見つからずクリア";
	}

	private string GetStatusMark(bool done, bool decided)
	{
		if (!decided) return "…";
		return done ? "✔" : "✘";
	}

	private void CheckAllClear()
	{
		if (!(_mission1Done && _mission2Done && _mission3Done)) return;
		if (missionClearText != null)
		{
			missionClearText.gameObject.SetActive(true);
			missionClearText.text = "ミッションコンプリート！";
			Invoke(nameof(HideClearText), 3f);
		}
	}

	private void HideClearText()
	{
		if (missionClearText != null) missionClearText.gameObject.SetActive(false);
	}
}