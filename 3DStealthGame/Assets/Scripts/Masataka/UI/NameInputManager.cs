using UnityEngine;
using UnityEngine.UI;

public class NameInputManager : MonoBehaviour
{
	[Header("UI")]
	public InputField nameInputField;
	public Button confirmButton;
	public GameObject titlePanel;
	public GameObject roomSelectPanel;
	public Text warningText;

	private WebSocketClient _wsClient;

	void Start()
	{
		Debug.Log("★NameInputManager: Start開始");

		// 1. WebSocketClientを探す（見つからなくても次に進むようにガード）
		_wsClient = FindObjectOfType<WebSocketClient>();
		if (_wsClient == null)
		{
			Debug.LogError("⚠️WebSocketClientがヒエラルキーにありません！");
		}

		// 2. ボタンの紐付け（ここが止まるとボタンが反応しなくなる）
		if (confirmButton != null)
		{
			confirmButton.onClick.RemoveAllListeners();
			confirmButton.onClick.AddListener(OnConfirmName);
			Debug.Log("★NameInputManager: ボタンの予約完了");
		}
		else
		{
			Debug.LogError("⚠️ConfirmButtonの枠が空っぽです！");
		}

		if (warningText != null) warningText.gameObject.SetActive(false);
	}

	private void OnConfirmName()
	{
		string inputName = nameInputField.text.Trim();
		Debug.Log("★ボタン押された！ 入力された名前: " + inputName);

		if (string.IsNullOrEmpty(inputName))
		{
			if (warningText != null) warningText.gameObject.SetActive(true);
			return;
		}

		// WebSocketClientがあるときだけ名前を渡す
		if (_wsClient != null)
		{
			_wsClient.SetPlayerName(inputName);
		}

		// パネル切り替え
		if (titlePanel != null) titlePanel.SetActive(false);
		if (roomSelectPanel != null) roomSelectPanel.SetActive(true);
	}
}