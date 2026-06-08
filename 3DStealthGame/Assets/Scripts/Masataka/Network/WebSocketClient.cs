using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// 受信メッセージの定義クラス

// 接続初期化メッセージ
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

// プレイヤー移動・アニメーション同期メッセージ
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

// タイマー更新メッセージ
[System.Serializable]
public class TimerUpdateMessage
{
	public string type;           // "timer_update"
	public int time_remaining; // 残り秒数
}

// ゴール関連メッセージ
[System.Serializable]
public class GoalMessage
{
	public string type;    // "goal"
	public string id;      // ゴールしたプレイヤーID
	public string name;    // プレイヤー名
	public float elapsed; // クリアタイム（秒）
}

// 敵同期メッセージ
[System.Serializable]
public class EnemyMoveMessage
{
	public string type;      // "enemy_move"
	public int enemy_index;  // シーン内の敵インデックス
	public float x;
	public float y;
	public float z;
	public float angle;      // Y軸回転
	public float light_r;
	public float light_g;
	public float light_b;
	public float last_sound_x;
	public float last_sound_z;
	public string reaction; // "", "!", "?"
	public string sender_id;
}

// スイッチ操作同期メッセージ
[System.Serializable]
public class SwitchActivatedMessage
{
	public string type;
	public int switch_id;
}
[System.Serializable]
public class RespawnMessage
{
	public string type;
	public string id;
	public PositionData position;
}

/// <summary>
/// WebSocketによるサーバーとの通信を一元管理するクラス。
/// プレイヤーの移動・アニメーション・敵の同期・ゲームイベントの送受信を行う。
/// DontDestroyOnLoadでシーンをまたいで動作する。
/// </summary>
public class WebSocketClient : MonoBehaviour
{
	#region インスペクター設定

	public GameObject playerPrefab;
	public GameObject myPlayer;
	public string serverUrl = "ws://192.168.56.102:8080/ws?room_id=test&name=Player1";

	public Material localPlayerMaterial;  // Player1（赤）のマテリアル
	public Material remotePlayerMaterial; // Player2（青）のマテリアル

	public RoomMemberPanel roomMemberPanel;

	public string ngrokUrl = "https://rice-washer-suitcase.ngrok-free.dev";
	public ServerMode serverMode = ServerMode.VirtualBox;

	// 接続先の切り替え用列挙型
	public enum ServerMode
	{
		VirtualBox, // 仮想環境
		LocalHost,  // server.exe用
		Ngrok,      // ngrok
		Render,     // Render
		FlyIO       // Fly.io
	}

	#endregion

	#region フィールド

	private WebSocket websocket;
	public string myId;
	private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
	private bool isGameSceneLoaded;
	private List<string> pendingMessages = new List<string>();

	private float sendInterval = 0.05f; // 位置送信間隔（秒）
	private float timer = 0f;

	private string playerName;

	private Vector3 pendingSpawnPos = Vector3.zero;
	private bool hasSpawnPos = false;

	private Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();
	private Dictionary<string, Quaternion> targetRotations = new Dictionary<string, Quaternion>();

	private Dictionary<int, Vector3> spawnPositions = new Dictionary<int, Vector3>();
	public int myPlayerNumber = 0; // 1=ホスト(敵を送信), 2=ゲスト(敵を受信)
	private GameObject[] _enemyObjects; // シーン内の全敵

	private Dictionary<int, Vector3> enemyTargetPositions = new Dictionary<int, Vector3>();
	private Dictionary<int, float> enemyTargetAngles = new Dictionary<int, float>();
	private float enemySendTimer = 0f;
	private float enemySendInterval = 0.05f;

	private Vector3 _lastRemotePosition = Vector3.zero;

	//public bool useLocalServer = true; // trueでローカル、falseでRender

	private bool _stunSent = false; 

	#endregion

	#region Unityイベント

