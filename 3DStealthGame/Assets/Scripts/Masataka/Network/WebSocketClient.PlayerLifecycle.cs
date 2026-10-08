using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WebSocketClient
{
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
}
