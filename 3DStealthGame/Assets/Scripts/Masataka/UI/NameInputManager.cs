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

	private Text _placeholder;
	private string _defaultPlaceholderText;
	private Color _defaultPlaceholderColor;

	void Start()
	{
		_placeholder = nameInputField.placeholder.GetComponent<Text>();
		if (_placeholder != null)
		{
			_defaultPlaceholderText = _placeholder.text;
			_defaultPlaceholderColor = _placeholder.color;
		}
		//Debug.Log("NameInputManager: Start開始");

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
			//Debug.Log("NameInputManager: ボタンの予約完了");
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

		if (string.IsNullOrEmpty(inputName))
		{
			if (warningText != null)
			{
				warningText.gameObject.SetActive(true);
				warningText.text = "※ユーザーネームを入力してください";
				warningText.color = Color.red;
			}
			return;
		}

		// 入力されたら警告を消す
		if (warningText != null) warningText.gameObject.SetActive(false);

		// WebSocketClientがあるときだけ名前を渡す
		if (_wsClient != null)
		{
			_wsClient.SetPlayerName(inputName);
		}

        // パネル切り替え
        if (titlePanel != null) titlePanel.SetActive(false);
        var roomSelectManager = FindObjectOfType<RoomSelectManager>();
        if (roomSelectManager != null) roomSelectManager.ShowRoomSelect();
    }
}