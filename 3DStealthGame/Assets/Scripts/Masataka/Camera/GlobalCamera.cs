using UnityEngine;

/// <summary>
/// GameScene専用カメラ。
/// プレイヤーをなめらかに追従する。
/// </summary>
public class GlobalCamera : MonoBehaviour
{
	public static GlobalCamera Instance;

	[SerializeField] private Vector3 offset = new Vector3(0, 10, -3.5f);
	[SerializeField] private Vector3 actionOffset = new Vector3(0, 8.5f, -3f);

	[SerializeField] private float smoothTime = 0.08f;
	[SerializeField] private float offsetChangeSpeed = 8f;

	private Transform _target;
	private Vector3 _velocity;
	private Vector3 _currentOffset;

	private bool isActionCamera = false;

	private void Awake()
	{
		Instance = this;
		_currentOffset = offset;
	}

	private void Start()
	{
		transform.rotation = Quaternion.Euler(75, 0, 0);
	}

	private void LateUpdate()
	{
		if (_target == null) return;

		Vector3 targetOffset = isActionCamera ? actionOffset : offset;

		_currentOffset = Vector3.Lerp(
			_currentOffset,
			targetOffset,
			offsetChangeSpeed * Time.deltaTime
		);

		Vector3 targetPos = _target.position + _currentOffset;

		transform.position = Vector3.SmoothDamp(
			transform.position,
			targetPos,
			ref _velocity,
			smoothTime
		);
	}

	public void SetTarget(Transform target)
	{
		_target = target;

		if (_target != null)
		{
			_currentOffset = offset;
			transform.position = _target.position + offset;
		}
	}

	public void ActionCameraTrue()
	{
		isActionCamera = true;
	}

	public void ActionCameraFalse()
	{
		isActionCamera = false;
	}
}