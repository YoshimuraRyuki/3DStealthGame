using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;


/// <summary>
/// ゲーム中のWebSocket通信をまとめて扱う。
/// ルーム接続、プレイヤー同期、敵同期、ゲーム進行通知を担当。
/// </summary>
public partial class WebSocketClient : MonoBehaviour
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

	private readonly Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
	private readonly Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();
	private readonly Dictionary<string, Quaternion> targetRotations = new Dictionary<string, Quaternion>();
	private readonly Dictionary<int, Vector3> spawnPositions = new Dictionary<int, Vector3>();

	private bool isGameSceneLoaded;
	private readonly List<string> pendingMessages = new List<string>();

	private float sendInterval = 0.05f;
	private float timer = 0f;
	private string playerName;

	private string currentRoomId = "";

	public int myPlayerNumber = 0; // 1=ホスト, 2=ゲスト

	private GameObject[] _enemyObjects;
	private readonly Dictionary<int, Vector3> enemyTargetPositions = new Dictionary<int, Vector3>();
	private readonly Dictionary<int, float> enemyTargetAngles = new Dictionary<int, float>();
	private float enemySendTimer = 0f;
	private float enemySendInterval = 0.05f;

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
		await CloseSocketAsync();
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
