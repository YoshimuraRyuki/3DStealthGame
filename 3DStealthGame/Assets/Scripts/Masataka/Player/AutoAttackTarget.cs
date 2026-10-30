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
        var gimmick = target.GetComponent<ElementalGimmick>();
        if (gimmick != null) return !gimmick.Activated;
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
    // Both the reticle and HP bar use the same projected rectangle, including
    // the reticle's padding/minimum size. A world-space head point is insufficient
    // when the camera is tilted or the enemy has depth.
    public static bool TryLockScreenBounds(Transform target, Camera camera, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        if (camera == null || !IsAlive(target) || !TryBounds(target, out Bounds bounds)) return false;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            Vector3 point = camera.WorldToScreenPoint(corner);
            if (point.z <= 0f) return false;
            min = Vector2.Min(min, new Vector2(point.x, point.y));
            max = Vector2.Max(max, new Vector2(point.x, point.y));
        }
        if (max.x < 0f || max.y < 0f || min.x > Screen.width || min.y > Screen.height) return false;
        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min + Vector2.one * 14f;
        size.x = Mathf.Max(52f, size.x);
        size.y = Mathf.Max(68f, size.y);
        min = center - size * 0.5f;
        max = center + size * 0.5f;
        return true;
    }
}
