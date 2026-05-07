using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// --- 通信データの定義クラス ---

[System.Serializable]
public class InitMessage
{
	public string type;
	public string id;
	public string name;
	public int player_number;
	public PositionData position;
	public PositionData rotation;
}

[System.Serializable]
public class ExistingPlayersMessage
{
	public string type;
	public PlayerData[] players;
}

[System.Serializable]
public class PlayerJoinedMessage
{
	public string type;
	public string id;
	public string name;
	public int player_number;
	public PositionData position;
	public PositionData rotation;

}

[System.Serializable]
public class PlayerMoveMessage
{
	public string type;
	public string id;
	public PositionData position;
	public PositionData rotation;
}

[System.Serializable]
public class PlayerLeftMessage
{
	public string type;
	public string id;
	public string name;
}

[System.Serializable]
public class ExistingPlayersWrapper
{
	public PlayerData[] players;
}


// --- WebSocketクライアント本体 ---

public class WebSocketClient : MonoBehaviour
{
	public GameObject playerPrefab; // 他プレイヤーのモデル
	public GameObject myPlayer;     // 自分自身のプレイヤーオブジェクト
	//public GameObject readyPanel;    // 接続待機中のUIパネル
	public string serverUrl = "ws://192.168.56.102:8080/ws?room_id=test&name=Player1";

	private WebSocket websocket;
	private string myId;
	private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
	private bool isGameSceneLoaded;
	private List<string> pendingMessages = new List<string>(); // シーン遷移中に届いたメッセージの保持用


	private float sendInterval = 0.05f; // 0.05秒（秒間20回）に制限
	private float timer = 0f;

	private string playerName;

	public Material localPlayerMaterial;  // 自分用マテリアル
	public Material remotePlayerMaterial; // 他プレイヤー用マテリアル

	private Vector3 pendingSpawnPos = Vector3.zero;
	private bool hasSpawnPos = false;

