using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

#region メッセージ定義

// 接続初期化メッセージ
[System.Serializable]
public class NetworkEnvelope
{
	public string type;
}

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

// 既存プレイヤー一覧メッセージ（後から入室した側が受信）
[System.Serializable]
public class ExistingPlayersMessage
{
	public string type;
	public PlayerData[] players;
}

// 新規プレイヤー参加通知メッセージ
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

// プレイヤー移動・動作状態の同期メッセージ
[System.Serializable]
public class PlayerMoveMessage
{
	public string type;
	public string id;
	public PositionData position;
	public PositionData rotation;
	public string anim_state;   // "idle" / "run" / "sneak"
	public string anim_trigger; // "PunchEnemy" / "PunchSwitch" など
}

[System.Serializable]
public class PlayerMoveSendMessage
{
	public string type = "player_move";
	public string id;
	public PositionData position;
	public PositionData rotation;
	public string anim_state;
	public string anim_trigger;
}

// プレイヤー退室通知メッセージ
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

// 残り時間更新メッセージ
[System.Serializable]
public class TimerUpdateMessage
{
	public string type;        // "timer_update"
	public int time_remaining; // 残り秒数
}

// ゴール関連メッセージ
[System.Serializable]
public class GoalMessage
{
	public string type;    // "goal"
	public string id;      // ゴールしたプレイヤーID
	public string name;    // プレイヤー名
	public float elapsed;  // クリアタイム（秒）
}

[System.Serializable]
public class GoalSendMessage
{
	public string type = "goal";
}

// 敵の位置・状態同期メッセージ
[System.Serializable]
public class EnemyMoveMessage
{
	public string type;         // "enemy_move"
	public int enemy_index;     // シーン内の敵の番号
	public float x;
	public float y;
	public float z;
	public float angle;         // 向き（Y軸回転）
	public float light_r;
	public float light_g;
	public float light_b;
	public float last_sound_x;
	public float last_sound_z;
	public string reaction;     // "" / "!" / "?"
	public string sender_id;
}

[System.Serializable]
public class EnemyMoveSendMessage
{
	public string type = "enemy_move";
	public int enemy_index;
	public float x;
	public float y;
	public float z;
	public float angle;
	public float light_r;
	public float light_g;
	public float light_b;
	public float last_sound_x;
	public float last_sound_z;
	public string reaction;
}
// スイッチ操作の同期メッセージ
[System.Serializable]
public class SwitchActivatedMessage
{
	public string type;
	public int switch_id;
}

// リスポーン位置の同期メッセージ
[System.Serializable]
public class RespawnMessage
{
	public string type;
	public string id;
	public PositionData position;
}

// 意思疎通用チャットメッセージ
[System.Serializable]
public class ChatMessage
{
	public string type;
	public string message;
	public string sender_id;
	public string sender_name;
}

[System.Serializable]
public class ChatSendMessage
{
	public string type = "chat";
	public string message;
	public string sender_id;
}

[System.Serializable]
public class StaminaItemDropMessage
{
	public string type;
	public string sender_id;
	public int drop_type;
	public int enemy_id;
	public float x;
	public float y;
	public float z;
}

[System.Serializable]
public class EnemyFoundMessage
{
	public string type = "enemy_found";
	public string sender_id;
}

[System.Serializable]
public class ItemPickedSendMessage
{
	public string type = "item_picked";
	public float x;
	public float z;
}

[System.Serializable]
public class StaminaItemPickedSendMessage
{
	public string type = "stamina_item_picked";
	public string sender_id;
	public float x;
	public float z;
}

[System.Serializable]
public class StaminaItemDropSendMessage
{
	public string type = "stamina_item_drop";
	public string sender_id;
	public int drop_type;
	public float x;
	public float y;
	public float z;
}

[System.Serializable]
public class StaminaItemDropRequestSendMessage
{
	public string type = "stamina_item_drop_request";
	public string sender_id;
	public int drop_type;
	public float x;
	public float y;
	public float z;
}

[System.Serializable]
public class RespawnSendMessage
{
	public string type = "respawn";
	public string id;
	public PositionData position;
}

[System.Serializable]
public class SwitchActivatedSendMessage
{
	public string type = "switch_activated";
	public int switch_id;
}

[System.Serializable]
public class EnemyStunSendMessage
{
	public string type;
	public int enemy_index;
	public string sender_id;
}

[System.Serializable]
public class RemoteRespawnSendMessage
{
	public string type = "remote_respawn";
	public string id;
}

[System.Serializable]
public class ReadySendMessage
{
	public string type = "ready";
	public PositionData position;
}

[System.Serializable]
public class StaminaStateMessage
{
	public string type;
	public string sender_id;
	public int current;
	public int max;
}

[System.Serializable]
public class StartGameMessage
{
	public string type;
	public string session_id;
}

#endregion

/// <summary>
/// サーバーへ送るメッセージを作る。
/// </summary>
public static class SendMessageBuilder
{
	public static string Goal()
	{
		return JsonUtility.ToJson(new GoalSendMessage());
	}

	public static string PlayerMove(
	string id,
	Vector3 pos,
	Vector3 rot,
	string animState,
	string trigger)
	{
		var data = new PlayerMoveSendMessage
		{
			id = id,
			position = new PositionData
			{
				x = pos.x,
				y = pos.y,
				z = pos.z
			},
			rotation = new PositionData
			{
				x = rot.x,
				y = rot.y,
				z = 0f
			},
			anim_state = string.IsNullOrEmpty(animState) ? "idle" : animState,
			anim_trigger = string.IsNullOrEmpty(trigger) ? "" : trigger
		};

		return JsonUtility.ToJson(data);
	}

	public static string Respawn(string id, Vector3 pos)
	{
		var data = new RespawnSendMessage
		{
			id = id,
			position = new PositionData
			{
				x = pos.x,
				y = pos.y,
				z = pos.z
			}
		};

		return JsonUtility.ToJson(data);
	}

	public static string SwitchActivated(int switchId)
	{
		var data = new SwitchActivatedSendMessage
		{
			switch_id = switchId
		};

		return JsonUtility.ToJson(data);
	}

