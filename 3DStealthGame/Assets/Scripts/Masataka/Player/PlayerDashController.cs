using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>移動方向への短距離ダッシュ。通常の位置同期を使用し、敵AIには触れない。</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(50)]
[RequireComponent(typeof(PlayerController), typeof(PlayerInput), typeof(Rigidbody))]
public sealed class PlayerDashController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float dashDistance = 3f;
    [SerializeField, Min(0.02f)] private float dashDuration = 0.15f;
    [SerializeField, Min(0f)] private float cooldown = 3f;
    [SerializeField, Min(0.005f)] private float wallSkin = 0.04f;
    public bool IsDashing { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, nextDashAt - Time.time);
    private PlayerController player;
    private PlayerInput input;
    private Rigidbody body;
    private Vector3 direction;
    private float remaining, speed, nextDashAt;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        input = GetComponent<PlayerInput>();
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (player == null || !player.isLocalPlayer || input == null || Time.timeScale <= 0f) return;
        foreach (InputDevice device in input.devices)
        {
            bool pressed = device is Keyboard keyboard &&
                (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame);
            if (device is Gamepad pad && pad.buttonEast.wasPressedThisFrame) pressed = true;
            if (pressed) { TryStartDash(); break; }
        }
    }

    private bool MovementBlocked()
    {
        if (player == null || !player.isLocalPlayer || body == null || body.isKinematic ||
            player.isPlayerMoveStop || player.isAction || player.IsFading) return true;
        var state = GetComponent<CoopPlayerStateController>();
        if (state != null && (state.IsStunned || state.IsCarrying)) return true;
        var session = CoopGameSession.Instance;
        return session != null && (!session.Ready || session.Cleared);
    }

    public bool TryStartDash()
    {
        if (Time.timeScale <= 0f || IsDashing || Time.time < nextDashAt || MovementBlocked() || player.IsShotLocked) return false;
        Vector3 move = player.GetMoveDirection();
        move.y = 0f;
        if (move.sqrMagnitude < 0.001f) { move = transform.forward; move.y = 0f; }
        if (move.sqrMagnitude < 0.001f) return false;
        direction = move.normalized;
        // Don't spend cooldown when a wall/chain prevents any meaningful movement.
        if (AllowedStep(direction * dashDistance).sqrMagnitude < 0.0025f) return false;
        remaining = dashDistance;
        speed = dashDistance / Mathf.Max(0.02f, dashDuration);
        nextDashAt = Time.time + cooldown;
        player.CancelShotLock();
        body.rotation = Quaternion.LookRotation(direction, Vector3.up);
        IsDashing = true;
        return true;
    }

    // Called from PlayerController.FixedUpdate instead of the normal movement step.
    public bool TickDash(float dt)
    {
        if (!IsDashing) return false;
        if (dt <= 0f || MovementBlocked() || remaining <= 0.0001f)
        { CancelDash(); return false; }
        float requested = Mathf.Min(remaining, speed * dt);
        Vector3 step = AllowedStep(direction * requested);
        if (step.sqrMagnitude < 0.000001f) { CancelDash(); return false; }
        body.velocity = new Vector3(step.x / dt, body.velocity.y, step.z / dt);
        remaining = Mathf.Max(0f, remaining - step.magnitude);
        // Stop on a wall/chain instead of spending later frames pushing into it.
        if (step.magnitude + 0.0001f < requested) remaining = 0f;
        return true;
    }

    private Vector3 AllowedStep(Vector3 step)
    {
        var session = CoopGameSession.Instance;
        Transform other = session != null ? session.OtherPlayer(transform) : null;
        if (other != null && session.Chain != null)
            step = LimitToChain(body.position, other.position, step, Mathf.Max(0.1f, session.Chain.maxDistance - 0.15f));
        float length = step.magnitude;
        if (length <= 0.0001f) return Vector3.zero;
        Vector3 heading = step / length;
        foreach (RaycastHit hit in body.SweepTestAll(heading, length + wallSkin, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            // Ignore the walkable floor/ramp, but keep walls and solid characters.
            if (hit.normal.y >= 0.6f) continue;
            length = Mathf.Min(length, Mathf.Max(0f, hit.distance - wallSkin));
        }
        return heading * length;
    }

    internal static Vector3 LimitToChain(Vector3 position, Vector3 other, Vector3 step, float radius)
    {
        Vector3 offset = position - other; offset.y = 0f;
        step.y = 0f;
        float a = Vector3.Dot(step, step);
        if (a < 0.000001f) return Vector3.zero;
        float startSq = offset.sqrMagnitude;
        float endSq = (offset + step).sqrMagnitude;
        float radiusSq = radius * radius;
        if (endSq <= radiusSq) return step;
        if (startSq > radiusSq)
            return endSq < startSq ? step : Vector3.zero;
        float b = Vector3.Dot(offset, step);
        float c = startSq - radiusSq;
        float fraction = (-b + Mathf.Sqrt(Mathf.Max(0f, b * b - a * c))) / a;
        return step * Mathf.Clamp01(fraction);
    }

    public void CancelDash()
    {
        if (!IsDashing) return;
        IsDashing = false;
        remaining = 0f;
        if (body != null && !body.isKinematic)
            body.velocity = new Vector3(0f, body.velocity.y, 0f);
    }
    private void OnDisable() { CancelDash(); }
}
