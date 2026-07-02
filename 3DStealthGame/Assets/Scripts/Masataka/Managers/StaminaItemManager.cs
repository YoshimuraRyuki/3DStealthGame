using UnityEngine;

/// <summary>
/// スタミナ回復アイテムの取得判定を管理するクラス。
/// 指定したプレイヤーだけが取得できる。
/// 自分が取れないアイテムは霊体用マテリアルで半透明表示される。
/// </summary>
public class StaminaItemManager : MonoBehaviour
{
	#region インスペクター設定

	[Header("何番のプレイヤーが取れるか（1 or 2）")]
	public int targetPlayerNumber = 1;

	[Header("取れないときに使う霊体用マテリアル")]
	public Material ghostMaterial;

	#endregion

	#region フィールド

	private bool _isPicked = false;
	private bool _initialized = false;
	#endregion

	#region Unityイベント

	private void Update()
	{
		if (_initialized) return;

		var wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient == null || wsClient.myPlayerNumber == 0) return;

		_initialized = true;

		if (wsClient.myPlayerNumber != targetPlayerNumber)
			SetGhostAppearance();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_isPicked) return;
		if (!other.CompareTag("Player1") && !other.CompareTag("Player2")) return;

		var wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient == null || other.gameObject != wsClient.myPlayer) return;

		// 自分が取れるアイテムじゃなければ無視
		if (wsClient.myPlayerNumber != targetPlayerNumber) return;

		_isPicked = true;
		SoundManager.Instance?.PlayPickup();
		gameObject.SetActive(false);
		wsClient.SendStaminaItemPicked(transform.position); 
	}

	#endregion

	#region 内部処理

	/// <summary>
	/// 全Rendererのマテリアルを霊体用に差し替える
	/// </summary>
	private void SetGhostAppearance()
	{
		if (ghostMaterial == null) return;

		foreach (var r in GetComponentsInChildren<Renderer>())
		{
			r.material = ghostMaterial;
		}
	}

	#endregion
}