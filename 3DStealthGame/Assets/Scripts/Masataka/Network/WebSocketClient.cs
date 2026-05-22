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

[System.Serializable]
public class TimerUpdateMessage
{
	public string type;           // "timer_update"
	public int time_remaining; // 残り秒数
}

// goal
[System.Serializable]
public class GoalMessage
{
	public string type;    // "goal"
	public string id;      // ゴールしたプレイヤーID
	public string name;    // プレイヤー名
	public float elapsed; // クリアタイム（秒）
}


// --- WebSocketクライアント本体 ---

public class WebSocketClient : MonoBehaviour
{
	public GameObject playerPrefab;
	public GameObject myPlayer;
	public string serverUrl = "ws://192.168.56.102:8080/ws?room_id=test&name=Player1";

	private WebSocket websocket;
	public string myId;
	private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
	private bool isGameSceneLoaded;
	private List<string> pendingMessages = new List<string>();

	private float sendInterval = 0.05f;
	private float timer = 0f;

	private string playerName;

	public Material localPlayerMaterial;
	public Material remotePlayerMaterial;

	private Vector3 pendingSpawnPos = Vector3.zero;
	private bool hasSpawnPos = false;

	private Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();
	private Dictionary<string, Quaternion> targetRotations = new Dictionary<string, Quaternion>();

	public string GetPlayerName() => playerName;
	public RoomMemberPanel roomMemberPanel;

	private Dictionary<int, Vector3> spawnPositions = new Dictionary<int, Vector3>();

	void Awake()
	{
		Application.runInBackground = true;
		Application.targetFrameRate = 60;
		DontDestroyOnLoad(this.gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
		isGameSceneLoaded = SceneManager.GetActiveScene().name == "MapTest";
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		isGameSceneLoaded = scene.name == "MapTest";

		if (scene.name != "MapTest")
		{
			ClearRemotePlayers();
		}
		else
		{
			Invoke("ProcessPendingMessages", 0.5f);
			// ★ MapTest ロード完了 → ミッションタイマースタート
			Invoke("DelayedGameStart", 0.6f);
		}
		Debug.Log("シーン変更: " + scene.name);
	}

	async void Start()
	{
		Application.runInBackground = true;
		playerName = "Player_" + Random.Range(1000, 9999);
		websocket = new WebSocket($"ws://192.168.56.102:8080/ws?room_id=test&name={playerName}");
		websocket.OnOpen += () => Debug.Log("サーバーに接続");
		websocket.OnMessage += OnMessageReceived;
		websocket.OnError += (e) => Debug.Log("エラー: " + e);
	}

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

	public void SetPlayerName(string name) { playerName = name; }

	public async void OnQuitButtonClicked()
	{
		if (websocket != null)
		{
			websocket.OnMessage -= OnMessageReceived;
			websocket.OnOpen -= OnWebSocketOpened;
			if (websocket.State == WebSocketState.Open) await websocket.Close();
			websocket = null;
		}

		if (myPlayer != null) { Destroy(myPlayer); myPlayer = null; }
		ClearRemotePlayers();
		myId = null;
		spawnPositions.Clear();
		pendingMessages.Clear();
		Debug.Log("接続を切断しました");
	}

	private void OnWebSocketOpened() { Debug.Log("接続成功"); }

	public async void OnReadyButtonClicked()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		string json = "{\"type\":\"ready\",\"position\":{\"x\":0,\"y\":1,\"z\":0}}";
		await websocket.SendText(json);
	}

	private void OnMessageReceived(byte[] bytes)
	{
		string json = Encoding.UTF8.GetString(bytes);

		if (!isGameSceneLoaded)
		{
			if (json.Contains("\"type\":\"start_game\""))
			{
				SceneManager.LoadScene("MapTest");
				return;
			}

			if (json.Contains("\"type\":\"init\"")) HandleInitForLobby(json);
			else if (json.Contains("\"type\":\"player_joined\"")) HandlePlayerJoinedForLobby(json);
			else if (json.Contains("\"type\":\"player_left\"")) HandlePlayerLeftForLobby(json);
			else if (json.Contains("\"type\":\"player_ready\"")) HandlePlayerReadyForLobby(json);
			else if (json.Contains("\"type\":\"existing_players\"")) HandleExistingPlayersForLobby(json);

			pendingMessages.Add(json);
			return;
		}

		ProcessMessage(json);
	}

