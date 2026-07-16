using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スタミナ回復アイテムの取得処理。
/// 指定されたプレイヤーだけが取得できる。
/// </summary>
public class StaminaItemManager : MonoBehaviour
{
	#region インスペクター設定

	[Header("取得できるプレイヤー番号")]
	public int targetPlayerNumber = 1;

	[Header("取れないときの表示")]
	public Material ghostMaterial;

	[Header("吸い込みエフェクト")]
	public StaminaAbsorbEffect absorbEffectPrefab;
	public Color effectColor = Color.blue;

	[Header("取れるときのアウトライン")]
	public Material outlineMaterial;

	#endregion

	#region 内部状態

	private bool _isPicked = false;
	private bool _initialized = false;

	private WebSocketClient _wsClient;
	private ElementGenerator _elementGenerator;

	#endregion

	#region Unityイベント

	private void Start()
	{
		_wsClient = FindObjectOfType<WebSocketClient>();
		_elementGenerator = FindObjectOfType<ElementGenerator>();
	}

	private void Update()
	{
		if (_initialized) return;

		TryInitializeAppearance();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_isPicked) return;
		if (!IsPlayer(other)) return;

		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null) return;
		if (other.gameObject != _wsClient.myPlayer) return;
		if (_wsClient.myPlayerNumber != targetPlayerNumber) return;

		// このアイテムは「拾った人」ではなく「味方」を回復するため、
		// 味方のスタミナが満タンなら拾えない
		if (!_wsClient.CanRemoteRecoverStamina())
		{
			LogManager.Instance?.AddLog("味方のスタミナが満タンなので取れない", "#ffcc44");
			return;
		}

		PickItem();
	}

	#endregion

	#region 初期化

	private void TryInitializeAppearance()
	{
		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null || _wsClient.myPlayerNumber == 0) return;

		_initialized = true;

		if (_wsClient.myPlayerNumber == targetPlayerNumber)
		{
			SetOutlineAppearance();
		}
		else
		{
			SetGhostAppearance();
		}
	}

	#endregion

	#region 取得処理

	private void PickItem()
	{
		_isPicked = true;

		SoundManager.Instance?.PlayPickup();

		Vector3 pickedPosition = transform.position;
		Transform remoteTransform = _wsClient.GetRemotePlayerTransform();

		if (_elementGenerator == null)
		{
			_elementGenerator = FindObjectOfType<ElementGenerator>();
		}

		if (_elementGenerator != null)
		{
			_elementGenerator.RemoveItemIcon(pickedPosition);
		}

		_wsClient.SendStaminaItemPicked(pickedPosition);
		PlayMetrics.AddStaminaItem();

		if (absorbEffectPrefab != null && remoteTransform != null)
		{
			var effect = Instantiate(absorbEffectPrefab, pickedPosition, Quaternion.identity);
			effect.Play(pickedPosition, remoteTransform, effectColor);
		}

		gameObject.SetActive(false);
	}

	#endregion

	#region 見た目

	private void SetGhostAppearance()
	{
		if (ghostMaterial == null) return;

		foreach (var renderer in GetComponentsInChildren<Renderer>())
		{
			renderer.material = ghostMaterial;
		}
	}

	private void SetOutlineAppearance()
	{
		if (outlineMaterial == null) return;

		foreach (var renderer in GetComponentsInChildren<Renderer>())
		{
			var materials = new List<Material>(renderer.sharedMaterials);

			if (!materials.Contains(outlineMaterial))
			{
				materials.Add(outlineMaterial);
				renderer.materials = materials.ToArray();
			}
		}
	}

	#endregion

	#region 判定

	private bool IsPlayer(Collider other)
	{
		return other.CompareTag("Player1") || other.CompareTag("Player2");
	}

	#endregion
}