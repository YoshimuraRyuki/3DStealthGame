using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(PlayerInput))]
public sealed class PlayerCombatController : MonoBehaviour
{
    [Header("自動ロックオン（敵NPCのみ）")]
    [SerializeField, Min(0.1f)] private float attackRange = 14f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [Header("射撃のタイミング")]
    [SerializeField, Min(0.12f)] private float fireInterval = 0.5f;
    [SerializeField, Min(0f)] private float windup = 0.05f;
    [SerializeField, Min(0f)] private float movementLock = 0.12f;
    [Header("エネルギー弾")]
    [SerializeField, Min(1f)] private float projectileSpeed = 26f;
    [SerializeField, Min(0.1f)] private float projectileRange = 14f;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.14f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private float muzzleHeight = 1.1f;
    private PlayerController controller;
    private PlayerInput input;
    private InputAction fire;
    private float nextFireTime, releaseTime, nextSearchTime;
    private Vector3 shotDirection;
    private bool pendingShot;
    private Transform lockedTarget, shotTarget;
    private EnemyLockOnIndicator lockIndicator;
    private Material energyMaterial;
    private readonly HashSet<Transform> candidates = new HashSet<Transform>();
    public Transform LockedTarget => lockedTarget;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        input = GetComponent<PlayerInput>();
        energyMaterial = CreateEnergyMaterial(new Color(0.08f, 0.8f, 1f));
        lockIndicator = gameObject.AddComponent<EnemyLockOnIndicator>();
    }

    private void Start()
    {
        // Resolve after PlayerInput has created its player-specific action instance.
        fire = input.actions != null ? input.actions.FindAction("Fire", false) : null;
    }

    private bool FireHeld()
    {
        if (fire != null) return fire.IsPressed();
        foreach (var device in input.devices)
        {
            if (device is Gamepad pad && pad.rightTrigger.isPressed) return true;
            if (device is Mouse mouse && mouse.leftButton.isPressed) return true;
        }
        return false;
    }

    private void Update()
    {
        if (!controller.isLocalPlayer || controller.isPlayerMoveStop || controller.IsFading || controller.isAction)
        {
            CancelAttack();
            return;
        }
        if (!CanTarget(lockedTarget, out _))
        {
            lockedTarget = null;
            if (Time.time >= nextSearchTime)
            {
                lockedTarget = FindNearestTarget();
                nextSearchTime = Time.time + 0.1f;
            }
        }
        if (pendingShot && Time.time >= releaseTime)
        {
            pendingShot = false;
            // Use the originally selected enemy, never silently shoot a different one.
            SpawnShot();
            shotTarget = null;
        }
        if (!FireHeld() || lockedTarget == null || pendingShot || controller.IsShotLocked || Time.time < nextFireTime) return;
        if (!CanTarget(lockedTarget, out Bounds bounds)) return;
        shotTarget = lockedTarget;
        shotDirection = bounds.center - (transform.position + Vector3.up * muzzleHeight);
        if (shotDirection.sqrMagnitude < 0.001f) return;
        shotDirection.Normalize();
        float lockDuration = Mathf.Max(movementLock, windup);
        controller.BeginShotLock(shotDirection, lockDuration);
        releaseTime = Time.time + windup;
        nextFireTime = Time.time + Mathf.Max(fireInterval, lockDuration);
        pendingShot = true;
        if (windup <= 0f) { pendingShot = false; SpawnShot(); shotTarget = null; }
    }

    private Transform FindNearestTarget()
    {
        candidates.Clear();
        // Include detection triggers so an enemy with a child trigger is discoverable.
        foreach (Collider collider in Physics.OverlapSphere(transform.position, attackRange, targetLayers, QueryTriggerInteraction.Collide))
        {
            Transform target = AutoAttackTarget.Resolve(collider);
            if (target != null) candidates.Add(target);
        }
        Transform nearest = null;
        float bestDistance = float.PositiveInfinity;
        foreach (Transform target in candidates)
        {
            if (!CanTarget(target, out _)) continue;
            float distance = (target.position - transform.position).sqrMagnitude;
            if (distance < bestDistance) { bestDistance = distance; nearest = target; }
        }
        return nearest;
    }

    private bool CanTarget(Transform target, out Bounds bounds)
    {
        bounds = new Bounds();
        if (!AutoAttackTarget.IsAlive(target)) return false;
        float range = Mathf.Min(attackRange, projectileRange);
        if ((target.position - transform.position).sqrMagnitude > range * range) return false;
        if (!AutoAttackTarget.TryBounds(target, out bounds)) return false;
        Vector3 origin = transform.position + Vector3.up * muzzleHeight;
        Vector3 delta = bounds.center - origin;
        if (delta.sqrMagnitude < 0.001f || delta.sqrMagnitude > projectileRange * projectileRange) return false;
        // Sweeps share the projectile's collision mask and radius: thin walls block locking too.
        foreach (Collider collider in Physics.OverlapSphere(origin, hitRadius, hitLayers, QueryTriggerInteraction.Ignore))
            if (BlocksLock(collider, target)) return false;
        foreach (RaycastHit hit in Physics.SphereCastAll(origin, hitRadius, delta.normalized,
            delta.magnitude, hitLayers, QueryTriggerInteraction.Ignore))
            if (BlocksLock(hit.collider, target)) return false;
        // Target colliders must also participate in projectile collisions.
        foreach (Collider collider in target.GetComponentsInChildren<Collider>())
            if (collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy &&
                (hitLayers.value & (1 << collider.gameObject.layer)) != 0) return true;
        return false;
    }

    private bool BlocksLock(Collider collider, Transform target)
    {
        if (collider == null || collider.isTrigger) return false;
        if (collider.GetComponentInParent<PlayerController>() != null) return false;
        return collider.transform != target && !collider.transform.IsChildOf(target);
    }

    private void LateUpdate()
    {
        if (controller.isLocalPlayer && lockedTarget != null) lockIndicator.Show(lockedTarget);
        else lockIndicator.Hide();
    }

    private void SpawnShot()
    {
        if (!CanTarget(shotTarget, out Bounds bounds)) return;
        Vector3 origin = transform.position + Vector3.up * muzzleHeight;
        shotDirection = (bounds.center - origin).normalized;
        if (shotDirection.sqrMagnitude < 0.001f) return;
        GameObject shot = new GameObject("EnergyBasicAttack");
        // Start at the player's centre; collision sweeps ignore the owner.
        // This prevents spawning a projectile on the far side of a nearby wall.
        shot.transform.position = transform.position + Vector3.up * muzzleHeight;
        shot.transform.rotation = Quaternion.LookRotation(shotDirection);
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(shot.transform, false);
        visual.transform.localScale = new Vector3(0.09f, 0.09f, 0.65f);
        Collider visualCollider = visual.GetComponent<Collider>();
        visualCollider.enabled = false;
        Destroy(visualCollider);
        visual.GetComponent<Renderer>().sharedMaterial = energyMaterial;
        TrailRenderer trail = shot.AddComponent<TrailRenderer>();
        trail.sharedMaterial = energyMaterial;
        trail.time = 0.06f;
        trail.startWidth = 0.07f;
        trail.endWidth = 0f;
        trail.startColor = new Color(0.12f, 0.9f, 1f, 0.8f);
        trail.endColor = new Color(0.12f, 0.9f, 1f, 0f);
        shot.AddComponent<EnergyProjectile>().Initialize(transform, shotDirection,
            projectileSpeed, projectileRange / projectileSpeed, damage, hitRadius, hitLayers);
    }

    private void CancelAttack()
    {
        pendingShot = false;
        shotTarget = lockedTarget = null;
        if (controller != null) controller.CancelShotLock();
        if (lockIndicator != null) lockIndicator.Hide();
    }
    private void OnDisable() { CancelAttack(); }
    private void OnDestroy()
    {
        if (energyMaterial != null) Destroy(energyMaterial, projectileRange / Mathf.Max(1f, projectileSpeed) + 0.2f);
    }
    public static Material CreateEnergyMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }
}