	void Awake()
	{
		Application.runInBackground = true;
		Application.targetFrameRate = 60;
		DontDestroyOnLoad(this.gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
		isGameSceneLoaded = SceneManager.GetActiveScene().name == "MapTest";
	}

	async void Start()
	{
		Application.runInBackground = true;
		playerName = "";//"Player_" + Random.Range(1000, 9999);
		websocket = new WebSocket(GetServerUrl("test"));
		websocket.OnOpen += () => Debug.Log("サーバーに接続");
		websocket.OnMessage += OnMessageReceived;
		websocket.OnError += (e) => Debug.Log("エラー: " + e);
	}

	async void Update()
	{
		// リモートプレイヤーの位置を補間移動
		foreach (var id in targetPositions.Keys)
		{
			if (id == myId) continue; // 自分は補間しない
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

	private async void OnApplicationQuit()
	{
		if (websocket != null && websocket.State == WebSocketState.Open) await websocket.Close();
	}

	#endregion

	#region 接続管理

	/// <summary>
	/// ServerModeに応じたWebSocket接続URLを返す
	/// </summary>
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
			default:
				return $"ws://192.168.56.102:8080/ws?room_id={roomId}&name={playerName}";
		}
	}

	/// <summary>
	/// シーンロード時の処理。MapTest以外ではリモートプレイヤーを削除する。
	/// </summary>
	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		isGameSceneLoaded = scene.name == "MapTest";

		Debug.Log($"OnSceneLoaded: {scene.name} isGameSceneLoaded={isGameSceneLoaded}");

		if (scene.name != "MapTest")
		{
			ClearRemotePlayers();
		}
		else
		{
			Invoke("ProcessPendingMessages", 0.5f);
			// MapTest ロード完了 → ミッションタイマースタート
			Invoke("DelayedGameStart", 0.6f);
		}
	}

	/// <summary>
	/// 指定したルームIDに接続する。既存の接続がある場合は切断してから再接続する。
	/// </summary>
	public async void ConnectToRoom(string roomId)
	{
		if (websocket != null)
		{
			try { await websocket.Close(); } catch { }
			websocket = null;
		}

		websocket = new WebSocket(GetServerUrl(roomId));
		websocket.OnOpen += OnWebSocketOpened;
		websocket.OnMessage += OnMessageReceived;
		websocket.OnError += (e) => Debug.Log("エラー: " + e);

		await websocket.Connect();
	}

	public void SetPlayerName(string name) { playerName = name; }
	public string GetPlayerName() => playerName;

	/// <summary>
	/// 退室ボタン押下時にWebSocketを切断してプレイヤーを削除する
	/// </summary>
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