	void Awake()
	{
		Application.runInBackground = true;
		Application.targetFrameRate = 60;
		// サーバーとの接続を維持するため、シーンをまたいでも破棄されないように設定
		DontDestroyOnLoad(this.gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
		isGameSceneLoaded = SceneManager.GetActiveScene().name == "TestScene";
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// ゲーム本編のシーンに切り替わったか判定
		isGameSceneLoaded = scene.name == "TestScene";

		if (scene.name != "TestScene")
		{
			// ロビーに戻る際などは、一旦リモートプレイヤーの情報をリセット
			ClearRemotePlayers();
		}
		else
		{
			// シーン読み込み完了後、少し遅らせて保留メッセージを処理（生成タイミングの調整）
			Invoke("ProcessPendingMessages", 0.5f);
		}
		Debug.Log("シーン変更: " + scene.name);
	}

	async void Start()
	{
		
		// 重複を避けるため簡易的なランダム名を設定
		playerName = "Player_" + Random.Range(1000, 9999);

		websocket = new WebSocket($"ws://192.168.56.102:8080/ws?room_id=test&name={playerName}");
		websocket.OnOpen += () => Debug.Log("サーバーに接続");
		websocket.OnMessage += OnMessageReceived;
		websocket.OnError += (e) => Debug.Log("エラー: " + e);
	}

	/*
	// UIの接続ボタンから呼び出し
	public async void OnConnectButtonClicked()
	{
		if (websocket != null)
		{
			try { await websocket.Close(); } catch { }
			websocket = null;
		}

		websocket = new WebSocket($"ws://192.168.56.102:8080/ws?room_id=test&name={playerName}");
		websocket.OnOpen += OnWebSocketOpened;
		websocket.OnMessage += OnMessageReceived;
		websocket.OnError += (e) => Debug.Log("エラー: " + e);

		await websocket.Connect();
	}*/

	public async void ConnectToRoom(string roomId)
	{

		if (websocket != null)
		{
			try { await websocket.Close(); } catch { }
			websocket = null;
		}

		websocket = new WebSocket($"ws://192.168.56.102:8080/ws?room_id={roomId}&name={playerName}");
		websocket.OnOpen += OnWebSocketOpened;
		websocket.OnMessage += OnMessageReceived;
		websocket.OnError += (e) => Debug.Log("エラー: " + e);

		await websocket.Connect();
	}

	// UIの切断ボタンから呼び出し
	public async void OnQuitButtonClicked()
	{
		if (websocket != null)
		{
			websocket.OnMessage -= OnMessageReceived;
			websocket.OnOpen -= OnWebSocketOpened;
			if (websocket.State == WebSocketState.Open)
			{
				await websocket.Close();
			}
			websocket = null;
		}

		//if (readyPanel != null) readyPanel.SetActive(false);
		Debug.Log("接続を切断しました");
	}

	private void OnWebSocketOpened()
	{
		Debug.Log("接続成功");
		//if (readyPanel != null) readyPanel.SetActive(true);
	}

	// 準備完了メッセージを送信（ゲーム開始の合図）
	public async void OnReadyButtonClicked()
	{
		Vector3 spawnPos = GameObject.Find("SpawnPoint").transform.position;
		if (websocket != null && websocket.State == WebSocketState.Open)
		{
			string json = $"{{\"type\":\"ready\",\"position\":{{\"x\":{spawnPos.x},\"y\":{spawnPos.y},\"z\":{spawnPos.z}}}}}";
			await websocket.SendText(json);
			Debug.Log("準備完了をサーバーに送信");
		}
		else
		{
			Debug.LogWarning("WebSocketが接続されていないため、準備完了を送信できません");
		}
	}

	// サーバーからデータを受信した際の窓口
	private void OnMessageReceived(byte[] bytes)
	{
		string json = Encoding.UTF8.GetString(bytes);
		//Debug.Log("受信: " + json);

		if (!isGameSceneLoaded && !json.Contains("\"type\":\"start_game\""))
		{
			pendingMessages.Add(json);
			return;
		}

		ProcessMessage(json);
	}

	// 貯めていたメッセージを順番に処理
	private void ProcessPendingMessages()
	{
		foreach (var json in pendingMessages)
		{
			ProcessMessage(json);
		}
		pendingMessages.Clear();
	}

	// 受信したJSONの"type"に応じて各ハンドラへ振り分け
	private void ProcessMessage(string json)
	{
		if (json.Contains("\"type\":\"init\"")) HandleInitMessage(json);
		else if (json.Contains("\"type\":\"existing_players\"")) HandleExistingPlayersMessage(json);
		else if (json.Contains("\"type\":\"player_joined\"")) HandlePlayerJoinedMessage(json);
		else if (json.Contains("\"type\":\"player_move\"")) HandlePlayerMoveMessage(json);
		else if (json.Contains("\"type\":\"player_left\"")) HandlePlayerLeftMessage(json);
		else if (json.Contains("\"type\":\"start_game\""))
		{
			if (SceneManager.GetActiveScene().name == "TestScene")
			{
				// すでにゲームシーンにいる場合は即処理
				ProcessPendingMessages();
			}
			else
			{
				SceneManager.LoadScene("TestScene");
			}
		}
	}

	// 自分の初期化情報を処理
	private void HandleInitMessage(string json)
	{
		if (SceneManager.GetActiveScene().name != "TestScene") return;

		InitMessage init = JsonUtility.FromJson<InitMessage>(json);
		myId = init.id;
		

		if (playerPrefab == null) return;

		// 自分のプレイヤーを生成
		myPlayer = Instantiate(playerPrefab);
		myPlayer.tag = "Player" + init.player_number;
		if (localPlayerMaterial != null)
			myPlayer.GetComponentInChildren<Renderer>().material = localPlayerMaterial;

		var controller = myPlayer.GetComponent<PlayerController>();
		if (controller != null) controller.isLocalPlayer = true;
		myPlayer.GetComponent<PlayerController>().enabled = true;
		DontDestroyOnLoad(myPlayer);
		playerObjects[myId] = myPlayer;

		// スポーン地点の同期
		/*
		if (hasSpawnPos)
		{
			myPlayer.transform.position = pendingSpawnPos;
			Debug.Log("ElementGeneratorのSpawnPointに配置しました");
		}
		else
		{
			GameObject spawnPoint = GameObject.Find("SpawnPoint");
			if (spawnPoint != null)
				myPlayer.transform.position = spawnPoint.transform.position;
			else if (init.position != null)
				myPlayer.transform.position = new Vector3(init.position.x, init.position.y, init.position.z);
		}

		*/
		GameObject spawnPoint = GameObject.Find("SpawnPoint");
		if (spawnPoint != null)
		{
			myPlayer.transform.position = spawnPoint.transform.position;
			Debug.Log("Unity側のSpawnPointに配置しました");
		}
		else if (init.position != null)
		{
			myPlayer.transform.position = new Vector3(init.position.x, init.position.y, init.position.z);
		}

		//playerObjects[myId] = myPlayer;

		if (GlobalCamera.Instance != null)
			GlobalCamera.Instance.SetTarget(myPlayer.transform);
	}

	// 既存のプレイヤーリストを処理するハンドラ
	private void HandleExistingPlayersMessage(string json)
	{
		if (SceneManager.GetActiveScene().name != "TestScene") return;
		ExistingPlayersMessage msg = JsonUtility.FromJson<ExistingPlayersMessage>(json);
		if (msg != null && msg.players != null)
		{
			foreach (var player in msg.players)
			{
				if (player.id != myId)
				{
					SpawnRemotePlayer(player);
				}
			}
		}
	}

	// 他のプレイヤーが新しく入ってきた時の処理
	private void HandlePlayerJoinedMessage(string json)
	{
		if (SceneManager.GetActiveScene().name != "TestScene") return;
		var msg = JsonUtility.FromJson<PlayerJoinedMessage>(json);

		// myId が未設定 or 自分自身なら無視
		if (string.IsNullOrEmpty(myId) || msg.id == myId) return;
		// すでに生成済みなら重複を防ぐ
		if (playerObjects.ContainsKey(msg.id)) return;



		var player = new PlayerData
		{
			id = msg.id,
			name = msg.name,
			player_number = msg.player_number,
			position = new PositionData { x = msg.position.x, y = msg.position.y, z = msg.position.z }
		};
		SpawnRemotePlayer(player);
	}

	// 他プレイヤーの移動反映
	private void HandlePlayerMoveMessage(string json)
	{
		var msg = JsonUtility.FromJson<PlayerMoveMessage>(json);

		if (msg != null && msg.position != null && playerObjects.ContainsKey(msg.id))
		{
			GameObject targetPlayer = playerObjects[msg.id];
			if (targetPlayer == null)
			{
				playerObjects.Remove(msg.id); // 死んでいるオブジェクトを辞書から掃除
				return;
			}

			Vector3 targetPos = new Vector3(
				msg.position.x,
				msg.position.y,
				msg.position.z
			);

			playerObjects[msg.id].transform.position = Vector3.Lerp(
				playerObjects[msg.id].transform.position,
				targetPos,
				Time.deltaTime * 10f
			);

			if (msg.rotation != null)
			{
				playerObjects[msg.id].transform.rotation = Quaternion.Euler(msg.rotation.x, msg.rotation.y, 0);
			}
		}
	}

	// プレイヤーが退出した際の削除処理
	private void HandlePlayerLeftMessage(string json)
	{
		var msg = JsonUtility.FromJson<PlayerLeftMessage>(json);

		if (playerObjects.ContainsKey(msg.id))
		{
			Destroy(playerObjects[msg.id]);
			playerObjects.Remove(msg.id);
			Debug.Log("プレイヤー退出: " + msg.name);
		}
	}

	// 他プレイヤーのオブジェクトを生成し、管理用辞書に登録
	private void SpawnRemotePlayer(PlayerData player)
	{
		if (playerObjects.ContainsKey(player.id)) return;

		GameObject newPlayer = Instantiate(playerPrefab);
		newPlayer.tag = "Player" + player.player_number;


		if (remotePlayerMaterial != null)
			newPlayer.GetComponentInChildren<Renderer>().material = remotePlayerMaterial;

		AudioListener remoteListener = newPlayer.GetComponent<AudioListener>();
		if (remoteListener != null)
		{
			Destroy(remoteListener);
		}

		// 1. 操作フラグをオフにする
		var controller = newPlayer.GetComponent<PlayerController>();
		if (controller != null) controller.isLocalPlayer = false;

    // 2. 物理演算が同期の邪魔をしないように「キネマティック」にする
    var rb = newPlayer.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.isKinematic = true; // サーバーからの座標上書きを優先させる
			rb.useGravity = false;
		}

		newPlayer.transform.position = new Vector3(player.position.x, player.position.y, player.position.z);
		playerObjects[player.id] = newPlayer;
	}


