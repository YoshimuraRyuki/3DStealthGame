using UnityEngine;

/// <summary>
/// 通常アイテムの取得処理。
/// 自分のプレイヤーが触れたときだけ、サーバーへ取得通知を送る。
/// </summary>
public class ItemManager : MonoBehaviour
{
	#region フィールド

	private bool _isPicked = false;
	private WebSocketClient _wsClient;
	private ElementGenerator _elementGenerator;

	#endregion

	#region Unityイベント

	private void Start()
	{
		_wsClient = FindObjectOfType<WebSocketClient>();
		_elementGenerator = FindObjectOfType<ElementGenerator>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_isPicked) return;
		if (!IsPlayer(other)) return;

		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null || other.gameObject != _wsClient.myPlayer) return;

		_isPicked = true;

		SoundManager.Instance?.PlayPickup();
		_wsClient.SendItemPicked(transform.position);

		if (_elementGenerator == null)
		{
			_elementGenerator = FindObjectOfType<ElementGenerator>();
		}

		if (_elementGenerator != null)
		{
			_elementGenerator.RemoveItemIcon(transform.position);
		}

		Destroy(gameObject);
	}

	#endregion

	#region 判定

	private bool IsPlayer(Collider other)
	{
		return other.CompareTag("Player1") || other.CompareTag("Player2");
	}

	#endregion
}