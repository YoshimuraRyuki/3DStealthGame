using UnityEngine;
using NativeWebSocket;
using System.Text;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WebSocketClient
{
	#region 敵・ギミック・アイテム同期

	private void HandleEnemyMoveMessage(string json)
	{
		if (IsHostPlayer()) return;
		var msg = JsonUtility.FromJson<EnemyMoveMessage>(json);
		enemyTargetPositions[msg.enemy_index] = new Vector3(msg.x, msg.y, msg.z);
		enemyTargetAngles[msg.enemy_index] = msg.angle;

		if (_enemyObjects == null || _enemyObjects.Length == 0)
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");

		if (msg.enemy_index < _enemyObjects.Length)
		{
			var light = _enemyObjects[msg.enemy_index].GetComponentInChildren<Light>();
			if (light != null)
				light.color = new Color(msg.light_r, msg.light_g, msg.light_b);

			var em = _enemyObjects[msg.enemy_index].GetComponent<EnemyManager>();
			if (em != null)
			{
				em.SetReactionState(msg.reaction);
				em.SetLastSoundPosition(new Vector3(msg.last_sound_x, 0, msg.last_sound_z));
			}
		}
	}


	private void HandleEnemyStunMessage(string json)
	{
		var msg = JsonUtility.FromJson<EnemyStunSendMessage>(json);
		if (msg == null) return;

		if (msg.sender_id == myId) return;

		if (msg.enemy_index < 0)
		{
			Debug.LogWarning($"enemy_stun の enemy_index が不正です: {msg.enemy_index}");
			return;
		}

		if (_enemyObjects == null || _enemyObjects.Length == 0)
		{
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
		}

		foreach (var e in _enemyObjects)
		{
			if (e == null) continue;

			var em = e.GetComponent<EnemyManager>();
			if (em == null) continue;

			if (em.enemyID == msg.enemy_index)
			{
				em.PlayAnimationEnemy();
				break;
			}
		}
	}


	private void HandleEnemyStunCancelMessage(string json)
	{
		var msg = JsonUtility.FromJson<EnemyStunSendMessage>(json);
		if (msg == null) return;

		if (msg.sender_id == myId) return;

		if (msg.enemy_index < 0)
		{
			Debug.LogWarning($"enemy_stun_cancel の enemy_index が不正です: {msg.enemy_index}");
			return;
		}

		if (_enemyObjects == null || _enemyObjects.Length == 0)
		{
			_enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
		}

		foreach (var e in _enemyObjects)
		{
			if (e == null) continue;

			var em = e.GetComponent<EnemyManager>();
			if (em == null) continue;

			if (em.enemyID == msg.enemy_index)
			{
				em.StunCancel();
				break;
			}
		}
	}


	private void HandleSwitchActivatedMessage(string json)
	{
		var msg = JsonUtility.FromJson<SwitchActivatedMessage>(json);
		var eg = FindObjectOfType<ElementGenerator>();
		if (eg == null) return;

		foreach (var sw in eg.GetSwitchList())
		{
			if (sw.targetEnemyID == msg.switch_id)
			{
				sw.OnSwitchActivated();
				break;
			}
		}
		LogManager.Instance?.AddLog("どこかのギミックが作動した", "#ffcc44");
	}


	private void HandleItemPickedMessage(string json)
	{

		var msg = JsonUtility.FromJson<ItemPickedSendMessage>(json);

		MissionManager.Instance?.OnItemPicked();
		LogManager.Instance?.AddLog("アイテムを取得した", "#aadd44");

		ItemManager nearestItem = null;
		float nearestDist = float.MaxValue;

		foreach (var item in FindObjectsOfType<ItemManager>())
		{
			Vector2 itemXZ = new Vector2(item.transform.position.x, item.transform.position.z);
			Vector2 msgXZ = new Vector2(msg.x, msg.z);
			float dist = Vector2.Distance(itemXZ, msgXZ);

			if (dist < 2f && dist < nearestDist)
			{
				nearestDist = dist;
				nearestItem = item;
			}
		}

		if (nearestItem != null)
		{
			var generator = FindObjectOfType<ElementGenerator>();
			if (generator != null)
			{
				generator.RemoveItemIcon(nearestItem.transform.position);
			}

			Destroy(nearestItem.gameObject);
		}
	}


	private void HandleStaminaItemPickedMessage(string json)
	{
		var msg = JsonUtility.FromJson<StaminaItemPickedSendMessage>(json);

		if (msg.sender_id == myId) return;

		StaminaManager.Instance?.RecoverStamina();
		LogManager.Instance?.AddLog("仲間がスタミナアイテムを取得した", "#ffcc44");

		Vector3 itemPos = new Vector3(msg.x, 1f, msg.z);

		StaminaItemManager nearestItem = null;
		float nearestDist = float.MaxValue;
		var candidates = FindObjectsOfType<StaminaItemManager>();

		foreach (var item in candidates)
		{
			Vector2 itemXZ = new Vector2(item.transform.position.x, item.transform.position.z);
			Vector2 msgXZ = new Vector2(msg.x, msg.z);
			float dist = Vector2.Distance(itemXZ, msgXZ);

			if (dist < 2f && dist < nearestDist)
			{
				nearestDist = dist;
				nearestItem = item;
			}
		}

		//Debug.Log($"[picked受信] pos=({msg.x:F1},{msg.z:F1}) 候補数={candidates.Length} ヒット={(nearestItem != null ? nearestItem.name + " dist=" + nearestDist.ToString("F2") : "なし")}");

		if (nearestItem != null && myPlayer != null && nearestItem.absorbEffectPrefab != null)
		{
			var effect = Instantiate(nearestItem.absorbEffectPrefab, itemPos, Quaternion.identity);
			effect.Play(itemPos, myPlayer.transform, nearestItem.effectColor);

			var generator = FindObjectOfType<ElementGenerator>();
			if (generator != null)
			{
				generator.RemoveItemIcon(nearestItem.transform.position);
			}

			nearestItem.gameObject.SetActive(false);
		}
	}


	private void HandleStaminaItemDropRequestMessage(string json)
	{
		if (!IsHostPlayer()) return;

		var msg = JsonUtility.FromJson<StaminaItemDropMessage>(json);
		if (msg.sender_id == myId) return;

		var sw = FindObjectOfType<SwitchManager>();
		if (sw == null) return;

		Vector3 dropPos = new Vector3(msg.x, msg.y, msg.z);
		if (msg.drop_type == 1)
			sw.SpawnGreenItem(dropPos);
		else
			sw.SpawnBlueItem(dropPos);

		SendStaminaItemDrop(msg.drop_type, dropPos);
	}


	private void HandleStaminaItemDropMessage(string json)
	{
		var msg = JsonUtility.FromJson<StaminaItemDropMessage>(json);
		if (msg.sender_id == myId) return; // 自分のドロップは無視（すでに生成済み）

		var sw = FindObjectOfType<SwitchManager>();
		if (sw == null) return;

		Vector3 pos = new Vector3(msg.x, msg.y, msg.z);
		if (msg.drop_type == 1)
			sw.SpawnGreenItem(pos);
		else
			sw.SpawnBlueItem(pos);
	}


	private void HandleChatMessage(string json)
	{
		var msg = JsonUtility.FromJson<ChatMessage>(json);
		if (msg.sender_id == myId) return;
		QuickChatManager.Instance?.OnChatReceived(msg.message, msg.sender_name);
	}


	#endregion
}