	/// <summary>
	/// 準備完了ボタン押下時にサーバーへreadyを送信する
	/// </summary>
	public async void OnReadyButtonClicked()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		string json = "{\"type\":\"ready\",\"position\":{\"x\":0,\"y\":1,\"z\":0}}";
		await websocket.SendText(json);
	}

	#endregion

	#region メッセージ受信

	/// <summary>
	/// サーバーからメッセージを受信したときの処理。
	/// ゲームシーン未ロード時はキューに積んでおく。
	/// </summary>
	private void OnMessageReceived(byte[] bytes)
	{
		string json = Encoding.UTF8.GetString(bytes);
		if (json.Contains("enemy_stun"))
			Debug.Log($"OnMessageReceived: isGameSceneLoaded={isGameSceneLoaded} json={json}");

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

	/// <summary>
	/// キューに積まれたメッセージを一括処理する
	/// </summary>
	private void ProcessPendingMessages()
	{
		foreach (var json in pendingMessages) ProcessMessage(json);
		pendingMessages.Clear();
	}

	/// <summary>
	/// メッセージタイプに応じて対応するハンドラに振り分ける
	/// </summary>
	private void ProcessMessage(string json)
	{
		if (json.Contains("\"type\":\"init\"")) HandleInitMessage(json);
		else if (json.Contains("\"type\":\"existing_players\"")) HandleExistingPlayersMessage(json);
		else if (json.Contains("\"type\":\"player_joined\"")) HandlePlayerJoinedMessage(json);
		else if (json.Contains("\"type\":\"player_move\"")) HandlePlayerMoveMessage(json);
		else if (json.Contains("\"type\":\"player_left\"")) HandlePlayerLeftMessage(json);
		else if (json.Contains("\"type\":\"item_picked\"")) HandleItemPickedMessage(json);
		//else if (json.Contains("\"type\":\"goal\"")) HandleGoalMessage(json);
		else if (json.Contains("\"type\":\"timer_update\"")) HandleTimerUpdate(json);
		else if (json.Contains("\"type\":\"enemy_move\"")) HandleEnemyMoveMessage(json);
		else if (json.Contains("\"type\":\"remote_respawn\"")) HandleRemoteRespawnMessage(json);
		else if (json.Contains("\"type\":\"player_goal\"")) HandlePlayerGoalMessage(json);
		else if (json.Contains("\"type\":\"all_goal\"")) HandleAllGoalMessage(json);
		else if (json.Contains("\"type\":\"switch_activated\"")) HandleSwitchActivatedMessage(json);
		else if (json.Contains("\"type\":\"enemy_stun_cancel\"")) HandleEnemyStunCancelMessage(json);
		else if (json.Contains("\"type\":\"enemy_stun\"")) HandleEnemyStunMessage(json);
		else if (json.Contains("\"type\":\"respawn\"")) HandleRespawnMessage(json);

		else if (json.Contains("\"type\":\"start_game\""))
		{
			if (SceneManager.GetActiveScene().name == "MapTest")
			{
				ProcessPendingMessages();
				MissionManager.Instance?.OnGameStart();
			}
			else
			{
				SceneManager.LoadScene("MapTest");
			}
		}
		else if (json.Contains("\"type\":\"player_ready\""))
		{
			var msg = JsonUtility.FromJson<PlayerReadyMessage>(json);
			if (roomMemberPanel != null) roomMemberPanel.SetReady(msg.id, true);
		}
	}

	#endregion

	#region メッセージハンドラ（ゲームシーン用）

	/// <summary>
	/// 初期化メッセージ：自分のプレイヤーを生成してカメラをセットする
	/// </summary>
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
		myPlayerNumber = init.player_number;
		// 自分のミニマップだけ表示
		var eg = FindObjectOfType<ElementGenerator>();
		/*if (eg != null)
		{
			if (myPlayerNumber == 1)
				eg.ShowMiniMap(1);
			else
				eg.ShowMiniMap(2);
		}*/
		// ゲスト（player2）は敵のAIを止めてWebSocketで受信した位置で動かす
		if (myPlayerNumber == 2)
		{
			var enemies = GameObject.FindGameObjectsWithTag("Enemy");
			foreach (var e in enemies)
			{
				var em = e.GetComponent<EnemyManager>();
				if (em != null) em.isRemoteControlled = true;
			}
		}
		if (myPlayerNumber == 1)
			myPlayer.GetComponentInChildren<Renderer>().material = localPlayerMaterial; // 赤
		else
			myPlayer.GetComponentInChildren<Renderer>().material = remotePlayerMaterial; // 青

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
		{
			GlobalCamera.Instance.SetTarget(myPlayer.transform);
			//Debug.Log("カメラターゲット設定完了");
		}
		else
		{
			Debug.LogWarning("GlobalCamera.Instanceがnull");
		}

		//var eg = FindObjectOfType<ElementGenerator>();
		if (elementGenerator != null) elementGenerator.SetRemotePlayerTransform(myPlayer.transform);

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

		// 敵に自分のプレイヤーを渡す（Player1のみ）
		if (myPlayerNumber == 1)
		{
			var enemyList = GameObject.FindGameObjectsWithTag("Enemy");
			foreach (var e in enemyList)
			{
				var em = e.GetComponent<EnemyManager>();
				//if (em != null) em.SetTargetPlayer(myPlayer.transform);
			}
		}
	}

	/// <summary>
	/// 既存プレイヤー一覧：後から入室した場合に先に入っていたプレイヤーを生成する
	/// </summary>
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

	/// <summary>
	/// 新規プレイヤー参加：後から入室したプレイヤーを生成する
	/// </summary>
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

	/// <summary>
	/// プレイヤー移動：位置・回転・アニメーションを反映し、ホスト側は敵への音通知も行う
	/// </summary>
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

		// ホスト側だけリモートプレイヤーの音を敵に通知
		if (myPlayerNumber == 1 && msg.id != myId)
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
		if (anim != null && !string.IsNullOrEmpty(msg.anim_state))
		{
			anim.SetBool("Run", msg.anim_state == "run");
			anim.SetBool("Sneak", msg.anim_state == "sneak");
		}

		if (!string.IsNullOrEmpty(msg.anim_trigger))
		{
			anim.SetTrigger(msg.anim_trigger);
		}
	}

	/// <summary>
	/// プレイヤー退室：退室したプレイヤーのオブジェクトを削除する
	/// </summary>
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

	/// <summary>
	/// アイテム取得：MissionManagerに通知する（サーバーが全員にブロードキャスト）
	/// </summary>
	private void HandleItemPickedMessage(string json)
	{
		// 自分が取得した場合だけミッションフラグを立てる
		// （サーバーは全員にブロードキャストするので id で自分かどうか判定）
		/*var msg = JsonUtility.FromJson<GoalMessage>(json); // id フィールドだけ使う
		if (msg.id == myId)*/
		MissionManager.Instance?.OnItemPicked();
	}

	/// <summary>
	/// 片方がゴール：自分かどうかで待機メッセージを切り替える
	/// </summary>
	private void HandlePlayerGoalMessage(string json)
	{
		var msg = JsonUtility.FromJson<GoalMessage>(json);

		if (msg.id == myId)
		{
			// 自分がゴールした
			MissionManager.Instance?.OnGoal();
			// 「相手を待っています...」表示
			if (MissionManager.Instance != null)
				MissionManager.Instance.ShowWaitingMessage("相手を待っています...");
		}
		else
		{
			// 相手がゴールした
			// 「仲間がゴールで待っています！」表示
			if (MissionManager.Instance != null)
				MissionManager.Instance.ShowWaitingMessage("仲間がゴールで待っています！");
		}
	}

	/// <summary>
	/// 全員ゴール：クリア演出→フェード→リザルトシーンへ遷移する
	/// </summary>
	private void HandleAllGoalMessage(string json)
	{
		MissionManager.Instance?.StopTimer();
		MissionManager.Instance?.ShowClearMessage();

		if (MissionManager.Instance != null)
		{
			ResultData.elapsedTime = MissionManager.Instance.GetElapsedSeconds();
			ResultData.missionCount = MissionManager.Instance.GetClearedMissionCount();
		}
		ResultData.playerName = playerName;
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

	/// <summary>
	/// リザルトシーンへ遷移する（FadeToResult内から呼ばれる）
	/// </summary>
	private void LoadResultScene()
	{
		var msg_dummy = new GoalMessage();
		if (MissionManager.Instance != null)
		{
			ResultData.elapsedTime = MissionManager.Instance.GetElapsedSeconds();
			ResultData.missionCount = MissionManager.Instance.GetClearedMissionCount();
		}
		ResultData.playerName = playerName;
		foreach (var obj in playerObjects.Values)
		{
			var nameTag = obj.GetComponentInChildren<NameTag>();
			if (nameTag != null)
				ResultData.remotePlayerName = nameTag.GetName();
		}
		SceneManager.LoadScene("Result");
	}

	/// <summary>
	/// タイマー更新：現時点では未使用
	/// </summary>
	private void HandleTimerUpdate(string json)
	{
		var msg = JsonUtility.FromJson<TimerUpdateMessage>(json);
	}

	/// <summary>
	/// 敵移動（ゲスト側のみ処理）：位置・ライト色・リアクション・最終音検知位置を反映する
	/// </summary>
	private void HandleEnemyMoveMessage(string json)
	{
		if (myPlayerNumber == 1) return;
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

	#endregion

	#region メッセージハンドラ（ロビー用）

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

	/// <summary>
	/// スイッチ操作受信：対応するスイッチのOnSwitchActivatedを呼ぶ
	/// </summary>
	private void HandleSwitchActivatedMessage(string json)
	{
		Debug.Log($"switch_activated受信: {json}");
		var msg = JsonUtility.FromJson<SwitchActivatedMessage>(json);
		Debug.Log($"switch_id: {msg.switch_id}");
		var switches = FindObjectsOfType<SwitchManager>();
		foreach (var sw in switches)
		{
			Debug.Log($"スイッチID確認: {sw.targetEnemyID}");
			if (sw.targetEnemyID == msg.switch_id)
			{
				sw.OnSwitchActivated();
				break;
			}
		}
	}


	private void HandleEnemyStunMessage(string json)
	{
		var msg = JsonUtility.FromJson<EnemyMoveMessage>(json);
		if (msg.sender_id == myId) return;

		if (_enemyObjects == null || _enemyObjects.Length == 0)
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");

		// インデックスではなくenemyIDで該当の敵を探す
		foreach (var e in _enemyObjects)
		{
			var em = e.GetComponent<EnemyManager>();
			if (em != null && em.enemyID == msg.enemy_index)
			{
				em.PlayAnimationEnemy();
				break;
			}
		}
	}

	private void HandleEnemyStunCancelMessage(string json)
	{
		var msg = JsonUtility.FromJson<EnemyMoveMessage>(json);
		if (msg.sender_id == myId) return;

		if (_enemyObjects == null || _enemyObjects.Length == 0)
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");

		foreach (var e in _enemyObjects)
		{
			var em = e.GetComponent<EnemyManager>();
			if (em != null && em.enemyID == msg.enemy_index)
			{
				em.StunCancel();
				break;
			}
		}
	}
	#endregion

	#region プレイヤー生成・削除

	/// <summary>
	/// リモートプレイヤーを生成する。PlayerNumberに応じてマテリアルを設定する。
	/// </summary>
	private void SpawnRemotePlayer(PlayerData player)
	{
		Debug.Log($"SpawnRemotePlayer: id={player.id}, player_number={player.player_number}");
		if (playerObjects.ContainsKey(player.id)) return;

		GameObject newPlayer = Instantiate(playerPrefab);
		newPlayer.tag = "Player" + player.player_number;
		if (player.player_number == 1)
			newPlayer.GetComponentInChildren<Renderer>().material = localPlayerMaterial; // 赤
		else
			newPlayer.GetComponentInChildren<Renderer>().material = remotePlayerMaterial; // 青

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

	/// <summary>
	/// 全リモートプレイヤーのオブジェクトを削除する
	/// </summary>
	private void ClearRemotePlayers()
	{
		foreach (var obj in playerObjects.Values)
			if (obj != null) Destroy(obj);
		playerObjects.Clear();
	}

	private void HandleRespawnMessage(string json)
	{
		var msg = JsonUtility.FromJson<RespawnMessage>(json);
		if (msg.id == myId)
		{
			targetPositions.Remove(myId);
			return;
		}
		if (!playerObjects.ContainsKey(msg.id)) return;
		var obj = playerObjects[msg.id];
		if (obj == null) return;
		obj.transform.position = new Vector3(msg.position.x, msg.position.y, msg.position.z);
		targetPositions[msg.id] = obj.transform.position;
	}


	private void HandleRemoteRespawnMessage(string json)
	{
		if (myPlayerNumber != 2) return; // Player2（リモート）だけ処理する

		if (myPlayer == null) return;
		var pc = myPlayer.GetComponent<PlayerController>();
		if (pc == null) return;

		pc.RespawnWithEffectPublic();

		/*var spawnPos = GetSpawnPosition();
		myPlayer.transform.position = spawnPos != Vector3.zero ? spawnPos : myPlayer.transform.position;

		var rb = myPlayer.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}

		var enemies = GameObject.FindGameObjectsWithTag("Enemy");
		foreach (var e in enemies)
		{
			var em = e.GetComponent<EnemyManager>();
			if (em != null)
			{
				em.ResetRespawnFlag();
				em.currentAlertCount = em.alertCount;
			}
		}*/
	}

	public async void SendRemoteRespawn()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		string json = $"{{\"type\":\"remote_respawn\",\"id\":\"{myId}\"}}";
		await websocket.SendText(json);
	}
	#endregion

	#region 送信処理

	/// <summary>
	/// 自分の位置・回転・アニメーション状態をサーバーに送信する
	/// </summary>
	public async void SendPosition()
	{
		if (myPlayer == null) return;
		var pc = myPlayer.GetComponent<PlayerController>();
		string animState = pc != null ? pc.GetAnimState() : "idle";
		string trigger = pc?.lastTrigger ?? "";
		if (!string.IsNullOrEmpty(trigger) && pc != null) pc.lastTrigger = "";

		string json = $"{{\"type\":\"player_move\",\"id\":\"{myId}\"," +
					  $"\"position\":{{\"x\":{myPlayer.transform.position.x}," +
					  $"\"y\":{myPlayer.transform.position.y}," +
					  $"\"z\":{myPlayer.transform.position.z}}}," +
					  $"\"rotation\":{{\"x\":{myPlayer.transform.rotation.eulerAngles.x}," +
					  $"\"y\":{myPlayer.transform.rotation.eulerAngles.y}}}," +
					  $"\"anim_state\":\"{animState}\"," +
					  $"\"anim_trigger\":\"{trigger}\"}}";
		await websocket.SendText(json);
	}

	public async void SendRespawn(Vector3 pos)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		string json = $"{{\"type\":\"respawn\",\"id\":\"{myId}\",\"position\":{{\"x\":{pos.x},\"y\":{pos.y},\"z\":{pos.z}}}}}";
		await websocket.SendText(json);
	}

	/// <summary>
	/// ゴールをサーバーに通知する
	/// </summary>
	public async void SendGoal()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		string json = $"{{\"type\":\"goal\"}}";
		await websocket.SendText(json);
	}

	/// <summary>
	/// スイッチ操作をサーバーに通知する
	/// </summary>
	public async void SendSwitchActivated(int switchIndex)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		string json = $"{{\"type\":\"switch_activated\",\"switch_id\":{switchIndex}}}";
		await websocket.SendText(json);
	}

	/// <summary>
	/// アイテム取得をサーバーに通知する
	/// </summary>
	public async void SendItemPicked()
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		string json = $"{{\"type\":\"item_picked\"}}";
		await websocket.SendText(json);
	}

	/// <summary>
	/// 敵のスタン処理の同期用
	/// </summary>
	/// <param name="enemyIndex"></param>
	public async void SendEnemyStun(int enemyIndex)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		Debug.Log($"SendEnemyStun送信: enemyIndex={enemyIndex}"); // 追加
		string json = $"{{\"type\":\"enemy_stun\",\"enemy_index\":{enemyIndex},\"sender_id\":\"{myId}\"}}";
		await websocket.SendText(json);
	}

	public async void SendEnemyStunCancel(int enemyIndex)
	{
		if (websocket == null || websocket.State != WebSocketState.Open) return;
		string json = $"{{\"type\":\"enemy_stun_cancel\",\"enemy_index\":{enemyIndex},\"sender_id\":\"{myId}\"}}";
		await websocket.SendText(json);
	}
	#endregion

	#region 敵同期

	/// <summary>
	/// ホストは敵の位置を送信、ゲストは受信した位置に補間移動させる
	/// </summary>
	private void UpdateEnemySync()
	{
		if (myPlayerNumber == 1)
		{
			// ホスト：敵の位置を送信
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
		else if (myPlayerNumber == 2)
		{
			// ゲスト：受信した位置に補間移動
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

	/// <summary>
	/// 敵の位置・ライト色・リアクション・最終音検知位置をサーバーに送信する
	/// </summary>
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

		string json = $"{{\"type\":\"enemy_move\",\"enemy_index\":{index}," +
			$"\"x\":{pos.x},\"y\":{pos.y},\"z\":{pos.z},\"angle\":{angle}," +
			$"\"light_r\":{lightColor.r},\"light_g\":{lightColor.g},\"light_b\":{lightColor.b}," +
			$"\"reaction\":\"{reaction}\"," +
			$"\"last_sound_x\":{lastSound.x},\"last_sound_z\":{lastSound.z}}}";

		await websocket.SendText(json);
	}

	#endregion

	#region ユーティリティ

	/// <summary>
	/// ゲーム開始を少し遅らせてMissionManagerに通知する（シーンロード直後の遅延対策）
	/// </summary>
	private void DelayedGameStart()
	{
		MissionManager.Instance?.OnGameStart();
		Debug.Log("DelayedGameStart: MissionManager.OnGameStart()呼び出し");
	}

	/// <summary>
	/// プレイヤーの頭上に名前タグを生成してアタッチする
	/// </summary>
	private void AttachNameTag(GameObject playerObj, string name, bool visible)
	{
		GameObject tagObj = new GameObject("NameTag");
		tagObj.transform.SetParent(playerObj.transform, false);
		NameTag tag = tagObj.AddComponent<NameTag>();
		tag.SetName(name);
		tag.SetVisible(visible);
	}

	// 竜希のスクリプトと連携用。消さないように注意。
	public void SetSpawnPosition(int playerNum, Vector3 pos) { spawnPositions[playerNum] = pos; }

	/// <summary>
	/// 自分のスポーン座標を返す（リスポーン時などに使用）
	/// </summary>
	public Vector3 GetSpawnPosition()
	{
		if (spawnPositions.ContainsKey(myPlayerNumber))
			return spawnPositions[myPlayerNumber];
		return Vector3.zero;
	}

	#endregion
}