	public static string ItemPicked()
	{
		return JsonUtility.ToJson(new ItemPickedSendMessage());
	}

	public static string ItemPicked(Vector3 pos)
	{
		var data = new ItemPickedSendMessage
		{
			x = pos.x,
			z = pos.z
		};

		return JsonUtility.ToJson(data);
	}

	public static string EnemyStun(int enemyIndex, string senderId)
	{
		var data = new EnemyStunSendMessage
		{
			type = "enemy_stun",
			enemy_index = enemyIndex,
			sender_id = senderId
		};

		return JsonUtility.ToJson(data);
	}

	public static string EnemyStunCancel(int enemyIndex, string senderId)
	{
		var data = new EnemyStunSendMessage
		{
			type = "enemy_stun_cancel",
			enemy_index = enemyIndex,
			sender_id = senderId
		};

		return JsonUtility.ToJson(data);
	}

	public static string RemoteRespawn(string id)
	{
		var data = new RemoteRespawnSendMessage
		{
			id = id
		};

		return JsonUtility.ToJson(data);
	}

	public static string Chat(string message, string senderId)
	{
		var data = new ChatSendMessage
		{
			message = message,
			sender_id = senderId
		};

		return JsonUtility.ToJson(data);
	}

	public static string StaminaItemPicked(string senderId, Vector3 pos)
	{
		var data = new StaminaItemPickedSendMessage
		{
			sender_id = senderId,
			x = pos.x,
			z = pos.z
		};

		return JsonUtility.ToJson(data);
	}

	public static string StaminaItemDrop(string senderId, int dropType, Vector3 pos)
	{
		var data = new StaminaItemDropSendMessage
		{
			sender_id = senderId,
			drop_type = dropType,
			x = pos.x,
			y = pos.y,
			z = pos.z
		};

		return JsonUtility.ToJson(data);
	}

	public static string StaminaItemDropRequest(string senderId, int dropType, Vector3 pos)
	{
		var data = new StaminaItemDropRequestSendMessage
		{
			sender_id = senderId,
			drop_type = dropType,
			x = pos.x,
			y = pos.y,
			z = pos.z
		};

		return JsonUtility.ToJson(data);
	}

	public static string EnemyFound(string senderId)
	{
		var data = new EnemyFoundMessage
		{
			sender_id = senderId
		};

		return JsonUtility.ToJson(data);
	}

	public static string EnemyMove(
	int index,
	Vector3 pos,
	float angle,
	Color light,
	string reaction,
	Vector3 lastSound)
	{
		var data = new EnemyMoveSendMessage
		{
			enemy_index = index,
			x = pos.x,
			y = pos.y,
			z = pos.z,
			angle = angle,
			light_r = light.r,
			light_g = light.g,
			light_b = light.b,
			last_sound_x = lastSound.x,
			last_sound_z = lastSound.z,
			reaction = string.IsNullOrEmpty(reaction) ? "" : reaction
		};

		return JsonUtility.ToJson(data);
	}

	public static string Ready(Vector3 pos)
	{
		var data = new ReadySendMessage
		{
			position = new PositionData
			{
				x = pos.x,
				y = pos.y,
				z = pos.z
			}
		};

		return JsonUtility.ToJson(data);
	}

	public static string StaminaState(string senderId, int current, int max)
	{
		return JsonUtility.ToJson(new StaminaStateMessage
		{
			type = "stamina_state",
			sender_id = senderId,
			current = current,
			max = max
		});
	}
}

/// <summary>
/// ゲーム中のWebSocket通信をまとめて扱う。
/// ルーム接続、プレイヤー同期、敵同期、ゲーム進行通知を担当。
/// </summary>
public class WebSocketClient : MonoBehaviour
{
	#region Inspector設定

	public GameObject playerPrefab;
	public GameObject myPlayer;
	public string serverUrl = "ws://192.168.56.102:8080/ws?room_id=test&name=Player1";

	public Material localPlayerMaterial;  // Player1
	public Material remotePlayerMaterial; // Player2

	[Header("プレイヤーアウトライン")]
	[SerializeField] private Material localPlayerOutlineMaterial;
	[SerializeField] private Material remotePlayerOutlineMaterial;

	public RoomMemberPanel roomMemberPanel;

	public string ngrokUrl = "https://rice-washer-suitcase.ngrok-free.dev";
	public ServerMode serverMode = ServerMode.VirtualBox;

	[Header("ローカルネットワーク設定")]
	[SerializeField] private string localNetworkIp = "192.168.0.200";

	[Header("VPS設定")]
	[SerializeField] private string vpsIp = "160.251.231.139";

	public enum ServerMode
	{
		VirtualBox,
		LocalHost,
		Ngrok,
		Render,
		FlyIO,
		LocalNetwork,
		VPS
	}

	#endregion

	#region 内部状態

	private WebSocket websocket;
	public string myId;

	private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
	private Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();
	private Dictionary<string, Quaternion> targetRotations = new Dictionary<string, Quaternion>();
	private Dictionary<int, Vector3> spawnPositions = new Dictionary<int, Vector3>();

	private bool isGameSceneLoaded;
	private List<string> pendingMessages = new List<string>();

	private float sendInterval = 0.05f;
	private float timer = 0f;
	private string playerName;

	private string currentRoomId = "";

	// 以前のスポーン調整用。今は保持のみ。
	private Vector3 pendingSpawnPos = Vector3.zero;
	private bool hasSpawnPos = false;

	public int myPlayerNumber = 0; // 1=ホスト, 2=ゲスト

	private GameObject[] _enemyObjects;
	private Dictionary<int, Vector3> enemyTargetPositions = new Dictionary<int, Vector3>();
	private Dictionary<int, float> enemyTargetAngles = new Dictionary<int, float>();
	private float enemySendTimer = 0f;
	private float enemySendInterval = 0.05f;

	// 通信調整中に使っていた値。安全確認が終わるまでは残す。
	private Vector3 _lastRemotePosition = Vector3.zero;
	private bool _stunSent = false;
	private int _connectGeneration = 0;

