using UnityEngine;
using System;

/// <summary>
/// プレイヤーのスタミナを管理するクラス。
/// スタミナの消費・回復を一元管理し、どのスクリプトからでも呼び出せる。
/// 自分のスタミナは仲間がアイテムを取ることで回復する仕組み。
/// </summary>
public class StaminaManager : MonoBehaviour
{
	public static StaminaManager Instance;

	#region インスペクター設定

	[Header("スタミナ設定")]
	public int maxStamina = 10;      // 最大スタミナ（マス数）
	public int currentStamina = 10;  // 現在のスタミナ

	[Header("回復量設定")]
	public int recoverAmount = 3;    // アイテム取得時の回復量

	#endregion

	#region イベント

	/// <summary>
	/// スタミナが変化したときに発火する。UIの更新に使う。
	/// 引数：現在値・最大値
	/// </summary>
	public event Action<int, int> OnStaminaChanged;

	#endregion

	#region Unityイベント

	void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }
	}

	private void Start()
	{
		// 半透明にして霊体っぽく見せる
		var renderers = GetComponentsInChildren<Renderer>();
		foreach (var r in renderers)
		{
			foreach (var mat in r.materials)
			{
				// Transparentモードに切り替え
				mat.SetFloat("_Mode", 3);
				mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				mat.SetInt("_ZWrite", 0);
				mat.DisableKeyword("_ALPHATEST_ON");
				mat.EnableKeyword("_ALPHABLEND_ON");
				mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				mat.renderQueue = 3000;

				// アルファを下げる（0.3くらいが霊体っぽい）
				Color c = mat.color;
				c.a = 0.3f;
				mat.color = c;
			}
		}
	}

	#endregion

	#region 公開メソッド

	/// <summary>
	/// スタミナを消費する。足りない場合は失敗としてfalseを返す。
	/// スイッチ・アクションなど各スクリプトから呼ぶ。
	/// </summary>
	/// <param name="amount">消費量（マス単位）</param>
	/// <returns>消費できたかどうか</returns>
	public bool UseStamina(int amount)
	{
		if (currentStamina < amount) return false;

		currentStamina = Mathf.Max(0, currentStamina - amount);
		OnStaminaChanged?.Invoke(currentStamina, maxStamina);
		return true;
	}

	/// <summary>
	/// スタミナを回復する。仲間がアイテムを取ったときにWebSocketClientから呼ぶ。
	/// </summary>
	/// <param name="amount">回復量（マス単位）。省略時はrecoverAmountを使う。</param>
	public void RecoverStamina(int amount = -1)
	{
		int recover = amount < 0 ? recoverAmount : amount;
		currentStamina = Mathf.Min(maxStamina, currentStamina + recover);
		OnStaminaChanged?.Invoke(currentStamina, maxStamina);
	}

	/// <summary>
	/// 指定量のスタミナが残っているか確認する。
	/// アクションの可否判定に使う。
	/// </summary>
	public bool CanUseStamina(int amount)
	{
		return currentStamina >= amount;
	}

	/// <summary>
	/// 現在のスタミナを返す。
	/// </summary>
	public int GetCurrentStamina() => currentStamina;

	/// <summary>
	/// スタミナを最大まで回復する（デバッグ・初期化用）。
	/// </summary>
	public void FillStamina()
	{
		currentStamina = maxStamina;
		OnStaminaChanged?.Invoke(currentStamina, maxStamina);
	}

	#endregion
}