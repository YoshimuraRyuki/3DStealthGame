using UnityEngine;

/// <summary>敵やギミックのルートに付ける任意の攻撃条件。敵AIは変更しない。</summary>
[DisallowMultipleComponent]
public sealed class AttackTargetCondition : MonoBehaviour
{
    [Tooltip("空なら初期座標から自動生成。重なるオブジェクトには別々のIDを指定")]
    public string networkId;
    [SerializeField] private bool attackAllowed = true;
    public bool requireElement;
    public AttackElement requiredElement = AttackElement.Fire;
    public string Key { get; private set; }
    public bool AttackAllowed => attackAllowed;
    public void Initialize()
    { Key = string.IsNullOrWhiteSpace(networkId) ? CoopGameSession.Key("A", transform.position) : networkId; }
    public bool Allows(AttackElement element)
    { return attackAllowed && (!requireElement || element == requiredElement); }
    public static bool AllowsTarget(Transform target, AttackElement element)
    {
        if (target == null) return false;
        var condition = target.GetComponentInParent<AttackTargetCondition>();
        return condition == null || condition.Allows(element);
    }
    // Connect a switch/event/custom condition to this method on the host.
    public void SetAttackAllowed(bool allowed)
    {
        if (CoopGameSession.Instance != null && !CoopGameSession.Instance.IsHost) return;
        attackAllowed = allowed;
    }
    public CoopAttackConditionState Capture()
    { return new CoopAttackConditionState { key = Key, allowed = attackAllowed, requireElement = requireElement, element = (int)requiredElement }; }
    public void Apply(CoopAttackConditionState state)
    {
        attackAllowed = state.allowed;
        requireElement = state.requireElement;
        requiredElement = (AttackElement)state.element;
    }
}
