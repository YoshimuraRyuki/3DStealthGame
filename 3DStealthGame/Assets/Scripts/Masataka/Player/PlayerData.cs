using System;

[System.Serializable]
public class PlayerData
{
	public string id;
	public string name;
	public int player_number;
	public PositionData position;
	public PositionData rotation;
}

[System.Serializable]
public class PositionData
{
	public float x;
	public float y;
	public float z;
}