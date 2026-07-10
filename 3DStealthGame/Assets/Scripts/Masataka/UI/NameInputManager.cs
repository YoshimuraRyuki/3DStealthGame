using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の名前入力を扱う。
/// 名前を確認してからルーム選択へ進める。
/// </summary>
public class NameInputManager : MonoBehaviour
{
	#region インスペクター設定

	[Header("UI")]
	public InputField nameInputField;
	public Button confirmButton;
	public GameObject titlePanel;
	public Text warningText;

	#endregion

	#region 内部状態

	private WebSocketClient _wsClient;
	private RoomSelectManager _roomSelectManager;

	#endregion

	#region Unityイベント

	private void Start()
	{
		SoundManager.Instance?.PlayBGM();

		_wsClient = FindObjectOfType<WebSocketClient>();
		_roomSelectManager = FindObjectOfType<RoomSelectManager>();

		if (confirmButton != null)
		{
			confirmButton.onClick.RemoveAllListeners();
			confirmButton.onClick.AddListener(OnConfirmName);
		}

		HideWarning();
	}

	#endregion

	#region 入力処理

	private void OnConfirmName()
	{
		if (nameInputField == null) return;

		string inputName = nameInputField.text.Trim();

		if (string.IsNullOrEmpty(inputName))
		{
			ShowWarning("※ユーザーネームを入力してください");
			return;
		}

		if (inputName.Length > 10)
		{
			ShowWarning("※10文字以内で入力してください");
			return;
		}

		HideWarning();

		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient != null)
		{
			_wsClient.SetPlayerName(inputName);
		}

		if (titlePanel != null)
		{
			titlePanel.SetActive(false);
		}

		if (_roomSelectManager == null)
		{
			_roomSelectManager = FindObjectOfType<RoomSelectManager>();
		}

		if (_roomSelectManager != null)
		{
			_roomSelectManager.ShowRoomSelect();
		}
	}

	#endregion

	#region 警告表示

	private void ShowWarning(string message)
	{
		if (warningText == null) return;

		warningText.gameObject.SetActive(true);
		warningText.text = message;
		warningText.color = Color.red;
	}

	private void HideWarning()
	{
		if (warningText == null) return;

		warningText.gameObject.SetActive(false);
	}

	#endregion
}