using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スタミナゲージのUIを管理するクラス。
/// StaminaManagerのイベントを受け取り、マスの表示を切り替える。
/// </summary>
public class StaminaGaugeUI : MonoBehaviour
{
	#region インスペクター設定

	[Header("マス画像（Sprite）")]
	public Sprite cellYellow;  // 満タン時
	public Sprite cellOrange;  // 中間時
	public Sprite cellRed;     // 残り少ない時
	public Sprite cellEmpty;   // 空マス

	[Header("マスのImageコンポーネント一覧（左から順に）")]
	public Image[] cells;

	[Header("色の切り替えしきい値")]
	public float orangeThreshold = 0.6f; // この割合以下でオレンジ
	public float redThreshold = 0.3f;    // この割合以下で赤

	#endregion

	#region Unityイベント

	void Start()
	{
		if (StaminaManager.Instance != null)
		{
			StaminaManager.Instance.OnStaminaChanged += RefreshGauge;
			RefreshGauge(StaminaManager.Instance.GetCurrentStamina(), cells.Length);
		}
	}

	void OnDestroy()
	{
		if (StaminaManager.Instance != null)
			StaminaManager.Instance.OnStaminaChanged -= RefreshGauge;
	}

	#endregion

	#region UI更新

	/// <summary>
	/// スタミナの値に応じてマスの画像を切り替える。
	/// </summary>
	private void RefreshGauge(int current, int max)
	{
		if (cells == null || cells.Length == 0) return;

		float ratio = max > 0 ? (float)current / max : 0f;

		// 現在の割合に応じてアクティブマスのスプライトを決定
		Sprite activeSprite;
		if (ratio <= redThreshold)
			activeSprite = cellRed;
		else if (ratio <= orangeThreshold)
			activeSprite = cellOrange;
		else
			activeSprite = cellYellow;

		for (int i = 0; i < cells.Length; i++)
		{
			if (cells[i] == null) continue;

			if (i < current)
				cells[i].sprite = activeSprite;
			else
				cells[i].sprite = cellEmpty;
		}
	}

	#endregion
}