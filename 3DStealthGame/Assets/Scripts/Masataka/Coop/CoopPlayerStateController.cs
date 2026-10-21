using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class CoopPlayerStateController : MonoBehaviour
{
    public string PlayerId { get; private set; }
    public AttackElement Element => CompareTag("Player2") ? AttackElement.Water : AttackElement.Fire;
    public bool IsStunned => Time.time < stunUntil;
    public bool IsImmune => Time.time < immuneUntil;
    public bool IsCarrying => CoopGameSession.Instance != null && CoopGameSession.Instance.PlayerCore(PlayerId) != null;
    public bool AttackBlocked => IsStunned || IsCarrying || (CoopGameSession.Instance != null && CoopGameSession.Instance.Cleared);
    public float StunRemaining => Mathf.Max(0f, stunUntil - Time.time);
    public float ImmuneRemaining => Mathf.Max(0f, immuneUntil - Time.time);
    private float stunUntil, immuneUntil, nextUse;
    private PlayerController player;
    private PlayerInput input;
    public void Initialize(string id) { PlayerId = id; player = GetComponent<PlayerController>(); input = GetComponent<PlayerInput>(); }
    public void ApplyTimers(float stun, float immune)
    {
        bool wasStunned = IsStunned;
        stunUntil = Time.time + Mathf.Max(0f, stun);
        immuneUntil = Time.time + Mathf.Max(0f, immune);
        if (!wasStunned && IsStunned && player != null) player.BeginStagger();
    }
    public void Hit(float seconds, float immunity)
    {
        if (IsImmune) return;
        ApplyTimers(seconds, seconds + immunity);
    }
    private void Update()
    {
        if (player == null || !player.isLocalPlayer || input == null || Time.time < nextUse) return;
        bool pressed = false;
        foreach (var device in input.devices)
        {
            if (device is Keyboard keyboard && keyboard.eKey.wasPressedThisFrame) pressed = true;
            if (device is Gamepad pad && pad.buttonWest.wasPressedThisFrame) pressed = true;
        }
        if (!pressed || IsStunned || player.IsFading || player.isAction || player.isPlayerMoveStop) return;
        nextUse = Time.time + 0.25f;
        CoopGameSession.Instance?.Interact(PlayerId);
    }
}
