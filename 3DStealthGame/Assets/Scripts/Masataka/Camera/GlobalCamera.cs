using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// プレイヤーを追いかけるカメラ。
/// プレイヤーは後から生成されるため、外部から追従対象を設定できるようにしている。
/// </summary>
public class GlobalCamera : MonoBehaviour
{
	#region フィールド

	public static GlobalCamera Instance;

	[SerializeField] private Vector3 offset = new Vector3(0, 15, -5);
	[SerializeField] private Vector3 actionOffset = new Vector3(0, 10, -5);

	private Transform _target;

	[SerializeField] float moveSpeed = 8f;
	bool isActionCamera = false;
	bool isZoomOut = false;
    public bool IsTransitioning => isActionCamera || isZoomOut;

    private Vector3 currentVelocity;

    #endregion

    #region カメラ移動処理

    /// <summary>
    /// プレイヤーの行動に合わせてカメラをズームインする処理
    /// </summary>
    public void ActionCameraTrue()
	{
		isActionCamera = true;
		isZoomOut = false;
        currentVelocity = Vector3.zero;
    }

	public void ActionCameraFalse()
	{
		isActionCamera = false;
        isZoomOut = true;
		currentVelocity = Vector3.zero;
    }

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

        // ズームイン
        if (isActionCamera)
        {
            Vector3 targetPos = _target.position + actionOffset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, 0.15f);
            return;
        }
        if (isZoomOut)
        {
            Vector3 targetPos = _target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, 0.15f);

            // 元の位置に十分近づいたらズームアウト完了
            if (Vector3.Distance(transform.position, targetPos) < 0.05f)
            {
                transform.position = targetPos;
                isZoomOut = false; // これで IsTransitioning が false になり、操作が解禁される！
            }
            return;
        }
        // 通常時は完全固定
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