	/*
	// ElementGeneratorから呼ばれる
	public void SetSpawnPosition(Vector3 pos)
	{
		pendingSpawnPos = pos;
		hasSpawnPos = true;
	}
	*/


	// シーン遷移時などにプレイヤーオブジェクトを一括削除
	private void ClearRemotePlayers()
	{
		foreach (var obj in playerObjects.Values)
		{
			if (obj != null) Destroy(obj);
		}
		playerObjects.Clear();
	}

	async void Update()
	{
		if (websocket != null && websocket.State == WebSocketState.Open)
		{
			// 受信処理は絶対に毎フレーム必要（削るとカクつく）
#if !UNITY_WEBGL || UNITY_EDITOR
			websocket.DispatchMessageQueue();
#endif

        // 送信処理だけ、タイマーを使って頻度を落とす
        if (!string.IsNullOrEmpty(myId))
			{
				timer += Time.deltaTime;
				if (timer >= sendInterval)
				{
					SendPosition();
                timer = 0f;
				}
			}
		}
	}

	// 自分の現在地と回転をJSON形式でサーバーへ送信
	private async void SendPosition()
	{
		if (myPlayer == null) return;

		string json = $"{{\"type\":\"player_move\",\"id\":\"{myId}\",\"position\":{{\"x\":{myPlayer.transform.position.x},\"y\":{myPlayer.transform.position.y},\"z\":{myPlayer.transform.position.z}}},\"rotation\":{{\"x\":{myPlayer.transform.rotation.eulerAngles.x},\"y\":{myPlayer.transform.rotation.eulerAngles.y}}}}}";
		await websocket.SendText(json);
	}

	private async void OnApplicationQuit()
	{
		if (websocket != null && websocket.State == WebSocketState.Open)
		{
			await websocket.Close();
		}
	}
}