	private bool _remoteRespawnSent = false;

private int _remoteCurrentStamina = 10;
	private int _remoteMaxStamina = 10;

	public bool CanRemoteRecoverStamina()
	{
		return _remoteCurrentStamina < _remoteMaxStamina;
	}

	private string currentSessionId = "";

#endregion

	#region プレイヤー番号

	public bool IsHostPlayer() => myPlayerNumber == 1;
	public bool IsGuestPlayer() => myPlayerNumber == 2;

	#endregion

	#region Unityイベント

	void Awake()
	{
		var existing = FindObjectsOfType<WebSocketClient>();
		if (existing.Length > 1)
		{
			// 自分より先に存在するインスタンスがあれば自分を破棄
			foreach (var other in existing)
			{
				if (other != this)
				{
					Destroy(gameObject);
					return;
				}
			}
		}
		Application.runInBackground = true;
		Application.targetFrameRate = 60;
		DontDestroyOnLoad(this.gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
		isGameSceneLoaded = SceneManager.GetActiveScene().name == "GameScene";
	}


	void Start()
	{
		Application.runInBackground = true;
		playerName = "";
	}


	void Update()
	{
		// 相手プレイヤーを補間
		foreach (var id in targetPositions.Keys)
		{
			if (id == myId) continue;
			if (!playerObjects.ContainsKey(id)) continue;
			var obj = playerObjects[id];
			if (obj == null) continue;
			obj.transform.position = Vector3.Lerp(obj.transform.position, targetPositions[id], Time.deltaTime * 25f);
			if (targetRotations.ContainsKey(id))
				obj.transform.rotation = Quaternion.Lerp(obj.transform.rotation, targetRotations[id], Time.deltaTime * 25f);
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
				UpdateEnemySync();
			}
		}
	}


	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		isGameSceneLoaded = scene.name == "GameScene";

		if (scene.name == "Title")
		{
			// Titleシーン再ロード時に参照を取り直す
			roomMemberPanel = FindObjectOfType<RoomMemberPanel>();
		}

