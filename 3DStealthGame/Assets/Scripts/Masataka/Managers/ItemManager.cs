using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// アイテム取得判定を管理するクラス。
/// 自分のプレイヤーがアイテムに触れたらWebSocket経由でサーバーに通知する。
/// MissionManagerへの通知はサーバーからのブロードキャストで行う。
/// </summary>
public class ItemManager : MonoBehaviour
{
	#region フィールド

	private bool isPicked = false; // 取得済みフラグ（二重取得防止）

	#endregion

	#region Unityイベント

	private void OnTriggerEnter(Collider other)
	{
		if (isPicked) return;
		if (other.CompareTag("Player1") || other.CompareTag("Player2"))
		{
			var wsClient = FindObjectOfType<WebSocketClient>();
			if (wsClient != null && other.gameObject == wsClient.myPlayer)
			{
				isPicked = true;
				gameObject.SetActive(false);
				wsClient.SendItemPicked(); // WebSocketで送信、MissionManagerはサーバーから呼ぶ
			}
		}
	}

	#endregion

	#region 旧HTTP送信（未使用）

	// WebSocket実装前に使用していたHTTP送信処理（現在は未使用）
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

	#endregion
}