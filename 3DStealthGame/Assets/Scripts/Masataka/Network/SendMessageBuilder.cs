using UnityEngine;

/// <summary>
/// サーバーへ送るメッセージを作る。
/// </summary>
public static class SendMessageBuilder
{
	public static string Goal()
	{
		return JsonUtility.ToJson(new GoalSendMessage());
	}

	public static string PlayerMove(
	string id,
	Vector3 pos,
	Vector3 rot,
	string animState,
	string trigger)
	{
		var data = new PlayerMoveSendMessage
		{
			id = id,
			position = new PositionData
			{
				x = pos.x,
				y = pos.y,
				z = pos.z
			},
			rotation = new PositionData
			{
				x = rot.x,
				y = rot.y,
				z = 0f
			},
			anim_state = string.IsNullOrEmpty(animState) ? "idle" : animState,
			anim_trigger = string.IsNullOrEmpty(trigger) ? "" : trigger
		};

		return JsonUtility.ToJson(data);
	}

	public static string Respawn(string id, Vector3 pos)
	{
		var data = new RespawnSendMessage
		{
			id = id,
			position = new PositionData
			{
				x = pos.x,
				y = pos.y,
				z = pos.z
			}
		};

		return JsonUtility.ToJson(data);
	}

	public static string SwitchActivated(int switchId)
	{
		var data = new SwitchActivatedSendMessage
		{
			switch_id = switchId
		};

		return JsonUtility.ToJson(data);
	}

	public static string ItemPicked()
	{
		return JsonUtility.ToJson(new ItemPickedSendMessage());
	}

	public static string ItemPicked(Vector3 pos)
	{
		var data = new ItemPickedSendMessage
		{
			x = pos.x,
			z = pos.z
		};

		return JsonUtility.ToJson(data);
	}

	public static string EnemyStun(int enemyIndex, string senderId)
	{
		var data = new EnemyStunSendMessage
		{
			type = "enemy_stun",
			enemy_index = enemyIndex,
			sender_id = senderId
		};

		return JsonUtility.ToJson(data);
	}

	public static string EnemyStunCancel(int enemyIndex, string senderId)
	{
		var data = new EnemyStunSendMessage
		{
			type = "enemy_stun_cancel",
			enemy_index = enemyIndex,
			sender_id = senderId
		};

		return JsonUtility.ToJson(data);
	}

	public static string RemoteRespawn(string id)
	{
		var data = new RemoteRespawnSendMessage
		{
			id = id
		};

		return JsonUtility.ToJson(data);
	}

	public static string Chat(string message, string senderId)
	{
		var data = new ChatSendMessage
		{
			message = message,
			sender_id = senderId
		};

		return JsonUtility.ToJson(data);
	}

	public static string StaminaItemPicked(string senderId, Vector3 pos)
	{
		var data = new StaminaItemPickedSendMessage
		{
			sender_id = senderId,
			x = pos.x,
			z = pos.z
		};

		return JsonUtility.ToJson(data);
	}

	public static string StaminaItemDrop(string senderId, int dropType, Vector3 pos)
	{
		var data = new StaminaItemDropSendMessage
		{
			sender_id = senderId,
			drop_type = dropType,
			x = pos.x,
			y = pos.y,
			z = pos.z
		};

		return JsonUtility.ToJson(data);
	}

	public static string StaminaItemDropRequest(string senderId, int dropType, Vector3 pos)
	{
		var data = new StaminaItemDropRequestSendMessage
		{
			sender_id = senderId,
			drop_type = dropType,
			x = pos.x,
			y = pos.y,
			z = pos.z
		};

		return JsonUtility.ToJson(data);
	}

	public static string EnemyFound(string senderId)
	{
		var data = new EnemyFoundMessage
		{
			sender_id = senderId
		};

		return JsonUtility.ToJson(data);
	}

	public static string EnemyMove(
	int index,
	Vector3 pos,
	float angle,
	Color light,
	string reaction,
	Vector3 lastSound)
	{
		var data = new EnemyMoveSendMessage
		{
			enemy_index = index,
			x = pos.x,
			y = pos.y,
			z = pos.z,
			angle = angle,
			light_r = light.r,
			light_g = light.g,
			light_b = light.b,
			last_sound_x = lastSound.x,
			last_sound_z = lastSound.z,
			reaction = string.IsNullOrEmpty(reaction) ? "" : reaction
		};

		return JsonUtility.ToJson(data);
	}

	public static string Ready(Vector3 pos)
	{
		var data = new ReadySendMessage
		{
			position = new PositionData
			{
				x = pos.x,
				y = pos.y,
				z = pos.z
			}
		};

		return JsonUtility.ToJson(data);
	}

	public static string StaminaState(string senderId, int current, int max)
	{
		return JsonUtility.ToJson(new StaminaStateMessage
		{
			type = "stamina_state",
			sender_id = senderId,
			current = current,
			max = max
		});
	}
}
