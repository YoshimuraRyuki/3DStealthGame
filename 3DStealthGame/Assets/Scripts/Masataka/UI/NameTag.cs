using UnityEngine;

public class NameTag : MonoBehaviour
{
	public Vector3 offset = new Vector3(0, 2.2f, 0);

	private TextMesh _textMesh;
	private Transform _cameraTransform;

	void Awake()
	{
		_textMesh = gameObject.AddComponent<TextMesh>();
		_textMesh.alignment = TextAlignment.Center;
		_textMesh.anchor = TextAnchor.LowerCenter;
		_textMesh.fontSize = 40;
		_textMesh.characterSize = 0.08f;
		_textMesh.color = Color.white;

		// 縁取り用にMeshRendererのsortingOrderを上げる
		GetComponent<MeshRenderer>().sortingOrder = 10;
	}

	void Start()
	{
		_cameraTransform = Camera.main?.transform;
	}

	public void SetName(string playerName)
	{
		_textMesh.text = playerName;
	}

	public void SetVisible(bool visible)
	{
		_textMesh.gameObject.SetActive(visible);
	}

	void LateUpdate()
	{
		// 親プレイヤーの頭上に追従
		transform.position = transform.parent.position + offset;

		// カメラに向き続ける（ビルボード）
		if (_cameraTransform != null)
			transform.forward = _cameraTransform.forward;
	}
}