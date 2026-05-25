using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ItemManager : MonoBehaviour
{
	private bool isPicked = false;

	private void OnTriggerEnter(Collider other)
	{
		if (isPicked) return;

		if (other.CompareTag("Player1") || other.CompareTag("Player2"))
		{
			var wsClient = FindObjectOfType<WebSocketClient>();
			if (wsClient != null && other.gameObject == wsClient.myPlayer)
			{
				isPicked = true;
				MissionManager.Instance?.OnItemPicked();
				SendItemPickedToServer(wsClient);
				gameObject.SetActive(false);
			}
		}
	}

	private void SendItemPickedToServer(WebSocketClient wsClient)
	{
		StartCoroutine(SendRequest(wsClient));
	}

	private IEnumerator SendRequest(WebSocketClient wsClient)
	{
		string url = $"http://localhost:8080/item_picked";
		using (UnityWebRequest req = UnityWebRequest.PostWwwForm(url, ""))
		{
			yield return req.SendWebRequest();
		}
	}
}
