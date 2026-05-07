using UnityEngine;

public class GlobalCamera : MonoBehaviour
{
	public static GlobalCamera Instance;
	public Vector3 offset = new Vector3(0, 15, -8); // 真上から見下ろす

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
		// カメラを真下に向ける
		transform.rotation = Quaternion.Euler(75, 0, 0);
	}

	// WebSocketClient から自分のプレイヤー生成後に呼ぶ
	public void SetTarget(Transform target)
	{
		_target = target;
	}

	void LateUpdate()
	{
		if (_target != null)
		{
			transform.position = _target.position + offset;
		}
	}
}