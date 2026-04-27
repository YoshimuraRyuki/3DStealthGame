using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// サーバーからの初期化メッセージ用
[System.Serializable]
public class InitMessage
{
	public string type;
	public string id;
}

public class WebSocketClient : MonoBehaviour
{
	public GameObject playerPrefab; // 他人の見た目（プレハブ）
	public GameObject myPlayer;     // 自分が操作しているCube（Hierarchyからドラッグ）
	public GameObject readyPanel; // ここに「準備完了パネル」をドラッグ＆ドロップ
	public string serverUrl = "ws://192.168.56.102:8080/ws?room_id=test&name=Player1";

	private WebSocket websocket;
	private string myId;
	private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();

	void Awake()
	{
		// シーン遷移してもこのオブジェクトを破壊しない
		DontDestroyOnLoad(this.gameObject);

		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// 古い参照をすべてクリアする
		playerObjects.Clear();
		Debug.Log("シーン遷移完了：プレイヤー辞書をリセットしました");
	}
	async void Start()
	{
		websocket = new WebSocket(serverUrl);
		websocket.OnOpen += () => Debug.Log("サーバーに繋がった！");
		websocket.OnMessage += OnMessageReceived;
		websocket.OnError += (e) => Debug.Log("エラー: " + e);

		//await websocket.Connect();
	}

	public async void OnConnectButtonClicked()
	{
		// すでに古い接続が残っていたら確実に閉じて破棄する
		if (websocket != null)
		{
			try { await websocket.Close(); } catch { }
			websocket = null;
		}

		// 新しく生成して繋ぎ直す
		websocket = new WebSocket(serverUrl);
		websocket.OnOpen += () => Debug.Log("新しく繋がった！");
		websocket.OnMessage += OnMessageReceived;
		websocket.OnError += (e) => Debug.Log("エラー: " + e);

		await websocket.Connect();

		if (readyPanel != null) readyPanel.SetActive(true);
	}

	// 退出ボタンを押した時に呼ぶ関数
	public async void OnQuitButtonClicked()
	{
		if (websocket != null)
		{
			// 1. イベント購読を即座に解除（これで受信処理が止まります）
			websocket.OnMessage -= OnMessageReceived;

			// 2. 通信を閉じる
			if (websocket.State == WebSocketState.Open)
			{
				await websocket.Close();
			}
			websocket = null;
		}

		if (readyPanel != null) readyPanel.SetActive(false);
		Debug.Log("安全に退出しました");
	}

	//準備完了ボタン用の処理
	public async void OnReadyButtonClicked()
	{
		// サーバーに「Ready」を通知するJSONを送る
		string json = "{\"type\":\"ready\"}";
		await websocket.SendText(json);
		Debug.Log("準備完了をサーバーに送信しました");
	}

	private void OnMessageReceived(byte[] bytes)
	{
		string json = Encoding.UTF8.GetString(bytes);

		// 1. まずは「型」だけ先に判定する
		if (json.Contains("\"type\":\"init\""))
		{
			InitMessage init = JsonUtility.FromJson<InitMessage>(json);
			myId = init.id; // 自分のIDをここで確定させる
			Debug.Log("ID確定: " + myId);
			return;
		}

		if (json.Contains("\"type\":\"start_game\""))
		{
			SceneManager.LoadScene("TestScene");
			return;
		}

		// 2. ここまで来たら座標データのはず
		Player remotePlayer = JsonUtility.FromJson<Player>(json);

		// 自分のデータ以外（＝他人のデータ）のみ処理する
		if (!string.IsNullOrEmpty(remotePlayer.id) && remotePlayer.id != myId)
		{
			UpdateRemotePlayer(remotePlayer);
		}
	}

	void UpdateRemotePlayer(Player data)
	{
		// 現在のシーン名を取得
		string currentScene = SceneManager.GetActiveScene().name;

		// 「メインゲームのシーン」以外なら、生成処理をスキップする
		if (currentScene != "MainGameScene")
		{
			return;
		}

		if (!playerObjects.ContainsKey(data.id))
		{
			GameObject newPlayer = Instantiate(playerPrefab);
			playerObjects.Add(data.id, newPlayer);
		}

		// 存在していれば位置を更新
		if (playerObjects.ContainsKey(data.id))
		{
			playerObjects[data.id].transform.position = new Vector3(data.position_x, data.position_y, data.position_z);
		}
	}

	async void Update()
	{
		if (websocket != null && websocket.State == WebSocketState.Open)
		{
#if !UNITY_WEBGL || UNITY_EDITOR
			websocket.DispatchMessageQueue();
#endif

			// 自分の位置をサーバーに送る（myIdが確定してから）
			if (websocket != null && websocket.State == WebSocketState.Open && !string.IsNullOrEmpty(myId))
			{
				SendPosition();
			}
		}

	}

	private async void SendPosition()
	{
		Player myData = new Player();
		myData.id = myId;
		myData.position_x = myPlayer.transform.position.x;
		myData.position_y = myPlayer.transform.position.y;
		myData.position_z = myPlayer.transform.position.z;

		string json = JsonUtility.ToJson(myData);
		await websocket.SendText(json);
	}

	private async void OnApplicationQuit()
	{
		await websocket.Close();
	}
}