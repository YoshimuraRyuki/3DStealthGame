using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// プレイヤーを追従するカメラの制御クラス。
/// シーンをまたいでも破棄されず、WebSocketClientからターゲットを設定できる。
/// </summary>
public class GlobalCamera : MonoBehaviour
{
	#region フィールド

	public static GlobalCamera Instance;
	public Vector3 offset = new Vector3(0, 15, -5); // カメラとプレイヤーの相対位置

	private Transform _target; // 追従対象のTransform

	#endregion

	#region Unityイベント

	void Awake()
	{
		// シングルトン設定
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
		// カメラの向きを初期化（見下ろし視点）
		transform.rotation = Quaternion.Euler(75, 0, 0);

		// エディタ直接起動時のフォールバック：Player1を自動検出
		if (_target == null)
		{
			var player = GameObject.FindWithTag("Player1");
			if (player != null)
			{
				SetTarget(player.transform);
				//Debug.Log("★GlobalCamera: Player1を自動検出してターゲット設定");
			}
			else
			{
				//Debug.LogWarning("⚠️GlobalCamera: Player1が見つかりません");
			}
		}
	}

	void LateUpdate()
	{
		// ターゲットの位置にオフセットを加算してカメラを追従させる
		if (_target != null)
		{
			transform.position = _target.position + offset;
		}
	}

	#endregion

	#region 公開メソッド

	/// <summary>
	/// 追従対象を設定する。WebSocketClientからプレイヤー生成後に呼ぶ。
	/// </summary>
	/// <param name="target">追従させるTransform</param>
	public void SetTarget(Transform target)
	{
		_target = target;
	}

	#endregion
}