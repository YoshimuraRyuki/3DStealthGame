using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

/// <summary>
/// ルーム選択画面を管理する。
/// サーバーからルーム一覧を取得し、選択したルームへ接続する。
/// </summary>
public class RoomSelectManager : MonoBehaviour
{
	#region データ構造

	[System.Serializable]
	public class RoomInfo
	{
		public string id;
		public string name;
		public int current;
		public int max;
	}

	[System.Serializable]
	public class RoomListWrapper
	{
		public RoomInfo[] rooms;
	}

	#endregion

	#region インスペクター設定

	[Header("サーバー")]
	public string serverBaseUrl = "https://stealth-game-server.onrender.com";

	[Header("ルーム一覧")]
	public Button[] roomButtons;
	public Text[] roomLabels;

	[Header("パネル")]
	public GameObject roomSelectPanel;
	public GameObject readyPanel;
	public GameObject titlePanel;

	[Header("メンバー表示")]
	public RoomMemberPanel roomMemberPanel;

	[Header("初期フォーカス")]
	[SerializeField] private GameObject roomButton;
	[SerializeField] private GameObject readyPanelButton;

	[Header("準備ボタン")]
	[SerializeField] private Button readyButton;

	#endregion

	#region 内部状態

	private WebSocketClient _wsClient;
	private string _selectedRoomId;

	#endregion

	#region Unityイベント

	private void Start()
	{
		_wsClient = FindObjectOfType<WebSocketClient>();

		if (_wsClient == null)
		{
			Debug.LogWarning("WebSocketClient が見つかりません");
			return;
		}

		SetupReadyButton();
		ApplyServerUrl();

		InvokeRepeating(nameof(FetchRoomList), 0f, 3f);
	}

	private void OnDestroy()
	{
		CancelInvoke(nameof(FetchRoomList));

		if (readyButton != null)
		{
			readyButton.onClick.RemoveListener(OnReadyButtonClicked);
		}
	}

	#endregion

	#region 公開メソッド

	public void ShowRoomSelect()
	{
		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null) return;

		if (string.IsNullOrEmpty(_wsClient.GetPlayerName()))
		{
			Debug.LogWarning("名前が入力されていません");
			return;
		}

		if (roomSelectPanel != null)
		{
			roomSelectPanel.SetActive(true);
		}

		StartCoroutine(SelectRoomButton());
		FetchRoomList();
	}

	public void OnQuitButtonClicked()
	{
		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient != null)
		{
			_wsClient.OnQuitButtonClicked();
		}

		if (readyPanel != null)
		{
			readyPanel.SetActive(false);
		}

		if (roomSelectPanel != null)
		{
			roomSelectPanel.SetActive(true);
		}

		StartCoroutine(SelectRoomButton());
		FetchRoomList();
	}

	#endregion

	#region 初期化

	private void SetupReadyButton()
	{
		if (readyButton == null) return;

		readyButton.onClick.RemoveAllListeners();
		readyButton.onClick.AddListener(OnReadyButtonClicked);
	}

	private void ApplyServerUrl()
	{
		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient != null)
		{
			serverBaseUrl = _wsClient.GetHttpBaseUrl();
			Debug.Log($"[RoomSelectManager] serverBaseUrl = {serverBaseUrl}");
		}
		else
		{
			Debug.LogWarning("[RoomSelectManager] WebSocketClient が見つかりません。Inspector の serverBaseUrl を使用します。");
		}
	}

	#endregion

	#region ボタン処理

	private void OnReadyButtonClicked()
	{
		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null) return;

		_wsClient.OnReadyButtonClicked();
	}

	private void OnRoomButtonClicked(string roomId)
	{
		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null) return;
		if (string.IsNullOrEmpty(_wsClient.GetPlayerName())) return;

		_selectedRoomId = roomId;

		_wsClient.ConnectToRoom(roomId);

		if (roomMemberPanel != null)
		{
			roomMemberPanel.ClearAll();

			string myName = _wsClient.GetPlayerName();
			roomMemberPanel.AddOrUpdateMember("self", myName, false);
		}

		if (roomSelectPanel != null)
		{
			roomSelectPanel.SetActive(false);
		}

		if (readyPanel != null)
		{
			readyPanel.SetActive(true);
		}

		StartCoroutine(SelectReadyButton());
	}

	#endregion

	#region ルーム一覧取得

	private void FetchRoomList()
	{
		if (!gameObject.activeInHierarchy) return;
		if (string.IsNullOrEmpty(serverBaseUrl)) return;

		StartCoroutine(FetchRoomListCoroutine());
	}

	private IEnumerator FetchRoomListCoroutine()
	{
		using (UnityWebRequest req = UnityWebRequest.Get($"{serverBaseUrl}/rooms"))
		{
			req.SetRequestHeader("ngrok-skip-browser-warning", "true");

			yield return req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				Debug.LogWarning("ルーム一覧取得失敗: " + req.error);
				yield break;
			}

			string json = "{\"rooms\":" + req.downloadHandler.text + "}";
			RoomListWrapper wrapper = JsonUtility.FromJson<RoomListWrapper>(json);

			UpdateRoomButtons(wrapper);
		}
	}

	private void UpdateRoomButtons(RoomListWrapper wrapper)
	{
		if (roomButtons == null || roomLabels == null) return;

		int roomCount = wrapper != null && wrapper.rooms != null ? wrapper.rooms.Length : 0;

		for (int i = 0; i < roomButtons.Length; i++)
		{
			if (roomButtons[i] == null) continue;

			if (i >= roomCount || i >= roomLabels.Length || roomLabels[i] == null)
			{
				roomButtons[i].interactable = false;

				if (i < roomLabels.Length && roomLabels[i] != null)
				{
					roomLabels[i].text = "---";
				}

				continue;
			}

			RoomInfo room = wrapper.rooms[i];
			bool isFull = room.current >= room.max;

			roomLabels[i].text = $"{room.name}: {room.current}/{room.max}人";
			roomButtons[i].interactable = !isFull;

			string roomId = room.id;

			roomButtons[i].onClick.RemoveAllListeners();
			roomButtons[i].onClick.AddListener(() => OnRoomButtonClicked(roomId));
		}
	}

	#endregion

	#region フォーカス

	private IEnumerator SelectRoomButton()
	{
		yield return new WaitForEndOfFrame();

		if (EventSystem.current == null || roomButton == null) yield break;

		EventSystem.current.SetSelectedGameObject(roomButton);
	}

	private IEnumerator SelectReadyButton()
	{
		yield return new WaitForEndOfFrame();

		if (EventSystem.current == null || readyPanelButton == null) yield break;

		EventSystem.current.SetSelectedGameObject(readyPanelButton);
	}

	#endregion
}