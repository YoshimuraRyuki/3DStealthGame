using UnityEngine;

/// <summary>
/// 敵の状態同期。ホストは状態を送信し、ゲストは受信値へ補間する。
/// </summary>
public partial class WebSocketClient
{
	private void UpdateEnemySync()
	{
		EnsureEnemyObjectsLoaded();

		if (IsHostPlayer())
		{
			SendEnemyStatesAtInterval();
			return;
		}

		if (IsGuestPlayer())
		{
			InterpolateEnemyStates();
		}
	}

	private void EnsureEnemyObjectsLoaded()
	{
		if (_enemyObjects == null || _enemyObjects.Length == 0)
		{
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
		}
	}

	private void SendEnemyStatesAtInterval()
	{
		enemySendTimer += Time.deltaTime;
		if (enemySendTimer < enemySendInterval) return;

		enemySendTimer = 0f;
		for (int index = 0; index < _enemyObjects.Length; index++)
		{
			GameObject enemy = _enemyObjects[index];
			if (enemy == null) continue;

			SendEnemyMove(index, enemy.transform.position, enemy.transform.eulerAngles.y);
		}
	}

	private void InterpolateEnemyStates()
	{
		foreach (var entry in enemyTargetPositions)
		{
			int index = entry.Key;
			if (index < 0 || index >= _enemyObjects.Length || _enemyObjects[index] == null) continue;

			Transform enemyTransform = _enemyObjects[index].transform;
			enemyTransform.position = Vector3.Lerp(
				enemyTransform.position,
				entry.Value,
				Time.deltaTime * 25f);

			if (enemyTargetAngles.TryGetValue(index, out float targetAngle))
			{
				enemyTransform.rotation = Quaternion.Lerp(
					enemyTransform.rotation,
					Quaternion.Euler(0f, targetAngle, 0f),
					Time.deltaTime * 25f);
			}
		}
	}

	private async void SendEnemyMove(int index, Vector3 position, float angle)
	{
		if (!IsSocketOpen || _enemyObjects == null || index < 0 || index >= _enemyObjects.Length) return;

		GameObject enemy = _enemyObjects[index];
		if (enemy == null) return;

		Color lightColor = Color.white;
		var enemyLight = enemy.GetComponentInChildren<Light>();
		if (enemyLight != null) lightColor = enemyLight.color;

		var enemyManager = enemy.GetComponent<EnemyManager>();
		string reaction = enemyManager != null ? enemyManager.GetReactionState() : "";
		Vector3 lastSoundPosition = enemyManager != null
			? enemyManager.GetLastSoundPosition()
			: Vector3.zero;

		await SendTextIfConnectedAsync(SendMessageBuilder.EnemyMove(
			index,
			position,
			angle,
			lightColor,
			reaction,
			lastSoundPosition));
	}
}
