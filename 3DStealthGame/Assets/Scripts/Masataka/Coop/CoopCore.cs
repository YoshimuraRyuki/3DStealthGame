using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoopCore : MonoBehaviour
{
    [Header("コア運搬")]
    [Min(0.1f)] public float pickupDistance = 1.8f;
    [Min(0f)] public float repickDelay = 0.8f;
    public string Key { get; private set; }
    public int Mode { get; private set; }
    public string Owner { get; private set; } = "";
    public string Enemy { get; private set; } = "";
    public Vector3 Home { get; private set; }
    public bool CanPick => Mode == 0 && Time.time >= canPickAt;
    private float canPickAt;
    private CoreCarryController carry;
    private Vector3 originalScale;
    private Collider[] colliders;
    private Renderer[] renderers;
    private bool[] colliderEnabled;
    private bool[] rendererEnabled;
    public void Initialize(string key)
    {
        Key = key; Home = transform.position; originalScale = transform.lossyScale;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        colliderEnabled = new bool[colliders.Length];
        rendererEnabled = new bool[renderers.Length];
        for (int i = 0; i < colliders.Length; i++) { colliderEnabled[i] = colliders[i].enabled; colliders[i].enabled = false; }
        for (int i = 0; i < renderers.Length; i++) rendererEnabled[i] = renderers[i].enabled;
        // Distance-based interaction: a pickup trigger cannot push or obstruct the players.
    }
    public CoopCoreState Capture() => new CoopCoreState { key = Key, mode = Mode, owner = Owner, enemy = Enemy, position = transform.position };
    public void Set(int mode, string owner, string enemy, Vector3 position)
    {
        bool changed = Mode != mode || Owner != owner || Enemy != enemy;
        if (changed && carry != null) { carry.ReleaseCore(position); carry = null; }
        Mode = mode; Owner = owner ?? ""; Enemy = enemy ?? "";
        if (changed) canPickAt = Time.time + repickDelay;
        if (mode == 1)
        {
            GameObject player = CoopGameSession.Instance.Player(Owner);
            if (player != null && carry == null)
            {
                carry = player.GetComponent<CoreCarryController>();
                if (carry == null) carry = player.AddComponent<CoreCarryController>();
                carry.BeginCarry(gameObject);
            }
        }
        else
        {
            transform.SetParent(null, true);
            transform.position = position;
            SetWorldScale(originalScale);
        }
        foreach (Collider collider in colliders) if (collider != null) collider.enabled = false;
        for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = mode != 3 && rendererEnabled[i];
    }
    private void SetWorldScale(Vector3 size)
    {
        transform.localScale = Vector3.one;
        Vector3 scale = transform.lossyScale;
        transform.localScale = new Vector3(size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)), size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)), size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
    }
    public void Drop(Vector3 at)
    {
        Vector3 position = CoopGameSession.Ground(at) + Vector3.up * 0.45f;
        Set(0, "", "", position);
    }
    private void OnDestroy()
    {
        if (carry != null) carry.ReleaseCore(transform.position);
    }
}
