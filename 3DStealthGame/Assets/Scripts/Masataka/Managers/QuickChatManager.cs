using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 定型文チャットの送受信と表示を管理するクラス。
/// Yボタンでパネルを開閉し、ボタンを選択して相手に定型文を送る。
/// </summary>
public class QuickChatManager : MonoBehaviour
{
	public static QuickChatManager Instance;

	#region インスペクター設定

	[Header("UI")]
	public GameObject chatFrame;
	public GameObject chatButtonParent; // ChatButtonオブジェクト

	[Header("定型文リスト（ボタンの順番通りに）")]
	public string[] messages = new string[]
	{
		"ギミック解こう",
		"アイテム探そう",
		"敵がいる",
		"静かに行こう",
		"ありがとう",
		"ちょっとまって",
		"了解！",
		"ゴールに向かおう"
	};

	#endregion

	#region 内部状態

	private Button[] _buttons;
	private WebSocketClient _wsClient;
	private bool _isOpen = false;

	#endregion

	#region Unityイベント

	void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }
	}

	void Start()
	{
		_wsClient = FindObjectOfType<WebSocketClient>();

		// ChatButtonの子からボタンを全取得
		_buttons = chatButtonParent.GetComponentsInChildren<Button>();

		// 各ボタンに定型文を割り当て
		for (int i = 0; i < _buttons.Length && i < messages.Length; i++)
		{
			int index = i;
			_buttons[i].GetComponentInChildren<Text>().text = messages[i];
			_buttons[i].onClick.AddListener(() => OnChatButtonClicked(index));
		}

		// 最初は閉じておく
		if (chatFrame != null) chatFrame.SetActive(false);
	}

	void Update()
	{
		bool gamepadY = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
		bool keyboardY = Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame;

		if (gamepadY || keyboardY)
		{
			Debug.Log("チャット欄呼ばれた");
			ToggleChat();
		}
	}

	#endregion

	#region 公開メソッド

	/// <summary>
	/// チャットメッセージを受信したときに呼ぶ（WebSocketClientから）
	/// </summary>
	public void OnChatReceived(string message)
	{
		LogManager.Instance?.AddLog($"味方：{message}", "#c0a0ff");
	}

	#endregion

	#region 内部処理

	private void ToggleChat()
	{
		_isOpen = !_isOpen;
		if (chatFrame != null) chatFrame.SetActive(_isOpen);
	}

	/// <summary>
	/// ボタンが押されたとき定型文を送信してパネルを閉じる
	/// </summary>
	private void OnChatButtonClicked(int index)
	{
		if (_wsClient == null) return;
		_wsClient.SendChatMessage(messages[index]);
		LogManager.Instance?.AddLog($"自分：{messages[index]}", "#ffffff");
		ToggleChat();
	}

	#endregion
}