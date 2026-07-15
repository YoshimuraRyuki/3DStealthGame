using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 定型文チャットを送受信する。
/// Yキー / Yボタンで開き、選んだメッセージを相手に送る。
/// </summary>
public class QuickChatManager : MonoBehaviour
{
	public static QuickChatManager Instance;

	#region インスペクター設定

	[Header("UI")]
	[SerializeField] private GameObject chatFrame;
	[SerializeField] private GameObject chatButtonParent;

	[Header("最初に選択するボタン")]
	[SerializeField] private GameObject chatButton;

	[Header("選択中の表示")]
	[SerializeField] private GameObject chatImage;

	[Header("定型文")]
	[SerializeField]
	private string[] messages = new string[]
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

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	private void Start()
	{
		_wsClient = FindObjectOfType<WebSocketClient>();

		SetupButtons();
		CloseChat();
	}

	private void Update()
	{
		bool gamepadY = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
		bool keyboardY = Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame;

		if (gamepadY || keyboardY)
		{
			ToggleChat();
		}
	}

	#endregion

	#region 公開メソッド

	public void OnChatReceived(string message, string senderName)
	{
		SoundManager.Instance?.PlayNotification();

		string name = string.IsNullOrEmpty(senderName) ? "相手" : senderName;
		LogManager.Instance?.AddLog($"{name}：{message}", "#c0a0ff");
	}

	#endregion

	#region 初期化

	private void SetupButtons()
	{
		if (chatButtonParent == null) return;

		_buttons = chatButtonParent.GetComponentsInChildren<Button>();

		for (int i = 0; i < _buttons.Length && i < messages.Length; i++)
		{
			int index = i;

			var text = _buttons[i].GetComponentInChildren<Text>();
			if (text != null)
			{
				text.text = messages[i];
			}

			_buttons[i].onClick.AddListener(() => OnChatButtonClicked(index));
		}
	}

	#endregion

	#region チャット開閉

	private void ToggleChat()
	{
		if (_isOpen)
		{
			CloseChat();
		}
		else
		{
			OpenChat();
		}
	}

	private void OpenChat()
	{
		_isOpen = true;

		if (chatFrame != null)
		{
			chatFrame.SetActive(true);
		}

		if (chatImage != null)
		{
			chatImage.SetActive(true);
		}

		StartCoroutine(SelectChatButton());
	}

	private void CloseChat()
	{
		_isOpen = false;

		if (chatFrame != null)
		{
			chatFrame.SetActive(false);
		}

		if (chatImage != null)
		{
			chatImage.SetActive(false);
		}

		if (EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(null);
		}
	}

	private IEnumerator SelectChatButton()
	{
		yield return new WaitForEndOfFrame();

		if (!_isOpen) yield break;
		if (EventSystem.current == null || chatButton == null) yield break;

		EventSystem.current.SetSelectedGameObject(chatButton);
	}

	#endregion

	#region 送信

	private void OnChatButtonClicked(int index)
	{
		if (index < 0 || index >= messages.Length) return;

		SoundManager.Instance?.PlayButton();

		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null) return;

		string message = messages[index];

		_wsClient.SendChatMessage(message);
		PlayMetrics.AddChat();
		LogManager.Instance?.AddLog($"{_wsClient.GetPlayerName()}：{message}", "#ffffff");

		CloseChat();
	}

	#endregion
}