using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WebSocketClient
{
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
}
