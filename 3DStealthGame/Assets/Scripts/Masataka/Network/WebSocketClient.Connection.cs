using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WebSocketClient
{
	#region 接続管理

	private void OnWebSocketOpened() { Debug.Log("接続成功"); }
	private bool IsSocketOpen => websocket != null && websocket.State == WebSocketState.Open;

	private async Task SendTextIfConnectedAsync(string json)
	{
		if (!IsSocketOpen) return;
		await websocket.SendText(json);
	}

	private async Task CloseSocketAsync()
	{
		if (websocket == null) return;

		WebSocket socketToClose = websocket;
		websocket = null;
		socketToClose.OnMessage -= OnMessageReceived;
		socketToClose.OnOpen -= OnWebSocketOpened;

		if (socketToClose.State == WebSocketState.Open)
		{
			try { await socketToClose.Close(); }
			catch (System.Exception exception)
			{
				Debug.LogWarning($"WebSocketの切断に失敗しました: {exception.Message}");
			}
		}
		else
		{
			socketToClose.CancelConnection();
		}
	}

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
		await CloseSocketAsync();

		var ws = new WebSocket(GetServerUrl(roomId));
		ws.OnOpen += OnWebSocketOpened;
		ws.OnMessage += OnMessageReceived;
		//ws.OnError += (e) => Debug.Log($"接続エラー: {e}");
		websocket = ws;
		await ws.Connect();
	}


	public async void OnReadyButtonClicked()
	{
		string json = SendMessageBuilder.Ready(new Vector3(0f, 1f, 0f));
		await SendTextIfConnectedAsync(json);
	}


	public async void OnQuitButtonClicked()
	{
		await CloseSocketAsync();

		if (myPlayer != null) { Destroy(myPlayer); myPlayer = null; }
		ClearRemotePlayers();
		myId = null;
		spawnPositions.Clear();
		pendingMessages.Clear();
	}


	public async Task DisconnectAndReset()
	{
		await CloseSocketAsync();

		if (myPlayer != null) { Destroy(myPlayer); myPlayer = null; }
		ClearRemotePlayers();

		myId = null;
		playerName = "";
		myPlayerNumber = 0;
		spawnPositions.Clear();
		pendingMessages.Clear();
		_enemyObjects = null;
		enemyTargetPositions.Clear();
		enemyTargetAngles.Clear();
	}


	#endregion
}
