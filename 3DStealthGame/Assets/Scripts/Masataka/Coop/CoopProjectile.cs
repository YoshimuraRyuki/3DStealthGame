using System;
using UnityEngine;

public sealed class CoopProjectile : MonoBehaviour
{
    private CoopShot shot;
    private bool authoritative, spent;
    private float remaining;
    private Material material;
    public static void Spawn(CoopShot packet, GameObject owner, bool canDamage)
    {
        PlayerFireAnimation.NotifyProjectile(owner);
        GameObject obj = new GameObject(packet.element == 0 ? "FireShot" : "WaterShot");
        var projectile = obj.AddComponent<CoopProjectile>();
        projectile.shot = packet; projectile.authoritative = canDamage;
        projectile.remaining = packet.range / Mathf.Max(1f, packet.speed);
        obj.transform.position = packet.origin; obj.transform.forward = packet.direction;
        Color color = packet.element == 0 ? new Color(1f, 0.25f, 0.04f) : new Color(0.1f, 0.65f, 1f);
        projectile.material = PlayerCombatController.CreateEnergyMaterial(color);
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(obj.transform, false); visual.transform.localScale = new Vector3(0.14f, 0.14f, 0.55f);
        Collider collider = visual.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        visual.GetComponent<Renderer>().sharedMaterial = projectile.material;
        var trail = obj.AddComponent<TrailRenderer>(); trail.sharedMaterial = projectile.material;
        trail.time = 0.1f; trail.startWidth = 0.1f; trail.endWidth = 0f;
    }
    private bool Eligible(Collider collider)
    {
        return collider != null && collider.GetComponentInParent<PlayerController>() == null &&
            collider.GetComponentInParent<CoopCore>() == null && !collider.transform.IsChildOf(transform) &&
            (!collider.isTrigger || collider.GetComponentInParent<ElementalGimmick>() != null);
    }
    private void Update()
    {
        if (spent) return;
        if (shot == null || CoopGameSession.Instance == null || !CoopGameSession.Instance.Ready || CoopGameSession.Instance.Cleared) { Destroy(gameObject); return; }
        foreach (Collider collider in Physics.OverlapSphere(transform.position, shot.radius, ~0, QueryTriggerInteraction.Collide))
            if (Eligible(collider))
            {
                Vector3 point = collider.ClosestPoint(transform.position);
                Hit(collider, point, (transform.position - point).normalized);
                return;
            }
        float distance = shot.speed * Mathf.Min(Time.deltaTime, remaining);
        var hits = Physics.SphereCastAll(transform.position, shot.radius, shot.direction, distance, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits) if (Eligible(hit.collider)) { Hit(hit.collider, hit.point, hit.normal); return; }
        transform.position += shot.direction * distance;
        remaining -= Time.deltaTime; if (remaining <= 0f) Destroy(gameObject);
    }
    private void Hit(Collider collider, Vector3 point, Vector3 normal)
    {
        if (spent) return;
        spent = true;
        if (normal.sqrMagnitude < 0.001f) normal = -shot.direction.normalized;
        if (authoritative && CoopGameSession.Instance != null && CoopGameSession.Instance.IsHost)
        {
            int kind = 0;
            var element = (AttackElement)shot.element;
            var gimmick = collider.GetComponentInParent<ElementalGimmick>();
            Transform enemy = AutoAttackTarget.Resolve(collider);
            if (gimmick != null)
            {
                kind = gimmick.CanReceiveAttack(element) ? 1 : 2;
                gimmick.Hit(element);
            }
            else if (enemy != null)
            {
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health == null) health = enemy.gameObject.AddComponent<EnemyHealth>();
                int before = health.CurrentHealth;
                health.TakeDamage(shot.damage, element);
                kind = health.CurrentHealth < before ? 1 : 2;
            }
            // Guests never predict a hit confirmation. Only the host reports it.
            CoopGameSession.Instance.ReportImpact(point, normal, element, kind);
        }
        Destroy(gameObject);
    }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
