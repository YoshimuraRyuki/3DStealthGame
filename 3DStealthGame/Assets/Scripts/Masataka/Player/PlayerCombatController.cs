using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(PlayerInput))]
public sealed class PlayerCombatController : MonoBehaviour
{
    [Header("射撃のタイミング")]
    [SerializeField, Min(0.12f)] private float fireInterval = 0.5f;
    [SerializeField, Min(0f)] private float windup = 0.05f;
    [SerializeField, Min(0f)] private float movementLock = 0.12f;
    [Header("直線弾")]
    [SerializeField, Min(1f)] private float projectileSpeed = 26f;
    [SerializeField, Min(0.1f)] private float projectileRange = 14f;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.14f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private float muzzleHeight = 1.1f;
    [Header("PAD照準")]
    [SerializeField, Range(0.05f, 0.9f)] private float stickDeadZone = 0.22f;
    private PlayerController controller;
    private PlayerInput input;
    private InputAction fire;
    private Camera aimCamera;
    private float nextFireTime;
    private float releaseTime;
    private Vector3 shotDirection;
    private bool pendingShot;
    private bool usingPad;
    private LineRenderer aimLine;
    private Material energyMaterial;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        input = GetComponent<PlayerInput>();
        fire = input.actions != null ? input.actions.FindAction("Fire", false) : null;
        aimCamera = Camera.main;
        energyMaterial = CreateEnergyMaterial(new Color(0.08f, 0.8f, 1f));
        GameObject guide = new GameObject("EnergyAimGuide");
        guide.transform.SetParent(transform, false);
        aimLine = guide.AddComponent<LineRenderer>();
        aimLine.material = energyMaterial;
        aimLine.positionCount = 2;
        aimLine.startWidth = 0.025f;
        aimLine.endWidth = 0.012f;
        aimLine.startColor = new Color(0.1f, 0.8f, 1f, 0.5f);
        aimLine.endColor = new Color(0.1f, 0.8f, 1f, 0.1f);
        aimLine.enabled = false;
    }

    private Gamepad PairedPad()
    {
        foreach (var device in input.devices)
            if (device is Gamepad pad) return pad;
        return null;
    }

    private bool HasPairedMouse()
    {
        foreach (var device in input.devices)
            if (device is Mouse) return true;
        return false;
    }

    private void Update()
    {
        if (!controller.isLocalPlayer) { aimLine.enabled = false; return; }
        if (controller.isPlayerMoveStop || controller.IsFading || controller.isAction)
        {
            pendingShot = false;
            controller.CancelShotLock();
            aimLine.enabled = false;
            return;
        }
        Gamepad pad = PairedPad();
        if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.04f ||
            pad.rightStick.ReadValue().sqrMagnitude > 0.04f || pad.rightTrigger.isPressed))
            usingPad = true;
        if (HasPairedMouse() && Mouse.current != null &&
            (Mouse.current.delta.ReadValue().sqrMagnitude > 1f || Mouse.current.leftButton.wasPressedThisFrame))
            usingPad = false;
        // An exclusively paired gamepad must never aim using the desktop cursor.
        if (pad != null && !HasPairedMouse()) usingPad = true;

        if (pendingShot && Time.time >= releaseTime)
        {
            pendingShot = false;
            SpawnShot();
        }
        Vector3 aim = GetAim(pad);
        aimLine.enabled = true;
        Vector3 origin = transform.position + Vector3.up * muzzleHeight;
        aimLine.SetPosition(0, origin);
        aimLine.SetPosition(1, origin + (controller.IsShotLocked ? shotDirection : aim) * 2.5f);

        // Existing Fire action takes priority. RT/left click are fallbacks for projects without Fire.
        bool held = fire != null ? fire.IsPressed() :
            (usingPad && pad != null ? pad.rightTrigger.isPressed :
             HasPairedMouse() && Mouse.current != null && Mouse.current.leftButton.isPressed);
        if (!held || pendingShot || controller.IsShotLocked || Time.time < nextFireTime) return;
        shotDirection = aim;
        float lockDuration = Mathf.Max(movementLock, windup);
        controller.BeginShotLock(shotDirection, lockDuration);
        releaseTime = Time.time + windup;
        nextFireTime = Time.time + Mathf.Max(fireInterval, lockDuration);
        pendingShot = true;
        if (windup <= 0f) { pendingShot = false; SpawnShot(); }
    }

    private Vector3 GetAim(Gamepad pad)
    {
        if (usingPad && pad != null)
        {
            Vector2 stick = pad.rightStick.ReadValue();
            if (stick.sqrMagnitude >= stickDeadZone * stickDeadZone)
                return new Vector3(stick.x, 0f, stick.y).normalized;
            // Use current movement input before the body has finished turning.
            Vector3 movement = controller.GetMoveDirection();
            return movement.sqrMagnitude > 0.001f ? movement.normalized : transform.forward;
        }
        if (HasPairedMouse() && Mouse.current != null)
        {
            if (aimCamera == null) aimCamera = Camera.main;
            if (aimCamera != null)
            {
                Ray ray = aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (new Plane(Vector3.up, transform.position).Raycast(ray, out float distance))
                {
                    Vector3 direction = ray.GetPoint(distance) - transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.001f) return direction.normalized;
                }
            }
        }
        return transform.forward;
    }

    private void SpawnShot()
    {
        GameObject shot = new GameObject("EnergySkillshot");
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

    private void OnDisable()
    {
        pendingShot = false;
        if (controller != null) controller.CancelShotLock();
        if (aimLine != null) aimLine.enabled = false;
    }
    private void OnDestroy()
    {
        // Projectiles share this material until their short lifetime expires.
        if (energyMaterial != null) Destroy(energyMaterial, projectileRange / projectileSpeed + 0.2f);
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