		if (scene.name != "GameScene")
		{
			ClearRemotePlayers();
		}
		else
		{
			Invoke("ProcessPendingMessages", 0.5f);
			Invoke("DelayedGameStart", 0.6f);
		}
	}


	private async void OnApplicationQuit()
	{
		if (websocket != null && websocket.State == WebSocketState.Open) await websocket.Close();
	}


	#endregion


	#region 接続管理

	private void OnWebSocketOpened() { Debug.Log("接続成功"); }

	private const int MaxPlayerNameLength = 10;

	public void SetPlayerName(string name)
	{
		name = (name ?? "").Trim();

		if (name.Length > MaxPlayerNameLength)
		{
			name = name.Substring(0, MaxPlayerNameLength);
		}

		playerName = name;
	}

	public string GetPlayerName() => playerName;


	private string GetServerUrl(string roomId)
	{
		switch (serverMode)
		{
			case ServerMode.VirtualBox:
				return $"ws://192.168.56.102:8080/ws?room_id={roomId}&name={playerName}";
			case ServerMode.LocalHost:
				return $"ws://localhost:8080/ws?room_id={roomId}&name={playerName}";
			case ServerMode.Ngrok:
				return $"wss://{ngrokUrl.Replace("https://", "")}/ws?room_id={roomId}&name={playerName}";
			case ServerMode.Render:
				return $"wss://stealth-game-server.onrender.com/ws?room_id={roomId}&name={playerName}";
			case ServerMode.FlyIO:
				return $"wss://stealth-game-server.fly.dev/ws?room_id={roomId}&name={playerName}";
			case ServerMode.LocalNetwork:
				return $"ws://{localNetworkIp}:8080/ws?room_id={roomId}&name={playerName}";
			case ServerMode.VPS:
				return $"ws://{vpsIp}:8080/ws?room_id={roomId}&name={playerName}";
			default:
				return $"ws://192.168.56.102:8080/ws?room_id={roomId}&name={playerName}";
		}
	}

	public string GetHttpBaseUrl()
	{
		switch (serverMode)
		{
			case ServerMode.VirtualBox:
				return "http://192.168.56.102:8080";

			case ServerMode.LocalHost:
				return "http://localhost:8080";

			case ServerMode.Ngrok:
				return ngrokUrl.TrimEnd('/');

			case ServerMode.Render:
				return "https://stealth-game-server.onrender.com";

			case ServerMode.FlyIO:
				return "https://stealth-game-server.fly.dev";

			case ServerMode.LocalNetwork:
				return $"http://{localNetworkIp}:8080";

			case ServerMode.VPS:
				return $"http://{vpsIp}:8080";

			default:
				return "http://192.168.56.102:8080";
		}
	}

	public async void ConnectToRoom(string roomId)
	{
		currentRoomId = roomId;
		if (websocket != null)
		{
			websocket.OnMessage -= OnMessageReceived;
			websocket.OnOpen -= OnWebSocketOpened;
			if (websocket.State == WebSocketState.Open)
			{
				try { await websocket.Close(); } catch { }
			}
			else
			{
				websocket.CancelConnection();
			}
			websocket = null;
		}

		var ws = new WebSocket(GetServerUrl(roomId));
		ws.OnOpen += OnWebSocketOpened;
		ws.OnMessage += OnMessageReceived;
		//ws.OnError += (e) => Debug.Log($"接続エラー: {e}");
		websocket = ws;
		await ws.Connect();
	}


	public async void OnReadyButtonClicked()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;

		string json = SendMessageBuilder.Ready(new Vector3(0f, 1f, 0f));
		await websocket.SendText(json);
	}


	public async void OnQuitButtonClicked()
	{
		if (websocket != null)
		{
			websocket.OnMessage -= OnMessageReceived;
			websocket.OnOpen -= OnWebSocketOpened;
			if (websocket.State == WebSocketState.Open)
			{
				try { await websocket.Close(); } catch { }
			}
			else
			{
				websocket.CancelConnection();
			}
			websocket = null;
		}

		if (myPlayer != null) { Destroy(myPlayer); myPlayer = null; }
		ClearRemotePlayers();
		myId = null;
		spawnPositions.Clear();
		pendingMessages.Clear();
	}


	public async System.Threading.Tasks.Task DisconnectAndReset()
	{
		//_connectGeneration++;
		if (websocket != null)
		{
			websocket.OnMessage -= OnMessageReceived;
			websocket.OnOpen -= OnWebSocketOpened;
			if (websocket.State == WebSocketState.Open)
			{
				try { await websocket.Close(); } catch { }
			}
			else
			{
				websocket.CancelConnection();
			}
			websocket = null;
		}

		if (myPlayer != null) { Destroy(myPlayer); myPlayer = null; }
		ClearRemotePlayers();

		myId = null;
		playerName = "";
		myPlayerNumber = 0;
		spawnPositions.Clear();
		pendingMessages.Clear();
		hasSpawnPos = false;
		pendingSpawnPos = Vector3.zero;
		_enemyObjects = null;
		enemyTargetPositions.Clear();
		enemyTargetAngles.Clear();
	}


	#endregion

	#region 受信入口

	private void OnMessageReceived(byte[] bytes)
	{
		string json = Encoding.UTF8.GetString(bytes);
		string type = GetMessageType(json);

		if (!isGameSceneLoaded)
		{
			switch (type)
			{
				case "start_game":
					HandleStartGameMessage(json);
					return;

				case "init":
					HandleInitForLobby(json);
					break;

				case "player_joined":
					HandlePlayerJoinedForLobby(json);
					break;

				case "player_left":
					HandlePlayerLeftForLobby(json);
					break;

				case "player_ready":
					HandlePlayerReadyForLobby(json);
					break;

				case "existing_players":
					HandleExistingPlayersForLobby(json);
					break;

				default:
					break;
			}

			// remote_respawnはゲーム開始後に再処理しない
			if (type != "remote_respawn")
			{
				pendingMessages.Add(json);
			}

			return;
		}

		ProcessMessage(json);
	}


	private void ProcessPendingMessages()
	{
		foreach (var json in pendingMessages) ProcessMessage(json);
		pendingMessages.Clear();
	}


	private string GetMessageType(string json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return "";
		}

		try
		{
			var envelope = JsonUtility.FromJson<NetworkEnvelope>(json);

			if (envelope == null || string.IsNullOrEmpty(envelope.type))
			{
				Debug.LogWarning($"typeが存在しないメッセージ: {json}");
				return "";
			}

			return envelope.type;
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"メッセージ解析失敗: {e.Message}\njson: {json}");
			return "";
		}
	}


	private void ProcessMessage(string json)
	{
		string type = GetMessageType(json);

		switch (type)
		{
			case "init":
				HandleInitMessage(json);
				break;

			case "existing_players":
				HandleExistingPlayersMessage(json);
				break;

			case "player_joined":
				HandlePlayerJoinedMessage(json);
				break;

			case "player_move":
				HandlePlayerMoveMessage(json);
				break;

			case "player_left":
				HandlePlayerLeftMessage(json);
				break;

			case "item_picked":
				HandleItemPickedMessage(json);
				break;

			case "timer_update":
				HandleTimerUpdate(json);
				break;

			case "enemy_move":
				HandleEnemyMoveMessage(json);
				break;

			case "remote_respawn":
				HandleRemoteRespawnMessage(json);
				break;

			case "player_goal":
				HandlePlayerGoalMessage(json);
				break;

			case "all_goal":
				HandleAllGoalMessage(json);
				break;

			case "switch_activated":
				HandleSwitchActivatedMessage(json);
				break;

			case "enemy_stun_cancel":
				HandleEnemyStunCancelMessage(json);
				break;

			case "enemy_stun":
				HandleEnemyStunMessage(json);
				break;

			case "respawn":
				HandleRespawnMessage(json);
				break;

			case "stamina_item_picked":
				HandleStaminaItemPickedMessage(json);
				break;

			case "chat":
				HandleChatMessage(json);
				break;

			case "stamina_item_drop_request":
				HandleStaminaItemDropRequestMessage(json);
				break;

			case "stamina_item_drop":
				HandleStaminaItemDropMessage(json);
				break;

			case "start_game":
				HandleStartGameMessage(json);
				break;

			case "player_ready":
				HandlePlayerReadyMessage(json);
				break;

			case "enemy_found":
				HandleEnemyFoundMessage(json);
				break;

			case "stamina_state":
				HandleStaminaStateMessage(json);
				break;

			default:
				Debug.LogWarning($"未対応のメッセージtype: {type}\njson: {json}");
				break;
		}
	}


	#endregion

	#region ロビー受信

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
				roomMemberPanel.AddOrUpdateMember(player.id, player.name, player.is_ready);
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


	private void HandlePlayerReadyMessage(string json)
	{
		var msg = JsonUtility.FromJson<PlayerReadyMessage>(json);

		if (roomMemberPanel != null)
		{
			roomMemberPanel.SetReady(msg.id, true);
		}
	}

	private void HandleStaminaStateMessage(string json)
	{
		var msg = JsonUtility.FromJson<StaminaStateMessage>(json);
		if (msg.sender_id == myId) return;

		_remoteCurrentStamina = msg.current;
		_remoteMaxStamina = msg.max;

		Debug.Log($"[RemoteStamina] current={_remoteCurrentStamina}/{_remoteMaxStamina}");
	}


	#endregion

	#region ゲーム開始・プレイヤー同期

	private void HandleStartGameMessage(string json)
	{
		Debug.Log($"[StartGame Raw] {json}");

		StartGameMessage msg =
			JsonUtility.FromJson<StartGameMessage>(json);

		currentSessionId = msg.session_id;

		Debug.Log(
			$"[Session] session_id = {currentSessionId}"
		);

		if (SceneManager.GetActiveScene().name == "GameScene")
		{
			ProcessPendingMessages();
			MissionManager.Instance?.OnGameStart();
		}
		else
		{
			SceneManager.LoadScene("GameScene");
		}
	}


	private void HandleInitMessage(string json)
	{
		if (SceneManager.GetActiveScene().name != "GameScene") return;
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
		myPlayerNumber = init.player_number;

		var eg = FindObjectOfType<ElementGenerator>();

		// ゲスト（Player2）は敵のAIを止め、サーバーからの位置情報で動かす
		if (IsGuestPlayer())
		{
			var enemies = GameObject.FindGameObjectsWithTag("Enemy");
			foreach (var e in enemies)
			{
				var em = e.GetComponent<EnemyManager>();
				if (em != null) em.isRemoteControlled = true;
			}
		}

		if (IsHostPlayer())
		{
			myPlayer.GetComponentInChildren<Renderer>().material = localPlayerMaterial;
			AddOutlineMaterial(myPlayer, localPlayerOutlineMaterial);
		}
		else
		{
			myPlayer.GetComponentInChildren<Renderer>().material = remotePlayerMaterial;
			AddOutlineMaterial(myPlayer, remotePlayerOutlineMaterial);
		}

		var elementGenerator = FindObjectOfType<ElementGenerator>();
		if (elementGenerator != null) elementGenerator.SetRemotePlayerTransform(myPlayer.transform);

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
			GlobalCamera.Instance.SetTarget(myPlayer.transform);
		//else
		//Debug.LogWarning("GlobalCamera.Instanceがnull");

		if (elementGenerator != null) elementGenerator.SetRemotePlayerTransform(myPlayer.transform);

		// ゲーム開始時にチュートリアルを表示する
		if (TutorialManager.Instance != null)
		{
			TutorialManager.Instance.ShowTutorial(() =>
			{
				//Debug.Log("ゲームスタート（チュートリアル後）");
			});
		}

		// 敵に自分のプレイヤーを渡す（ホストのみ）
		if (IsHostPlayer())
		{
			var enemyList = GameObject.FindGameObjectsWithTag("Enemy");
			foreach (var e in enemyList)
			{
				var em = e.GetComponent<EnemyManager>();
				//if (em != null) em.SetTargetPlayer(myPlayer.transform);
			}
		}
	}


	private void HandleExistingPlayersMessage(string json)
	{
		if (SceneManager.GetActiveScene().name != "GameScene") return;
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
		if (SceneManager.GetActiveScene().name != "GameScene") return;
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

		// ホスト側だけ、相手の足音を敵へ渡す
		if (IsHostPlayer() && msg.id != myId)
		{
			Vector3 newPos = new Vector3(msg.position.x, msg.position.y, msg.position.z);
			if (msg.anim_state == "run" || msg.anim_trigger == "PunchSwitch")
			{
				var enemies = GameObject.FindGameObjectsWithTag("Enemy");
				foreach (var e in enemies)
				{
					var em = e.GetComponent<EnemyManager>();
					if (em != null)
						em.HandleSoundFromRemote(newPos, 1f);
				}
			}
		}

		// アニメーション反映
		var anim = targetPlayer.GetComponentInChildren<Animator>();
		if (anim == null) return;

		if (!string.IsNullOrEmpty(msg.anim_state))
		{
			anim.SetBool("Run", msg.anim_state == "run");
			anim.SetBool("Sneak", msg.anim_state == "sneak");
		}

		if (!string.IsNullOrEmpty(msg.anim_trigger))
		{
			anim.SetTrigger(msg.anim_trigger);
		}
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


	#endregion

	#region ゲーム進行

	private void HandlePlayerGoalMessage(string json)
	{
		var msg = JsonUtility.FromJson<GoalMessage>(json);

		if (msg.id == myId)
		{
			MissionManager.Instance?.OnGoal();
		//	LogManager.Instance?.AddWaitingLog("ゴールした！相手を待っています", "#aadd44");
		}
		else
		{
		//	LogManager.Instance?.AddLog("味方がゴールした！早くゴールへ向かおう！", "#aadd44");
		}
	}


	private void HandleAllGoalMessage(string json)
	{
		SoundManager.Instance?.PlayClear();
		var msg = JsonUtility.FromJson<GoalMessage>(json);
		SoundManager.Instance?.StopBGM();
		MissionManager.Instance?.StopTimer();
		MissionManager.Instance?.ShowClearMessage();
		LogManager.Instance?.StopWaitingLog();

		if (MissionManager.Instance != null)
		{
			ResultData.elapsedTime = msg.elapsed;

			ResultData.missionCount =
				MissionManager.Instance.GetClearedMissionCount();

			ResultData.mission1Done =
				MissionManager.Instance.Mission1Done;

			ResultData.mission2Done =
				MissionManager.Instance.Mission2Done;

			ResultData.mission3Done =
				MissionManager.Instance.Mission3Done;
		}

		ResultData.playerName = playerName;
		ResultData.roomId = currentRoomId;
		ResultData.sessionId = currentSessionId;

		ResultData.deathCount = PlayMetrics.DeathCount;
		ResultData.punchCount = PlayMetrics.PunchCount;
		ResultData.chatCount = PlayMetrics.ChatCount;
		ResultData.sneakTime = PlayMetrics.SneakTime;
		ResultData.staminaItemCount = PlayMetrics.StaminaItemCount;
		foreach (var obj in playerObjects.Values)
		{
			var nameTag = obj.GetComponentInChildren<NameTag>();
			if (nameTag != null)
				ResultData.remotePlayerName = nameTag.GetName();
		}

		MissionManager.Instance?.StartCoroutine(
			MissionManager.Instance.FadeToResult()
		);
	}


	private void LoadResultScene()
	{
		var msg_dummy = new GoalMessage();
		if (MissionManager.Instance != null)
		{
			ResultData.elapsedTime =
				MissionManager.Instance.GetElapsedSeconds();

			ResultData.missionCount =
				MissionManager.Instance.GetClearedMissionCount();

			ResultData.mission1Done =
				MissionManager.Instance.Mission1Done;

			ResultData.mission2Done =
				MissionManager.Instance.Mission2Done;

			ResultData.mission3Done =
				MissionManager.Instance.Mission3Done;
		}

		ResultData.playerName = playerName;
		ResultData.roomId = currentRoomId;

		ResultData.deathCount = PlayMetrics.DeathCount;
		ResultData.punchCount = PlayMetrics.PunchCount;
		ResultData.chatCount = PlayMetrics.ChatCount;
		ResultData.staminaItemCount = PlayMetrics.StaminaItemCount;
		foreach (var obj in playerObjects.Values)
		{
			var nameTag = obj.GetComponentInChildren<NameTag>();
			if (nameTag != null)
				ResultData.remotePlayerName = nameTag.GetName();
		}
		SceneManager.LoadScene("Result");
	}


	private void HandleTimerUpdate(string json)
	{
		var msg = JsonUtility.FromJson<TimerUpdateMessage>(json);
	}

	private void RestoreStaminaOnRespawn()
	{
		if (StaminaManager.Instance == null) return;

		if (StaminaManager.Instance.GetCurrentStamina() <= 4)
		{
			StaminaManager.Instance.SetStamina(5);
			LogManager.Instance?.AddLog("スタミナが5に戻った", "#88ccff");
			Debug.Log("[Respawn] スタミナを5に戻しました");
		}
	}


	private void HandleRespawnMessage(string json)
	{
		var msg = JsonUtility.FromJson<RespawnMessage>(json);
		if (msg.id == myId)
		{
			RestoreStaminaOnRespawn();

			targetPositions.Remove(myId);
			LogManager.Instance?.AddLog("リスポーンした", "#ff6666");
			return;
		}
		if (!playerObjects.ContainsKey(msg.id)) return;
		var obj = playerObjects[msg.id];
		if (obj == null) return;
		obj.transform.position = new Vector3(msg.position.x, msg.position.y, msg.position.z);
		targetPositions[msg.id] = obj.transform.position;
		LogManager.Instance?.AddLog("味方がリスポーンした", "#ff6666");
	}


	private void HandleRemoteRespawnMessage(string json)
	{
		// 相手が敵に見つかったので、ミッション3を失敗扱いにする
		MissionManager.Instance?.OnEnemyFound();

		var mypc = myPlayer?.GetComponent<PlayerController>();
		if (mypc != null && mypc.IsFading) return;

		if (IsGuestPlayer())
		{
			if (myPlayer == null) return;

			var pc = myPlayer.GetComponent<PlayerController>();
			if (pc == null) return;

			pc.RespawnWithEffectPublic();
			RestoreStaminaOnRespawn();
			LogManager.Instance?.AddLog("リスポーンした", "#ff6666");
		}
		else if (IsHostPlayer())
		{
			LogManager.Instance?.AddLog("味方がリスポーンした", "#ff6666");
		}
	}


	private void HandleEnemyFoundMessage(string json)
	{
		var msg = JsonUtility.FromJson<EnemyFoundMessage>(json);

		// 自分が送った通知なら無視
		if (msg != null && msg.sender_id == myId) return;

		MissionManager.Instance?.OnEnemyFound();
	}


	#endregion

	#region 敵・ギミック・アイテム同期

	private void HandleEnemyMoveMessage(string json)
	{
		if (IsHostPlayer()) return;
		var msg = JsonUtility.FromJson<EnemyMoveMessage>(json);
		enemyTargetPositions[msg.enemy_index] = new Vector3(msg.x, msg.y, msg.z);
		enemyTargetAngles[msg.enemy_index] = msg.angle;

		if (_enemyObjects == null || _enemyObjects.Length == 0)
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");

		if (msg.enemy_index < _enemyObjects.Length)
		{
			var light = _enemyObjects[msg.enemy_index].GetComponentInChildren<Light>();
			if (light != null)
				light.color = new Color(msg.light_r, msg.light_g, msg.light_b);

			var em = _enemyObjects[msg.enemy_index].GetComponent<EnemyManager>();
			if (em != null)
			{
				em.SetReactionState(msg.reaction);
				em.SetLastSoundPosition(new Vector3(msg.last_sound_x, 0, msg.last_sound_z));
			}
		}
	}


	private void HandleEnemyStunMessage(string json)
	{
		var msg = JsonUtility.FromJson<EnemyStunSendMessage>(json);
		if (msg == null) return;

		if (msg.sender_id == myId) return;

		if (msg.enemy_index < 0)
		{
			Debug.LogWarning($"enemy_stun の enemy_index が不正です: {msg.enemy_index}");
			return;
		}

		if (_enemyObjects == null || _enemyObjects.Length == 0)
		{
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
		}

		foreach (var e in _enemyObjects)
		{
			if (e == null) continue;

			var em = e.GetComponent<EnemyManager>();
			if (em == null) continue;

			if (em.enemyID == msg.enemy_index)
			{
				em.PlayAnimationEnemy();
				break;
			}
		}
	}


	private void HandleEnemyStunCancelMessage(string json)
	{
		var msg = JsonUtility.FromJson<EnemyStunSendMessage>(json);
		if (msg == null) return;

		if (msg.sender_id == myId) return;

		if (msg.enemy_index < 0)
		{
			Debug.LogWarning($"enemy_stun_cancel の enemy_index が不正です: {msg.enemy_index}");
			return;
		}

		if (_enemyObjects == null || _enemyObjects.Length == 0)
		{
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
		}

		foreach (var e in _enemyObjects)
		{
			if (e == null) continue;

			var em = e.GetComponent<EnemyManager>();
			if (em == null) continue;

			if (em.enemyID == msg.enemy_index)
			{
				em.StunCancel();
				break;
			}
		}
	}


	private void HandleSwitchActivatedMessage(string json)
	{
		var msg = JsonUtility.FromJson<SwitchActivatedMessage>(json);
		var eg = FindObjectOfType<ElementGenerator>();
		if (eg == null) return;

		foreach (var sw in eg.GetSwitchList())
		{
			if (sw.targetEnemyID == msg.switch_id)
			{
				sw.OnSwitchActivated();
				break;
			}
		}
		LogManager.Instance?.AddLog("どこかのギミックが作動した", "#ffcc44");
	}


	private void HandleItemPickedMessage(string json)
	{

		var msg = JsonUtility.FromJson<ItemPickedSendMessage>(json);

		MissionManager.Instance?.OnItemPicked();
		LogManager.Instance?.AddLog("アイテムを取得した", "#aadd44");

		ItemManager nearestItem = null;
		float nearestDist = float.MaxValue;

		foreach (var item in FindObjectsOfType<ItemManager>())
		{
			Vector2 itemXZ = new Vector2(item.transform.position.x, item.transform.position.z);
			Vector2 msgXZ = new Vector2(msg.x, msg.z);
			float dist = Vector2.Distance(itemXZ, msgXZ);

			if (dist < 2f && dist < nearestDist)
			{
				nearestDist = dist;
				nearestItem = item;
			}
		}

		if (nearestItem != null)
		{
			var generator = FindObjectOfType<ElementGenerator>();
			if (generator != null)
			{
				generator.RemoveItemIcon(nearestItem.transform.position);
			}

			Destroy(nearestItem.gameObject);
		}
	}


	private void HandleStaminaItemPickedMessage(string json)
	{
		var msg = JsonUtility.FromJson<StaminaItemPickedSendMessage>(json);

		if (msg.sender_id == myId) return;

		StaminaManager.Instance?.RecoverStamina();
		LogManager.Instance?.AddLog("仲間がスタミナアイテムを取得した", "#ffcc44");

		Vector3 itemPos = new Vector3(msg.x, 1f, msg.z);

		StaminaItemManager nearestItem = null;
		float nearestDist = float.MaxValue;
		var candidates = FindObjectsOfType<StaminaItemManager>();

		foreach (var item in candidates)
		{
			Vector2 itemXZ = new Vector2(item.transform.position.x, item.transform.position.z);
			Vector2 msgXZ = new Vector2(msg.x, msg.z);
			float dist = Vector2.Distance(itemXZ, msgXZ);

			if (dist < 2f && dist < nearestDist)
			{
				nearestDist = dist;
				nearestItem = item;
			}
		}

		//Debug.Log($"[picked受信] pos=({msg.x:F1},{msg.z:F1}) 候補数={candidates.Length} ヒット={(nearestItem != null ? nearestItem.name + " dist=" + nearestDist.ToString("F2") : "なし")}");

		if (nearestItem != null && myPlayer != null && nearestItem.absorbEffectPrefab != null)
		{
			var effect = Instantiate(nearestItem.absorbEffectPrefab, itemPos, Quaternion.identity);
			effect.Play(itemPos, myPlayer.transform, nearestItem.effectColor);

			var generator = FindObjectOfType<ElementGenerator>();
			if (generator != null)
			{
				generator.RemoveItemIcon(nearestItem.transform.position);
			}

			nearestItem.gameObject.SetActive(false);
		}
	}


	private void HandleStaminaItemDropRequestMessage(string json)
	{
		if (!IsHostPlayer()) return;

		var msg = JsonUtility.FromJson<StaminaItemDropMessage>(json);
		if (msg.sender_id == myId) return;

		var sw = FindObjectOfType<SwitchManager>();
		if (sw == null) return;

		Vector3 dropPos = new Vector3(msg.x, msg.y, msg.z);
		if (msg.drop_type == 1)
			sw.SpawnGreenItem(dropPos);
		else
			sw.SpawnBlueItem(dropPos);

		SendStaminaItemDrop(msg.drop_type, dropPos);
	}


	private void HandleStaminaItemDropMessage(string json)
	{
		var msg = JsonUtility.FromJson<StaminaItemDropMessage>(json);
		if (msg.sender_id == myId) return; // 自分のドロップは無視（すでに生成済み）

		var sw = FindObjectOfType<SwitchManager>();
		if (sw == null) return;

		Vector3 pos = new Vector3(msg.x, msg.y, msg.z);
		if (msg.drop_type == 1)
			sw.SpawnGreenItem(pos);
		else
			sw.SpawnBlueItem(pos);
	}


	private void HandleChatMessage(string json)
	{
		var msg = JsonUtility.FromJson<ChatMessage>(json);
		if (msg.sender_id == myId) return;
		QuickChatManager.Instance?.OnChatReceived(msg.message, msg.sender_name);
	}


	#endregion

	#region 送信処理

	public async void SendPosition()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		if (myPlayer == null) return;

		var pc = myPlayer.GetComponent<PlayerController>();
		string animState = pc != null ? pc.GetAnimState() : "idle";
		string trigger = pc?.lastTrigger ?? "";
		if (!string.IsNullOrEmpty(trigger) && pc != null) pc.lastTrigger = "";

		await websocket.SendText(SendMessageBuilder.PlayerMove(
			myId,
			myPlayer.transform.position,
			myPlayer.transform.rotation.eulerAngles,
			animState,
			trigger
		));
	}


	public async void SendRespawn(Vector3 pos)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.Respawn(myId, pos));
	}


	public async void SendGoal()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.Goal());
	}


	public async void SendSwitchActivated(int switchIndex)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.SwitchActivated(switchIndex));
	}


	public async void SendEnemyStun(int enemyIndex)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;

		await websocket.SendText(SendMessageBuilder.EnemyStun(enemyIndex, myId));
	}


	public async void SendEnemyStunCancel(int enemyIndex)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.EnemyStunCancel(enemyIndex, myId));
	}


	public async void SendItemPicked(Vector3 pos)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.ItemPicked(pos));
	}


	public async void SendStaminaItemPicked(Vector3 pos)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.StaminaItemPicked(myId, pos));
	}


	public async void SendStaminaItemDrop(int dropType, Vector3 pos)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.StaminaItemDrop(myId, dropType, pos));
	}


	public async void SendStaminaItemDropRequest(int dropType, Vector3 pos)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.StaminaItemDropRequest(myId, dropType, pos));
	}


	public async void SendChatMessage(string message)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.Chat(message, myId));
	}


	public async void SendEnemyFound()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.EnemyFound(myId));
	}


	public async void SendRemoteRespawn()
	{
		if (_remoteRespawnSent) return;
		_remoteRespawnSent = true;
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		await websocket.SendText(SendMessageBuilder.RemoteRespawn(myId));
		StartCoroutine(ResetRemoteRespawnFlagAfterDelay());
	}

	public async void SendStaminaState(int current, int max)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;

		await websocket.SendText(SendMessageBuilder.StaminaState(myId, current, max));
	}

	private IEnumerator ResetRemoteRespawnFlagAfterDelay()
	{
		yield return new WaitForSeconds(4f); // リスポーン時間より長く待つ
		_remoteRespawnSent = false;
	}


	#endregion

	#region 敵同期

	private void UpdateEnemySync()
	{
		if (IsHostPlayer())
		{
			if (_enemyObjects == null || _enemyObjects.Length == 0)
				_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");

			enemySendTimer += Time.deltaTime;
			if (enemySendTimer >= enemySendInterval)
			{
				enemySendTimer = 0f;
				for (int i = 0; i < _enemyObjects.Length; i++)
				{
					if (_enemyObjects[i] == null) continue;
					var pos = _enemyObjects[i].transform.position;
					var angle = _enemyObjects[i].transform.eulerAngles.y;
					SendEnemyMove(i, pos, angle);
				}
			}
		}
		else if (IsGuestPlayer())
		{
			if (_enemyObjects == null || _enemyObjects.Length == 0)
				_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");

			foreach (var kv in enemyTargetPositions)
			{
				int idx = kv.Key;
				if (idx >= _enemyObjects.Length || _enemyObjects[idx] == null) continue;
				_enemyObjects[idx].transform.position = Vector3.Lerp(
					_enemyObjects[idx].transform.position, kv.Value, Time.deltaTime * 25f);
				if (enemyTargetAngles.ContainsKey(idx))
					_enemyObjects[idx].transform.rotation = Quaternion.Lerp(
						_enemyObjects[idx].transform.rotation,
						Quaternion.Euler(0, enemyTargetAngles[idx], 0),
						Time.deltaTime * 25f);
			}
		}
	}


	private async void SendEnemyMove(int index, Vector3 pos, float angle)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;

		Color lightColor = Color.white;
		if (_enemyObjects != null && index < _enemyObjects.Length && _enemyObjects[index] != null)
		{
			var light = _enemyObjects[index].GetComponentInChildren<Light>();
			if (light != null) lightColor = light.color;
		}

		string reaction = "";
		var em = _enemyObjects[index].GetComponent<EnemyManager>();
		if (em != null) reaction = em.GetReactionState();

		Vector3 lastSound = Vector3.zero;
		if (em != null) lastSound = em.GetLastSoundPosition();

		await websocket.SendText(SendMessageBuilder.EnemyMove(index, pos, angle, lightColor, reaction, lastSound));
	}


	#endregion

	#region プレイヤー生成・削除

	private void AddOutlineMaterial(GameObject playerObj, Material outlineMaterial)
	{
		if (playerObj == null || outlineMaterial == null) return;

		var renderers = playerObj.GetComponentsInChildren<Renderer>();

		foreach (var renderer in renderers)
		{
			var materials = new List<Material>(renderer.materials);

			if (!materials.Contains(outlineMaterial))
			{
				materials.Add(outlineMaterial);
				renderer.materials = materials.ToArray();
			}
		}
	}

	private void SpawnRemotePlayer(PlayerData player)
	{
		//Debug.Log($"SpawnRemotePlayer: id={player.id}, player_number={player.player_number}");
		if (playerObjects.ContainsKey(player.id)) return;

		GameObject newPlayer = Instantiate(playerPrefab);
		newPlayer.tag = "Player" + player.player_number;

		if (player.player_number == 1)
		{
			newPlayer.GetComponentInChildren<Renderer>().material = localPlayerMaterial;
			AddOutlineMaterial(newPlayer, localPlayerOutlineMaterial);
		}
		else
		{
			newPlayer.GetComponentInChildren<Renderer>().material = remotePlayerMaterial;
			AddOutlineMaterial(newPlayer, remotePlayerOutlineMaterial);
		}

		AudioListener remoteListener = newPlayer.GetComponent<AudioListener>();
		if (remoteListener != null) Destroy(remoteListener);

		var controller = newPlayer.GetComponent<PlayerController>();
		if (controller != null) controller.isLocalPlayer = false;

		var col = newPlayer.GetComponent<Collider>();
		if (col != null) col.enabled = true;
		var rb = newPlayer.GetComponent<Rigidbody>();
		if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

		newPlayer.transform.position = new Vector3(player.position.x, player.position.y, player.position.z);
		playerObjects[player.id] = newPlayer;
		AttachNameTag(newPlayer, player.name, true);

		var eg = FindObjectOfType<ElementGenerator>();
		if (eg != null) eg.SetPlayerTransform(newPlayer.transform);
	}


	private void ClearRemotePlayers()
	{
		foreach (var obj in playerObjects.Values)
			if (obj != null) Destroy(obj);
		playerObjects.Clear();
	}


	private void AttachNameTag(GameObject playerObj, string name, bool visible)
	{
		GameObject tagObj = new GameObject("NameTag");
		tagObj.transform.SetParent(playerObj.transform, false);
		NameTag tag = tagObj.AddComponent<NameTag>();
		tag.SetName(name);
		tag.SetVisible(visible);
	}


	#endregion

	#region ユーティリティ

	// プレイヤーのリスポーン位置をElementGeneratorから受け取る。
	public void SetSpawnPosition(int playerNum, Vector3 pos)
	{
		spawnPositions[playerNum] = pos;
	}


	public Vector3 GetSpawnPosition()
	{
		if (spawnPositions.ContainsKey(myPlayerNumber))
			return spawnPositions[myPlayerNumber];
		return Vector3.zero;
	}


	public Transform GetRemotePlayerTransform()
	{
		foreach (var kv in playerObjects)
		{
			if (kv.Value != null && kv.Value != myPlayer)
				return kv.Value.transform;
		}
		return null;
	}


	private void DelayedGameStart()
	{
		MissionManager.Instance?.OnGameStart();
	}


	#endregion
}
