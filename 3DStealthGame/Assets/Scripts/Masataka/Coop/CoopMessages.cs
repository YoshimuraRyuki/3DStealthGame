using System;
using UnityEngine;

public enum AttackElement { Fire = 0, Water = 1 }

[Serializable] public sealed class CoopRequest
{
    public string type = "coop_request", session_id, sender_id, op, target;
    public Vector3 direction;
}
[Serializable] public sealed class CoopShot
{
    public string type = "coop_shot", session_id, owner;
    public Vector3 origin, direction;
    public int element, damage;
    public float speed, range, radius;
}
[Serializable] public sealed class CoopState
{
    public string type = "coop_state", session_id;
    public int sequence;
    public bool cleared;
    public CoopCoreState[] cores;
    public CoopPlayerState[] players;
    public CoopGimmickState[] gimmicks;
}
[Serializable] public sealed class CoopCoreState
{
    public string key, owner = "", enemy = "";
    public int mode; // 0: ground, 1: player, 2: reserved, 3: returned
    public Vector3 position;
}
[Serializable] public sealed class CoopPlayerState
{
    public string id;
    public float stun, immune;
}
[Serializable] public sealed class CoopGimmickState { public string key; public bool activated; }
