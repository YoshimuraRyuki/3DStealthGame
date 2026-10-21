using UnityEngine;

[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class CoopChain : MonoBehaviour
{
    [Min(1f)] public float maxDistance = 6f;
    private LineRenderer line;
    private Material material;
    private void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>(); line.positionCount = 17;
        line.startWidth = line.endWidth = 0.045f;
        material = PlayerCombatController.CreateEnergyMaterial(new Color(0.65f, 0.7f, 0.75f));
        line.sharedMaterial = material; line.useWorldSpace = true; line.enabled = false;
    }
    // Restrict outward motion before Rigidbody integration. Tangential/inward movement is free.
    public Vector3 LimitVelocity(Transform local, Vector3 velocity, float dt)
    {
        Transform other = CoopGameSession.Instance.OtherPlayer(local);
        if (other == null) return velocity;
        Vector3 delta = local.position - other.position; delta.y = 0f;
        float length = delta.magnitude; if (length < 0.01f) return velocity;
        Vector3 radial = delta / length;
        float outward = Vector3.Dot(velocity, radial);
        float allowed = Mathf.Max(0f, maxDistance - 0.15f - length) / Mathf.Max(0.001f, dt);
        if (length > maxDistance) allowed = -Mathf.Min(4f, (length - maxDistance) * 8f);
        if (outward > allowed) velocity -= radial * (outward - allowed);
        return velocity;
    }
    private void LateUpdate()
    {
        var session = CoopGameSession.Instance;
        Transform first = session != null ? session.PlayerByNumber(1) : null;
        Transform second = session != null ? session.PlayerByNumber(2) : null;
        line.enabled = first != null && second != null;
        if (!line.enabled) return;
        Vector3 a = first.position + Vector3.up * 0.65f, b = second.position + Vector3.up * 0.65f;
        float distance = Vector3.Distance(a, b);
        Color color = distance > maxDistance - 0.4f ? new Color(1f, 0.35f, 0.1f) : new Color(0.65f, 0.7f, 0.75f);
        material.SetColor(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", color);
        for (int i = 0; i < 17; i++)
        {
            float t = i / 16f;
            line.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * Mathf.Clamp((maxDistance - distance) * 0.12f, 0f, 0.5f));
        }
    }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
