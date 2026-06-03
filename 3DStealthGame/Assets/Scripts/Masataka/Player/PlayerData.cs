using System;

/// <summary>
/// プレイヤーの基本情報を保持するデータクラス群。
/// WebSocketのメッセージのデシリアライズに使用する。
/// </summary>

// プレイヤーの識別情報と座標データ
[System.Serializable]
public class PlayerData
{
	public string id;
	public string name;
	public int player_number;
	public PositionData position;
	public PositionData rotation;
}

// 座標・回転の3次元データ
[System.Serializable]
public class PositionData
{
	public float x;
	public float y;
	public float z;
}

// 準備完了メッセージ
[System.Serializable]
public class PlayerReadyMessage
{
	public string type;
	public string id;
}