	private void ProcessPendingMessages()
	{
		foreach (var json in pendingMessages) ProcessMessage(json);
		pendingMessages.Clear();
	}

	private void ProcessMessage(string json)
	{
		if (json.Contains("\"type\":\"init\"")) HandleInitMessage(json);
		else if (json.Contains("\"type\":\"existing_players\"")) HandleExistingPlayersMessage(json);
		else if (json.Contains("\"type\":\"player_joined\"")) HandlePlayerJoinedMessage(json);
		else if (json.Contains("\"type\":\"player_move\"")) HandlePlayerMoveMessage(json);
		else if (json.Contains("\"type\":\"player_left\"")) HandlePlayerLeftMessage(json);
		else if (json.Contains("\"type\":\"item_picked\"")) HandleItemPickedMessage(json);
		else if (json.Contains("\"type\":\"goal\"")) HandleGoalMessage(json);
		else if (json.Contains("\"type\":\"timer_update\"")) HandleTimerUpdate(json);
		else if (json.Contains("\"type\":\"start_game\""))
		{
			if (SceneManager.GetActiveScene().name == "MapTest")
			{
				ProcessPendingMessages();
				MissionManager.Instance?.OnGameStart();
			}
			else
			{
				//  チュートリアルを挟んでからシーン遷移
				SceneManager.LoadScene("MapTest");
				// シーンロード後に TutorialManager.Instance.ShowTutorial() を
				// OnSceneLoaded 内または MapTest シーンの TutorialManager.Start() で呼ぶ
			}
		}
		else if (json.Contains("\"type\":\"player_ready\""))
		{
			var msg = JsonUtility.FromJson<PlayerReadyMessage>(json);
			if (roomMemberPanel != null) roomMemberPanel.SetReady(msg.id, true);
		}
	}

	private void HandleInitMessage(string json)
	{
		if (SceneManager.GetActiveScene().name != "MapTest") return;
		if (myPlayer != null) return;
		InitMessage init = JsonUtility.FromJson<InitMessage>(json);
		myId = init.id;

		if (roomMemberPanel != null)
		{
			roomMemberPanel.RemoveMember("self");
			roomMemberPanel.AddOrUpdateMember(myId, playerName, false);
		}
		if (playerPrefab == null) return;

		myPlayer = Instantiate(playerPrefab);
		AttachNameTag(myPlayer, playerName, false);
		myPlayer.tag = "Player" + init.player_number;
		if (localPlayerMaterial != null)
			myPlayer.GetComponentInChildren<Renderer>().material = localPlayerMaterial;

		var elementGenerator = FindObjectOfType<ElementGenerator>();
		if (elementGenerator != null) elementGenerator.SetPlayerTransform(myPlayer.transform);

		var controller = myPlayer.GetComponent<PlayerController>();
		if (controller != null) controller.isLocalPlayer = true;
		myPlayer.GetComponent<PlayerController>().enabled = true;
		DontDestroyOnLoad(myPlayer);
		playerObjects[myId] = myPlayer;

		if (spawnPositions.ContainsKey(init.player_number))
			myPlayer.transform.position = spawnPositions[init.player_number];
		else if (init.position != null)
			myPlayer.transform.position = new Vector3(init.position.x, init.position.y, init.position.z);

		if (GlobalCamera.Instance != null)
		{
			GlobalCamera.Instance.SetTarget(myPlayer.transform);
			Debug.Log("カメラターゲット設定完了");
		}
		else
		{
			Debug.LogWarning("GlobalCamera.Instanceがnull");
		}

		var eg = FindObjectOfType<ElementGenerator>();
		if (eg != null) eg.SetPlayerTransform(myPlayer.transform);

		// ★ チュートリアル表示（MapTest ロード後の init で呼ぶ）
		if (TutorialManager.Instance != null)
		{
			TutorialManager.Instance.ShowTutorial(() =>
			{
				Debug.Log("ゲームスタート（チュートリアル後）");
			});
		}
		else
		{

		}
	}

