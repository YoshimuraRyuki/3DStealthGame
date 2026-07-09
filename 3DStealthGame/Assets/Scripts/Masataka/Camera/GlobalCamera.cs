using UnityEngine;

/// <summary>
/// プレイヤーを追いかけるカメラ。
/// プレイヤーは後から生成されるため、外部から追従対象を設定できるようにしている。
/// </summary>
public class GlobalCamera : MonoBehaviour
{
	#region フィールド

	public static GlobalCamera Instance;

	[SerializeField] private Vector3 offset = new Vector3(0, 15, -5);

	private Transform _target;

	#endregion

	#region Unityイベント

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		transform.rotation = Quaternion.Euler(75, 0, 0);

		if (_target == null)
		{
			var player = GameObject.FindWithTag("Player1");
			if (player != null)
			{
				SetTarget(player.transform);
			}
		}
	}

	private void LateUpdate()
	{
		if (_target == null) return;

		transform.position = _target.position + offset;
	}

	#endregion

	#region 公開メソッド

	public void SetTarget(Transform target)
	{
		_target = target;
	}

	#endregion
}