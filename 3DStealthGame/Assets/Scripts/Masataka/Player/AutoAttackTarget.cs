using UnityEngine;

// Shared by targeting and damage: only enemy NPCs are eligible.
public static class AutoAttackTarget
{
    public static Transform Resolve(Collider collider)
    {
        if (collider == null || collider.GetComponentInParent<PlayerController>() != null) return null;
        EnemyManager ai = collider.GetComponentInParent<EnemyManager>();
        if (ai != null) return ai.transform;
        for (Transform t = collider.transform; t != null; t = t.parent)
            if (t.tag == "Enemy" || t.tag == "StrongEnemy") return t;
        return null;
    }

    public static bool IsAlive(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        EnemyHealth health = target.GetComponentInChildren<EnemyHealth>();
        return health == null || health.CurrentHealth > 0;
    }

    public static bool TryBounds(Transform target, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        if (target == null) return false;
        foreach (Collider collider in target.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy) continue;
            if (!found) { bounds = collider.bounds; found = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        return found;
    }
}