	private void HandleExistingPlayersMessage(string json)
	{
		if (SceneManager.GetActiveScene().name != "MapTest") return;
		ExistingPlayersMessage msg = JsonUtility.FromJson<ExistingPlayersMessage>(json);
		if (msg != null && msg.players != null)
		{
			foreach (var player in msg.players)
			{
				if (player.id != myId)
				{
					SpawnRemotePlayer(player);
					if (roomMemberPanel != null) roomMemberPanel.AddOrUpdateMember(player.id, player.name, false);
				}
			}
		}
	}

	private void HandlePlayerJoinedMessage(string json)
	{
		if (SceneManager.GetActiveScene().name != "MapTest") return;
		var msg = JsonUtility.FromJson<PlayerJoinedMessage>(json);
		if (string.IsNullOrEmpty(myId) || msg.id == myId) return;
		if (playerObjects.ContainsKey(msg.id)) return;

		var player = new PlayerData
		{
			id = msg.id,
			name = msg.name,
			player_number = msg.player_number,
			position = new PositionData { x = msg.position.x, y = msg.position.y, z = msg.position.z }
		};
		SpawnRemotePlayer(player);
		if (roomMemberPanel != null) roomMemberPanel.AddOrUpdateMember(msg.id, msg.name, false);
	}

	private void HandlePlayerMoveMessage(string json)
	{
		var msg = JsonUtility.FromJson<PlayerMoveMessage>(json);
		if (msg == null || msg.position == null) return;
		if (!playerObjects.ContainsKey(msg.id)) return;

		GameObject targetPlayer = playerObjects[msg.id];
		if (targetPlayer == null) { playerObjects.Remove(msg.id); return; }

		targetPositions[msg.id] = new Vector3(msg.position.x, msg.position.y, msg.position.z);
		if (msg.rotation != null)
			targetRotations[msg.id] = Quaternion.Euler(msg.rotation.x, msg.rotation.y, 0);
	}

	private void HandlePlayerLeftMessage(string json)
	{
		var msg = JsonUtility.FromJson<PlayerLeftMessage>(json);
		if (playerObjects.ContainsKey(msg.id))
		{
			Destroy(playerObjects[msg.id]);
			playerObjects.Remove(msg.id);
		}
		if (roomMemberPanel != null) roomMemberPanel.RemoveMember(msg.id);
	}


	private void HandleItemPickedMessage(string json)
	{
		// 自分が取得した場合だけミッションフラグを立てる
		// （サーバーは全員にブロードキャストするので id で自分かどうか判定）
		var msg = JsonUtility.FromJson<GoalMessage>(json); // id フィールドだけ使う
		if (msg.id == myId)
			MissionManager.Instance?.OnItemPicked();
	}

	private void HandleGoalMessage(string json)
	{
		var msg = JsonUtility.FromJson<GoalMessage>(json);

		if (msg.id == myId)
		{
			// 自分のゴール処理
			MissionManager.Instance?.OnGoal();

			int missionCount = MissionManager.Instance != null ? MissionManager.Instance.GetClearedMissionCount() : 0;

		}
		else
		{
			Debug.Log($" {msg.name} がゴールしました！");
		}
	}

	private void HandleTimerUpdate(string json)
	{
		var msg = JsonUtility.FromJson<TimerUpdateMessage>(json);
	}

	// ─── ロビー用ハンドラ（変更なし）───

	private void HandleInitForLobby(string json)
	{
		InitMessage init = JsonUtility.FromJson<InitMessage>(json);
		myId = init.id;
		if (roomMemberPanel != null)
		{
			roomMemberPanel.RemoveMember("self");
			roomMemberPanel.AddOrUpdateMember(myId, playerName, false);
		}
	}

	private void HandlePlayerJoinedForLobby(string json)
	{
		var msg = JsonUtility.FromJson<PlayerJoinedMessage>(json);
		if (msg == null || msg.id == myId) return;
		if (roomMemberPanel != null) roomMemberPanel.AddOrUpdateMember(msg.id, msg.name, false);
	}

	private void HandleExistingPlayersForLobby(string json)
	{
		ExistingPlayersMessage msg = JsonUtility.FromJson<ExistingPlayersMessage>(json);
		if (msg == null || msg.players == null) return;
		foreach (var player in msg.players)
			if (player.id != myId && roomMemberPanel != null)
				roomMemberPanel.AddOrUpdateMember(player.id, player.name, false);
	}

