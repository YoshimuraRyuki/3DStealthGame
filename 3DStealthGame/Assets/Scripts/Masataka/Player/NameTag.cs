using UnityEngine;

/// <summary>
/// プレイヤーの頭上に名前を表示する。
/// カメラの向きに合わせて回転する。
/// </summary>
public class NameTag : MonoBehaviour
{
	#region インスペクター設定

	[SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0);

	#endregion

	#region 内部状態

	private TextMesh _textMesh;
	private MeshRenderer _meshRenderer;
	private Transform _cameraTransform;

	#endregion

	#region Unityイベント

	private void Awake()
	{
		_textMesh = gameObject.AddComponent<TextMesh>();
		_textMesh.alignment = TextAlignment.Center;
		_textMesh.anchor = TextAnchor.LowerCenter;
		_textMesh.fontSize = 40;
		_textMesh.characterSize = 0.08f;
		_textMesh.color = Color.white;

		_meshRenderer = GetComponent<MeshRenderer>();
		if (_meshRenderer != null)
		{
			_meshRenderer.sortingOrder = 10;
		}
	}

	private void Start()
	{
		_cameraTransform = Camera.main?.transform;
	}

	private void LateUpdate()
	{
		if (transform.parent == null) return;

		transform.position = transform.parent.position + offset;

		if (_cameraTransform == null && Camera.main != null)
		{
			_cameraTransform = Camera.main.transform;
		}

		if (_cameraTransform != null)
		{
			transform.forward = _cameraTransform.forward;
		}
	}

	#endregion

	#region 公開メソッド

	public void SetName(string playerName)
	{
		if (_textMesh == null) return;

		_textMesh.text = playerName;
	}

	public void SetVisible(bool visible)
	{
		if (_meshRenderer == null) return;

		_meshRenderer.enabled = visible;
	}

	public string GetName()
	{
		return _textMesh != null ? _textMesh.text : "";
	}

	#endregion
}