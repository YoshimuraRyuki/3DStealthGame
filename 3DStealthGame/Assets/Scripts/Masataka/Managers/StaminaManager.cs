using UnityEngine;
using System;

/// <summary>
/// プレイヤーのスタミナを管理する。
/// 消費・回復を行い、変化したらUIへ通知する。
/// </summary>
public class StaminaManager : MonoBehaviour
{
	public static StaminaManager Instance;

	#region インスペクター設定

	[Header("スタミナ設定")]
	public int maxStamina = 10;
	public int currentStamina = 10;

	[Header("回復量")]
	public int recoverAmount = 3;

	#endregion

	#region イベント

	public event Action<int, int> OnStaminaChanged;

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

		maxStamina = Mathf.Max(1, maxStamina);
		currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
	}

	private void Start()
	{
		NotifyStaminaChanged();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	#endregion

	#region 公開メソッド

	public bool UseStamina(int amount)
	{
		if (amount <= 0) return true;
		if (currentStamina < amount) return false;

		currentStamina = Mathf.Max(0, currentStamina - amount);
		NotifyStaminaChanged();

		return true;
	}

	public void RecoverStamina(int amount = -1)
	{
		int recover = amount < 0 ? recoverAmount : amount;
		if (recover <= 0) return;

		currentStamina = Mathf.Min(maxStamina, currentStamina + recover);
		NotifyStaminaChanged();
	}

	public bool CanUseStamina(int amount)
	{
		if (amount <= 0) return true;

		return currentStamina >= amount;
	}

	public int GetCurrentStamina()
	{
		return currentStamina;
	}

	public void FillStamina()
	{
		currentStamina = maxStamina;
		NotifyStaminaChanged();
	}

	#endregion

	#region 内部処理

	private void NotifyStaminaChanged()
	{
		OnStaminaChanged?.Invoke(currentStamina, maxStamina);
	}

	#endregion
}