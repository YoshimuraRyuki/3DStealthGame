using System;
using UnityEngine;

public sealed class CoopProjectile : MonoBehaviour
{
    private CoopShot shot;
    private bool authoritative;
    private float remaining;
    private Material material;
    public static void Spawn(CoopShot packet, GameObject owner, bool canDamage)
    {
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
        if (shot == null || CoopGameSession.Instance == null || !CoopGameSession.Instance.Ready || CoopGameSession.Instance.Cleared) { Destroy(gameObject); return; }
        foreach (Collider collider in Physics.OverlapSphere(transform.position, shot.radius, ~0, QueryTriggerInteraction.Collide))
            if (Eligible(collider)) { Hit(collider); return; }
        float distance = shot.speed * Mathf.Min(Time.deltaTime, remaining);
        var hits = Physics.SphereCastAll(transform.position, shot.radius, shot.direction, distance, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits) if (Eligible(hit.collider)) { Hit(hit.collider); return; }
        transform.position += shot.direction * distance;
        remaining -= Time.deltaTime; if (remaining <= 0f) Destroy(gameObject);
    }
    private void Hit(Collider collider)
    {
        if (authoritative && CoopGameSession.Instance != null && CoopGameSession.Instance.IsHost)
        {
            var gimmick = collider.GetComponentInParent<ElementalGimmick>();
            Transform enemy = AutoAttackTarget.Resolve(collider);
            if (gimmick != null) gimmick.Hit((AttackElement)shot.element);
            else if (enemy != null)
            {
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health == null) health = enemy.gameObject.AddComponent<EnemyHealth>();
                health.TakeDamage(shot.damage);
            }
        }
        Destroy(gameObject);
    }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
