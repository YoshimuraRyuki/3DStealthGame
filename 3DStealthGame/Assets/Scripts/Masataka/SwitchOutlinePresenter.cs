using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 操作・取得できる対象にアウトラインを付ける。
/// targetPlayerNumber は 0=全員, 1=Player1, 2=Player2。
/// </summary>
public class SwitchOutlinePresenter : MonoBehaviour
{
	[Header("アウトライン用マテリアル")]
	[SerializeField] private Material outlineMaterial;

	private int _targetPlayerNumber = -1;
	private bool _initialized = false;

	private WebSocketClient _wsClient;

	public void SetTargetPlayerNumber(int playerNumber)
	{
		_targetPlayerNumber = playerNumber;
		_initialized = false;
	}

	private void Start()
	{
		_wsClient = FindObjectOfType<WebSocketClient>();
	}

	private void Update()
	{
		if (_initialized) return;
		if (_targetPlayerNumber < 0) return;

		TryInitialize();
	}

	private void TryInitialize()
	{
		if (_wsClient == null)
		{
			_wsClient = FindObjectOfType<WebSocketClient>();
		}

		if (_wsClient == null || _wsClient.myPlayerNumber == 0) return;

		_initialized = true;

		if (CanUse(_wsClient.myPlayerNumber))
		{
			SetOutlineAppearance();
		}
	}

	private bool CanUse(int myPlayerNumber)
	{
		return _targetPlayerNumber == 0 || myPlayerNumber == _targetPlayerNumber;
	}

	private void SetOutlineAppearance()
	{
		if (outlineMaterial == null) return;

		foreach (var renderer in GetComponentsInChildren<Renderer>())
		{
			var materials = new List<Material>(renderer.sharedMaterials);

			if (materials.Contains(outlineMaterial)) continue;

			materials.Add(outlineMaterial);
			renderer.materials = materials.ToArray();
		}
	}
}