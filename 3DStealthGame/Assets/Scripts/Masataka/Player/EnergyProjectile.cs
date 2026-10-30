using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnergyProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed, lifeTime, radius;
    private int damage;
    private Transform owner;
    private LayerMask hitLayers = ~0;
    private bool initialized, spent;
    // Preserve compatibility with old call sites.
    public void Initialize(Transform projectileOwner, Vector3 fireDirection,
        float projectileSpeed, float duration, int projectileDamage)
    {
        Initialize(projectileOwner, fireDirection, projectileSpeed, duration, projectileDamage, 0.14f, ~0);
    }
    public void Initialize(Transform projectileOwner, Vector3 fireDirection,
        float projectileSpeed, float duration, int projectileDamage, float hitRadius, LayerMask layers)
    {
        owner = projectileOwner;
        direction = fireDirection.normalized;
        speed = Mathf.Max(0.01f, projectileSpeed);
        lifeTime = duration;
        damage = projectileDamage;
        radius = Mathf.Max(0.01f, hitRadius);
        hitLayers = layers;
        transform.forward = direction;
        initialized = true;
    }
    private bool CanHit(Collider other)
    {
        return other != null && !other.isTrigger &&
            other.GetComponentInParent<PlayerController>() == null &&
            !(owner != null && (other.transform == owner || other.transform.IsChildOf(owner))) &&
            other.transform != transform && !other.transform.IsChildOf(transform);
    }
    private void Update()
    {
        if (!initialized || spent) return;
        // Sphere casts do not report colliders that already overlap the origin.
        foreach (Collider other in Physics.OverlapSphere(transform.position, radius, hitLayers, QueryTriggerInteraction.Ignore))
            if (CanHit(other)) { Hit(other, other.ClosestPoint(transform.position)); return; }
        float distance = speed * Mathf.Min(Time.deltaTime, Mathf.Max(0f, lifeTime));
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, direction,
            distance, hitLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
            if (CanHit(hit.collider)) { Hit(hit.collider, hit.point); return; }
        transform.position += direction * distance;
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f) Destroy(gameObject);
    }
    private void Hit(Collider other, Vector3 position)
    {
        if (spent) return;
        spent = true;
        var playerState = owner != null ? owner.GetComponent<CoopPlayerStateController>() : null;
        AttackElement element = playerState != null ? playerState.Element :
            (owner != null && owner.CompareTag("Player2") ? AttackElement.Water : AttackElement.Fire);
        var gimmick = other.GetComponentInParent<ElementalGimmick>();
        int kind = 0;
        if (gimmick != null)
        {
            kind = gimmick.CanReceiveAttack(element) ? 1 : 2;
            gimmick.Hit(element);
        }
        else
        {
            IDamageable target = FindDamageable(other);
            if (target is EnemyHealth health)
            {
                int before = health.CurrentHealth;
                health.TakeDamage(damage, element);
                kind = health.CurrentHealth < before ? 1 : 2;
            }
            else if (target != null)
            {
                bool allowed = AttackTargetCondition.AllowsTarget(AutoAttackTarget.Resolve(other), element);
                kind = allowed ? 1 : 2;
                if (allowed) target.TakeDamage(damage);
            }
        }
        Vector3 normal = (transform.position - position).normalized;
        if (normal.sqrMagnitude < 0.001f) normal = -direction;
        EnergyHitFeedback.Play(position, normal, element, kind);
        Destroy(gameObject);
    }
    private static IDamageable FindDamageable(Collider other)
    {
        Transform target = AutoAttackTarget.Resolve(other);
        if (!AutoAttackTarget.IsAlive(target)) return null;
        foreach (MonoBehaviour behaviour in other.GetComponentsInParent<MonoBehaviour>())
            if (behaviour is IDamageable damageable) return damageable;
        foreach (MonoBehaviour behaviour in target.GetComponentsInChildren<MonoBehaviour>())
            if (behaviour is IDamageable damageable) return damageable;
        return target.gameObject.AddComponent<EnemyHealth>();
    }
}
public sealed class EnergyImpactEffect : MonoBehaviour
{
    private float elapsed;
    private Material ownedMaterial;
    public void Initialize(Material material) { ownedMaterial = material; }
    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 0.4f, elapsed / 0.10f);
        if (elapsed >= 0.10f) Destroy(gameObject);
    }
    private void OnDestroy() { if (ownedMaterial != null) Destroy(ownedMaterial); }
}
