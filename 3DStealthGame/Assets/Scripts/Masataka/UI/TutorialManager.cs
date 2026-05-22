using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム開始直後にチュートリアル画像を表示し、OKで閉じるとゲームが始まる。
///
/// 【Unityでの設定手順】
/// 1. ヒエラルキーに空のGameObjectを作り "TutorialManager" とリネーム → スクリプトをアタッチ
/// 2. Canvas配下に以下を作る
///    └── TutorialPanel (Panel)
///         ├── TutorialImage (Image)   ← チュートリアル画像を設定
///         ├── PageText (Text)         ← "1 / 3" みたいなページ数表示（任意）
///         └── OKButton (Button)       ← 「次へ」「スタート！」ボタン
/// 3. Inspector で各フィールドをドラッグ&ドロップ
/// 4. WebSocketClient.cs の start_game 受信箇所で ShowTutorial() を呼ぶ
///    ※ WebSocketClient の修正版に「★ここで呼ぶ」コメントあり
/// </summary>
public class TutorialManager : MonoBehaviour
{
	public static TutorialManager Instance;

	[Header("UI パーツ")]
	public GameObject tutorialPanel;
	public Image tutorialImage;
	public Text pageText;
	public Button okButton;

	[Header("チュートリアル画像（複数枚可）")]
	public Sprite[] tutorialSprites;

	private int _currentPage;
	private System.Action _onFinished;

	void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }

		if (tutorialPanel != null) tutorialPanel.SetActive(false);
		if (okButton != null) okButton.onClick.AddListener(OnOKClicked);
	}

	/// <summary>
	/// WebSocketClient の start_game 受信時に呼ぶ。
	/// onFinished にゲーム本体の開始処理を渡す（不要なら null でOK）。
	/// </summary>
	public void ShowTutorial(System.Action onFinished = null)
	{
		_onFinished = onFinished;
		_currentPage = 0;

		if (tutorialSprites == null || tutorialSprites.Length == 0)
		{
			FinishTutorial();
			return;
		}

		ShowPage(0);
		if (tutorialPanel != null) tutorialPanel.SetActive(true);
		Time.timeScale = 0f; // チュートリアル中はゲームを止める
	}

	private void ShowPage(int page)
	{
		if (tutorialImage != null && page < tutorialSprites.Length)
			tutorialImage.sprite = tutorialSprites[page];

		if (pageText != null)
			pageText.text = $"{page + 1} / {tutorialSprites.Length}";

		var btnText = okButton != null ? okButton.GetComponentInChildren<Text>() : null;
		if (btnText != null)
			btnText.text = (page >= tutorialSprites.Length - 1) ? "スタート！" : "次へ";
	}

	private void OnOKClicked()
	{
		_currentPage++;
		if (_currentPage < tutorialSprites.Length)
			ShowPage(_currentPage);
		else
			FinishTutorial();
	}

	private void FinishTutorial()
	{
		if (tutorialPanel != null) tutorialPanel.SetActive(false);
		Time.timeScale = 1f;
		_onFinished?.Invoke();
		Debug.Log("★チュートリアル完了 → ゲームスタート");
	}
}