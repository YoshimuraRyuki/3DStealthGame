using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class RoomSelectManager : MonoBehaviour
{
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

	public string serverBaseUrl = "http://192.168.56.102:8080";

	public Button[] roomButtons;
	public Text[] roomLabels; // 各ボタンのテキスト

	public GameObject roomSelectPanel;  // ルーム選択パネル
	public GameObject readyPanel;       // 準備完了パネル
	public GameObject titlePanel;

	private WebSocketClient wsClient;
	private string selectedRoomId;

	public RoomMemberPanel roomMemberPanel;

	void Start()
	{
		wsClient = FindObjectOfType<WebSocketClient>();
		// 表示時に人数を更新
		InvokeRepeating("FetchRoomList", 0f, 3f); // 3秒ごとに更新
	}

	// 接続ボタン押下後にこのパネルを表示する想定
	public void ShowRoomSelect()
	{
		roomSelectPanel.SetActive(true);
		FetchRoomList();
	}

	private void FetchRoomList()
	{
		if (!gameObject.activeInHierarchy) return;
		StartCoroutine(FetchRoomListCoroutine());
	}

	private IEnumerator FetchRoomListCoroutine()
	{
		using (UnityWebRequest req = UnityWebRequest.Get($"{serverBaseUrl}/rooms"))
		{
			yield return req.SendWebRequest();

			if (req.result == UnityWebRequest.Result.Success)
			{
				string json = "{\"rooms\":" + req.downloadHandler.text + "}";
				RoomListWrapper wrapper = JsonUtility.FromJson<RoomListWrapper>(json);

				for (int i = 0; i < roomButtons.Length && i < wrapper.rooms.Length; i++)
				{
					RoomInfo room = wrapper.rooms[i];
					bool isFull = room.current >= room.max;

					// ラベル更新
					roomLabels[i].text = $"{room.name}: {room.current}/{room.max}人";

					roomButtons[i].interactable = !isFull;

					string roomId = room.id;
					roomButtons[i].onClick.RemoveAllListeners();
					roomButtons[i].onClick.AddListener(() => OnRoomButtonClicked(roomId));
				}
			}
			else
			{
				Debug.LogWarning("ルーム一覧取得失敗: " + req.error);
			}
		}
	}

	private void OnRoomButtonClicked(string roomId)
	{
		selectedRoomId = roomId;
		Debug.Log("ルーム選択: " + roomId);

		// WebSocketClientに選択したルームIDを渡して接続
		wsClient.ConnectToRoom(roomId);

		// ★ 自分をパネルに追加
		if (roomMemberPanel != null)
		{
			roomMemberPanel.ClearAll(); // 前のルームの残骸をリセット
			string myName = wsClient.GetPlayerName();
			roomMemberPanel.AddOrUpdateMember("self", myName, false);
		}
		/*string myName = FindObjectOfType<WebSocketClient>().GetPlayerName();
		if (roomMemberPanel != null)
			roomMemberPanel.AddOrUpdateMember("self", myName, false);*/

		// パネル切り替え
		roomSelectPanel.SetActive(false);
		readyPanel.SetActive(true);
	}

	public void OnQuitButtonClicked()
	{
		// WebSocket切断
		wsClient.OnQuitButtonClicked();

		// パネルを戻す
		readyPanel.SetActive(false);
		roomSelectPanel.SetActive(true);

		// 人数を最新に更新
		FetchRoomList();
	}

	void OnDestroy()
	{
		CancelInvoke("FetchRoomList");
	}
}