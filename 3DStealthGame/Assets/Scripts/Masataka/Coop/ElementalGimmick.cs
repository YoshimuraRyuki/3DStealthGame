using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class ElementalGimmick : MonoBehaviour
{
    [Tooltip("両端末で同じ一意ID。空なら初期座標から自動生成")]
    public string networkId;
    public AttackElement requiredElement = AttackElement.Fire;
    [Min(1)] public int requiredHits = 1;
    [Tooltip("燃やすツタ・消す炎など。ここに指定した物を非表示にする")]
    public GameObject[] objectsToDisable;
    public UnityEvent onActivated;
    public string Key { get; private set; }
    public bool Activated { get; private set; }
    private int hits;
    public void Initialize() { Key = string.IsNullOrWhiteSpace(networkId) ? CoopGameSession.Key("G", transform.position) : networkId; }
    public void Hit(AttackElement element)
    {
        if (!CoopGameSession.Instance.IsHost || Activated || element != requiredElement) return;
        if (++hits >= requiredHits) Apply(true);
    }
    public void Apply(bool activated)
    {
        if (!activated || Activated) return;
        Activated = true;
        // Prefer assigning a visual child. Leaving this object alive keeps its network identity.
        foreach (GameObject obj in objectsToDisable) if (obj != null && obj != gameObject) obj.SetActive(false);
        foreach (Collider c in GetComponents<Collider>()) c.enabled = false;
        foreach (Renderer r in GetComponents<Renderer>()) r.enabled = false;
        onActivated?.Invoke();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = requiredElement == AttackElement.Fire ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
    }
}
