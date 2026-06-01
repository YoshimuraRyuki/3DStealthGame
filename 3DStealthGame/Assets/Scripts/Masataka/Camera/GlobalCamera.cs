using UnityEngine;
using UnityEngine.UIElements;

public class GlobalCamera : MonoBehaviour
{
	public static GlobalCamera Instance;
	public Vector3 offset = new Vector3(0, 15, -5); // 真上から見下ろす

	private Transform _target;

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void Start()
	{
		transform.rotation = Quaternion.Euler(75, 0, 0);

		if (_target == null)
		{
			var player = GameObject.FindWithTag("Player1");
			if (player != null)
			{
				SetTarget(player.transform);
				Debug.Log("★GlobalCamera: Player1を自動検出してターゲット設定");
			}
			else
			{
				Debug.LogWarning("⚠️GlobalCamera: Player1が見つかりません");
			}
		}
	}

	// WebSocketClient から自分のプレイヤー生成後に呼ぶ
	public void SetTarget(Transform target)
	{
		_target = target;
	}

	/*void LateUpdate()
	{
		if (_target != null)
		{
			Vector3 targetPos = _target.position + offset;
			transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 25f);
		}
	}*/
	void LateUpdate()
	{
		if (_target != null)
		{
			transform.position = _target.position + offset;
		}
	}
}