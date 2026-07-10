using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スタミナゲージの表示を更新する。
/// StaminaManagerのイベントを受け取り、マスの見た目を切り替える。
/// </summary>
public class StaminaGaugeUI : MonoBehaviour
{
	#region インスペクター設定

	[Header("マス画像")]
	public Sprite cellYellow;
	public Sprite cellOrange;
	public Sprite cellRed;
	public Sprite cellEmpty;

	[Header("マスのImage一覧")]
	public Image[] cells;

	[Header("色のしきい値")]
	public float orangeThreshold = 0.6f;
	public float redThreshold = 0.3f;

	#endregion

	#region 内部状態

	private StaminaManager _staminaManager;

	#endregion

	#region Unityイベント

	private void Start()
	{
		_staminaManager = StaminaManager.Instance;

		if (_staminaManager == null) return;

		_staminaManager.OnStaminaChanged += RefreshGauge;
		RefreshGauge(_staminaManager.GetCurrentStamina(), _staminaManager.maxStamina);
	}

	private void OnDestroy()
	{
		if (_staminaManager != null)
		{
			_staminaManager.OnStaminaChanged -= RefreshGauge;
		}
	}

	#endregion

	#region UI更新

	private void RefreshGauge(int current, int max)
	{
		if (cells == null || cells.Length == 0) return;

		current = Mathf.Clamp(current, 0, cells.Length);

		float ratio = max > 0 ? (float)current / max : 0f;
		Sprite activeSprite = GetActiveSprite(ratio);

		for (int i = 0; i < cells.Length; i++)
		{
			if (cells[i] == null) continue;

			cells[i].sprite = i < current ? activeSprite : cellEmpty;
		}
	}

	private Sprite GetActiveSprite(float ratio)
	{
		if (ratio <= redThreshold)
		{
			return cellRed;
		}

		if (ratio <= orangeThreshold)
		{
			return cellOrange;
		}

		return cellYellow;
	}

	#endregion
}