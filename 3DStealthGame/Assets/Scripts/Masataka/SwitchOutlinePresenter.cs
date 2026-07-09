using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 操作・取得できる対象だけアウトライン表示する。
/// 0 = 全員、1 = Player1、2 = Player2
/// </summary>
public class SwitchOutlinePresenter : MonoBehaviour
{
	[Header("操作・取得できるときに使うアウトライン用マテリアル")]
	[SerializeField] private Material outlineMaterial;

	private int _targetPlayerNumber = -1; // -1=未設定, 0=全員, 1=Player1, 2=Player2
	private bool _initialized = false;

	/// <summary>
	/// 0=全員, 1=Player1, 2=Player2
	/// </summary>
	public void SetTargetPlayerNumber(int playerNumber)
	{
		_targetPlayerNumber = playerNumber;
		_initialized = false;
	}

	private void Update()
	{
		if (_initialized) return;
		if (_targetPlayerNumber < 0) return;

		var wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient == null || wsClient.myPlayerNumber == 0) return;

		_initialized = true;

		if (_targetPlayerNumber == 0 || wsClient.myPlayerNumber == _targetPlayerNumber)
		{
			SetOutlineAppearance();
		}
	}

	private void SetOutlineAppearance()
	{
		if (outlineMaterial == null) return;

		foreach (var r in GetComponentsInChildren<Renderer>())
		{
			var mats = new List<Material>(r.sharedMaterials);

			if (!mats.Contains(outlineMaterial))
			{
				mats.Add(outlineMaterial);
				r.materials = mats.ToArray();
			}
		}
	}
}