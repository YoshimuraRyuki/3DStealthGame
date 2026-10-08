#region メッセージ定義

// 接続初期化メッセージ
[System.Serializable]
public class NetworkEnvelope
{
	public string type;
}

[System.Serializable]
public class InitMessage
{
	public string type;
	public string id;
	public string name;
	public int player_number;
	public PositionData position;
	public PositionData rotation;
}

// 既存プレイヤー一覧メッセージ（後から入室した側が受信）
[System.Serializable]
public class ExistingPlayersMessage
{
	public string type;
	public PlayerData[] players;
}

// 新規プレイヤー参加通知メッセージ
[System.Serializable]
public class PlayerJoinedMessage
{
	public string type;
	public string id;
	public string name;
	public int player_number;
	public PositionData position;
	public PositionData rotation;
}

// プレイヤー移動・動作状態の同期メッセージ
[System.Serializable]
public class PlayerMoveMessage
{
	public string type;
	public string id;
	public PositionData position;
	public PositionData rotation;
	public string anim_state;   // "idle" / "run" / "sneak"
	public string anim_trigger; // "PunchEnemy" / "PunchSwitch" など
}

[System.Serializable]
public class PlayerMoveSendMessage
{
	public string type = "player_move";
	public string id;
	public PositionData position;
	public PositionData rotation;
	public string anim_state;
	public string anim_trigger;
}

// プレイヤー退室通知メッセージ
[System.Serializable]
public class PlayerLeftMessage
{
	public string type;
	public string id;
	public string name;
}

[System.Serializable]
public class ExistingPlayersWrapper
{
	public PlayerData[] players;
}

// 残り時間更新メッセージ
[System.Serializable]
public class TimerUpdateMessage
{
	public string type;        // "timer_update"
	public int time_remaining; // 残り秒数
}

// ゴール関連メッセージ
[System.Serializable]
public class GoalMessage
{
	public string type;    // "goal"
	public string id;      // ゴールしたプレイヤーID
	public string name;    // プレイヤー名
	public float elapsed;  // クリアタイム（秒）
}

[System.Serializable]
public class GoalSendMessage
{
	public string type = "goal";
}

// 敵の位置・状態同期メッセージ
[System.Serializable]
public class EnemyMoveMessage
{
	public string type;         // "enemy_move"
	public int enemy_index;     // シーン内の敵の番号
	public float x;
	public float y;
	public float z;
	public float angle;         // 向き（Y軸回転）
	public float light_r;
	public float light_g;
	public float light_b;
	public float last_sound_x;
	public float last_sound_z;
	public string reaction;     // "" / "!" / "?"
	public string sender_id;
}

[System.Serializable]
public class EnemyMoveSendMessage
{
	public string type = "enemy_move";
	public int enemy_index;
	public float x;
	public float y;
	public float z;
	public float angle;
	public float light_r;
	public float light_g;
	public float light_b;
	public float last_sound_x;
	public float last_sound_z;
	public string reaction;
}
// スイッチ操作の同期メッセージ
[System.Serializable]
public class SwitchActivatedMessage
{
	public string type;
	public int switch_id;
}

// リスポーン位置の同期メッセージ
[System.Serializable]
public class RespawnMessage
{
	public string type;
	public string id;
	public PositionData position;
}

// 意思疎通用チャットメッセージ
[System.Serializable]
public class ChatMessage
{
	public string type;
	public string message;
	public string sender_id;
	public string sender_name;
}

[System.Serializable]
public class ChatSendMessage
{
	public string type = "chat";
	public string message;
	public string sender_id;
}

[System.Serializable]
public class StaminaItemDropMessage
{
	public string type;
	public string sender_id;
	public int drop_type;
	public int enemy_id;
	public float x;
	public float y;
	public float z;
}

[System.Serializable]
public class EnemyFoundMessage
{
	public string type = "enemy_found";
	public string sender_id;
}

[System.Serializable]
public class ItemPickedSendMessage
{
	public string type = "item_picked";
	public float x;
	public float z;
}

[System.Serializable]
public class StaminaItemPickedSendMessage
{
	public string type = "stamina_item_picked";
	public string sender_id;
	public float x;
	public float z;
}

[System.Serializable]
public class StaminaItemDropSendMessage
{
	public string type = "stamina_item_drop";
	public string sender_id;
	public int drop_type;
	public float x;
	public float y;
	public float z;
}

[System.Serializable]
public class StaminaItemDropRequestSendMessage
{
	public string type = "stamina_item_drop_request";
	public string sender_id;
	public int drop_type;
	public float x;
	public float y;
	public float z;
}

[System.Serializable]
public class RespawnSendMessage
{
	public string type = "respawn";
	public string id;
	public PositionData position;
}

[System.Serializable]
public class SwitchActivatedSendMessage
{
	public string type = "switch_activated";
	public int switch_id;
}

[System.Serializable]
public class EnemyStunSendMessage
{
	public string type;
	public int enemy_index;
	public string sender_id;
}

[System.Serializable]
public class RemoteRespawnSendMessage
{
	public string type = "remote_respawn";
	public string id;
}

[System.Serializable]
public class ReadySendMessage
{
	public string type = "ready";
	public PositionData position;
}

[System.Serializable]
public class StaminaStateMessage
{
	public string type;
	public string sender_id;
	public int current;
	public int max;
}

[System.Serializable]
public class StartGameMessage
{
	public string type;
	public string session_id;
}

#endregion
