using UnityEngine;

/// <summary>
/// スタミナ回復アイテムの取得判定を管理するクラス。
/// 指定したプレイヤーだけが取得できる。
/// 自分が取れないアイテムは半透明で表示される。
/// </summary>
public class StaminaItemManager : MonoBehaviour
{
	#region インスペクター設定

	[Header("何番のプレイヤーが取れるか（1 or 2）")]
	public int targetPlayerNumber = 1;

	[Header("取れないときの透明度（0〜1）")]
	public float ghostAlpha = 0.3f;

	#endregion

	#region フィールド

	private bool _isPicked = false;

	#endregion

	#region Unityイベント

	private void Start()
	{
		var wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient == null) return;

		// 自分が取れないアイテムは半透明にする
		if (wsClient.myPlayerNumber != targetPlayerNumber)
		{
			SetGhostAppearance();
		}
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
		gameObject.SetActive(false);
		wsClient.SendStaminaItemPicked();
	}

	#endregion

	#region 内部処理

	/// <summary>
	/// 取れないアイテムを半透明にする
	/// </summary>
	private void SetGhostAppearance()
	{
		foreach (var r in GetComponentsInChildren<Renderer>())
		{
			foreach (var mat in r.materials)
			{
				mat.SetFloat("_Mode", 3);
				mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				mat.SetInt("_ZWrite", 0);
				mat.DisableKeyword("_ALPHATEST_ON");
				mat.EnableKeyword("_ALPHABLEND_ON");
				mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				mat.renderQueue = 3000;

				Color c = mat.color;
				c.a = ghostAlpha;
				mat.color = c;
			}
		}
	}

	#endregion
}