	private void HandlePlayerLeftForLobby(string json)
	{
		var msg = JsonUtility.FromJson<PlayerLeftMessage>(json);
		if (roomMemberPanel != null) roomMemberPanel.RemoveMember(msg.id);
	}

	private void HandlePlayerReadyForLobby(string json)
	{
		var msg = JsonUtility.FromJson<PlayerReadyMessage>(json);
		if (roomMemberPanel != null) roomMemberPanel.SetReady(msg.id, true);
	}

	// ─── プレイヤー生成 ───

	private void SpawnRemotePlayer(PlayerData player)
	{
		if (playerObjects.ContainsKey(player.id)) return;

		GameObject newPlayer = Instantiate(playerPrefab);
		newPlayer.tag = "Player" + player.player_number;
		if (remotePlayerMaterial != null) newPlayer.GetComponentInChildren<Renderer>().material = remotePlayerMaterial;

		AudioListener remoteListener = newPlayer.GetComponent<AudioListener>();
		if (remoteListener != null) Destroy(remoteListener);

		var controller = newPlayer.GetComponent<PlayerController>();
		if (controller != null) controller.isLocalPlayer = false;

		var rb = newPlayer.GetComponent<Rigidbody>();
		if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

		newPlayer.transform.position = new Vector3(player.position.x, player.position.y, player.position.z);
		playerObjects[player.id] = newPlayer;
		AttachNameTag(newPlayer, player.name, true);

		var eg = FindObjectOfType<ElementGenerator>();
		if (eg != null) eg.SetRemotePlayerTransform(newPlayer.transform);
	}

	private void ClearRemotePlayers()
	{
		foreach (var obj in playerObjects.Values)
			if (obj != null) Destroy(obj);
		playerObjects.Clear();
	}

	// 竜希のスクリプトと連携用。消さないように注意。
	public void SetSpawnPosition(int playerNum, Vector3 pos) { spawnPositions[playerNum] = pos; }

	async void Update()
	{
		foreach (var id in targetPositions.Keys)
		{
			if (!playerObjects.ContainsKey(id)) continue;
			var obj = playerObjects[id];
			if (obj == null) continue;
			obj.transform.position = Vector3.Lerp(obj.transform.position, targetPositions[id], Time.deltaTime * 15f);
			if (targetRotations.ContainsKey(id))
				obj.transform.rotation = Quaternion.Lerp(obj.transform.rotation, targetRotations[id], Time.deltaTime * 15f);
		}

		if (websocket != null && websocket.State == WebSocketState.Open)
		{
#if !UNITY_WEBGL || UNITY_EDITOR
			websocket.DispatchMessageQueue();
#endif
			if (!string.IsNullOrEmpty(myId))
			{
				timer += Time.deltaTime;
				if (timer >= sendInterval) { SendPosition(); timer = 0f; }
			}
		}
	}

	private async void SendPosition()
	{
		if (myPlayer == null) return;
		string json = $"{{\"type\":\"player_move\",\"id\":\"{myId}\"," +
					  $"\"position\":{{\"x\":{myPlayer.transform.position.x}," +
					  $"\"y\":{myPlayer.transform.position.y}," +
					  $"\"z\":{myPlayer.transform.position.z}}}," +
					  $"\"rotation\":{{\"x\":{myPlayer.transform.rotation.eulerAngles.x}," +
					  $"\"y\":{myPlayer.transform.rotation.eulerAngles.y}}}}}";
		await websocket.SendText(json);
	}

	private void DelayedGameStart()
	{
		MissionManager.Instance?.OnGameStart();
		Debug.Log("DelayedGameStart: MissionManager.OnGameStart()呼び出し");
	}

	private async void OnApplicationQuit()
	{
		if (websocket != null && websocket.State == WebSocketState.Open) await websocket.Close();
	}

	private void AttachNameTag(GameObject playerObj, string name, bool visible)
	{
		GameObject tagObj = new GameObject("NameTag");
		tagObj.transform.SetParent(playerObj.transform, false);
		NameTag tag = tagObj.AddComponent<NameTag>();
		tag.SetName(name);
		tag.SetVisible(visible);
	}
}