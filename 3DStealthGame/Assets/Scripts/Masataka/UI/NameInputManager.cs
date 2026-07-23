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

	private const int MaxNameLength = 10;

	private WebSocketClient _wsClient;
	private RoomSelectManager _roomSelectManager;

	private bool _isEditingText = false;

	#endregion

	#region Unityイベント

	private void Start()
	{
		SoundManager.Instance?.PlayBGM();

		_wsClient = FindObjectOfType<WebSocketClient>();
		_roomSelectManager = FindObjectOfType<RoomSelectManager>();

		if (nameInputField != null)
		{
			// 入力欄そのものに10文字制限を付ける
			nameInputField.characterLimit = MaxNameLength;

			// 念のため、貼り付けなどでも10文字を超えないようにする
			nameInputField.onValueChanged.RemoveListener(OnNameChanged);
			nameInputField.onValueChanged.AddListener(OnNameChanged);
		}

		if (confirmButton != null)
		{
			confirmButton.onClick.RemoveAllListeners();
			confirmButton.onClick.AddListener(OnConfirmName);
		}

		HideWarning();
	}

	private void OnDestroy()
	{
		if (nameInputField != null)
		{
			nameInputField.onValueChanged.RemoveListener(OnNameChanged);
		}
	}

	#endregion

	#region 入力処理

	private void OnNameChanged(string value)
	{
		if (_isEditingText) return;
		if (nameInputField == null) return;

		string fixedName = value.Trim();

		if (fixedName.Length > MaxNameLength)
		{
			fixedName = fixedName.Substring(0, MaxNameLength);
		}

		if (fixedName != value)
		{
			_isEditingText = true;
			nameInputField.text = fixedName;
			nameInputField.caretPosition = fixedName.Length;
			_isEditingText = false;
		}

		if (fixedName.Length <= MaxNameLength)
		{
			HideWarning();
		}
	}

	private void OnConfirmName()
	{
		if (nameInputField == null) return;

		string inputName = nameInputField.text.Trim();

		if (string.IsNullOrEmpty(inputName))
		{
			ShowWarning("※ユーザーネームを入力してください");
			return;
		}

		if (inputName.Length > MaxNameLength)
		{
			inputName = inputName.Substring(0, MaxNameLength);
			nameInputField.text = inputName;
			nameInputField.caretPosition = inputName.Length;

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