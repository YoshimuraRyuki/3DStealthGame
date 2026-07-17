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

    #endregion

    #region カメラ移動処理

    /// <summary>
    /// プレイヤーの行動に合わせてカメラをズームインする処理
    /// </summary>
    public void ActionCameraTrue()
	{
		isActionCamera = true;
    }

    public void ActionCameraFalse()
    {
        isActionCamera = false;
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

        Vector3 targetPos = _target.position + (isActionCamera ? actionOffset : offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

	#endregion

	#region 公開メソッド

	public void SetTarget(Transform target)
	{
		_target = target;
	}

	#endregion
}