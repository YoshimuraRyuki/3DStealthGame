using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 後期版のオリジナルプレイヤー「CORE RUNNER」を実行時に構築する。
/// 外部モデルに依存せず、既存の当たり判定・通信・Animatorを維持したまま見た目だけを置き換える。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class CoreRunnerRobotVisual : MonoBehaviour
{
    [Header("表示")]
    [SerializeField] private bool replaceLegacyModel = true;
    [SerializeField] private bool showCarriedCore;
    [SerializeField, Range(0.75f, 1.35f)] private float modelScale = 1f;
    [SerializeField, Range(0f, 0.5f)] private float groundOffset = 0.18f;

    [Header("アニメーション")]
    [SerializeField, Min(1f)] private float animationResponsiveness = 12f;
    [SerializeField, Range(5f, 35f)] private float runLimbAngle = 24f;
    [SerializeField, Range(0f, 0.15f)] private float runBobHeight = 0.055f;

    private PlayerController controller;
    private Rigidbody body;
    private readonly List<Renderer> legacyRenderers = new List<Renderer>();

    private Transform visualRoot;
    private Transform torsoPivot;
    private Transform headPivot;
    private Transform leftShoulder;
    private Transform rightShoulder;
    private Transform leftElbow;
    private Transform rightElbow;
    private Transform leftHip;
    private Transform rightHip;
    private Transform leftKnee;
    private Transform rightKnee;
    private Transform muzzle;
    private Transform coreDisplay;
    private Transform antennaTip;
    private Transform backpackGlow;
    private Transform leftThrusterGlow;
    private Transform rightThrusterGlow;

    private Material armorMaterial;
    private Material darkMaterial;
    private Material accentMaterial;
    private Material glowMaterial;
    private Material jointMaterial;

    private bool paletteIsLocal;
    private float smoothedSpeed;
    private float fireKick;
    private float hitFlash;
    private float alertPulse;
    private Vector3 baseRootPosition;
    private bool restoreDynamicBodyAfterBuild;
    private bool bodyUsedGravityBeforeBuild;

    public Vector3 MuzzlePosition => muzzle != null
        ? muzzle.position
        : transform.position + Vector3.up * 1.1f + transform.forward * 0.8f;

    public Vector3 MuzzleForward => muzzle != null ? muzzle.forward : transform.forward;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();

        ProtectBodyDuringBuild();

        CacheAndHideLegacyModel();
        BuildRobot();
        ApplyPalette(controller.isLocalPlayer, true);
        StartCoroutine(FinishBuildSafely());
    }

    private void OnDestroy()
    {
        RestoreBodyAfterBuild();
        RestoreLegacyModel();
        DestroyMaterial(armorMaterial);
        DestroyMaterial(darkMaterial);
        DestroyMaterial(accentMaterial);
        DestroyMaterial(glowMaterial);
        DestroyMaterial(jointMaterial);
    }

    private void Update()
    {
        if (visualRoot == null) return;

        if (paletteIsLocal != controller.isLocalPlayer)
            ApplyPalette(controller.isLocalPlayer, false);

        AnimateRobot();
    }

    public void PlayFire()
    {
        fireKick = 1f;
        if (muzzle != null)
            CreateMuzzleFlash();
    }

    public void PlayHit()
    {
        hitFlash = 1f;
    }

    public void SetCoreHeld(bool isHeld)
    {
        showCarriedCore = isHeld;
        if (coreDisplay != null)
            coreDisplay.gameObject.SetActive(isHeld);
    }

    public void SetAlertLevel(float normalizedAlert)
    {
        alertPulse = Mathf.Clamp01(normalizedAlert);
    }

    public void SetLegacyModelVisible(bool visible)
    {
        replaceLegacyModel = !visible;
        foreach (Renderer renderer in legacyRenderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }

        if (visualRoot != null)
            visualRoot.gameObject.SetActive(!visible);
    }

    private void CacheAndHideLegacyModel()
    {
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            legacyRenderers.Add(renderer);
            if (replaceLegacyModel)
                renderer.enabled = false;
        }
    }

    private void RestoreLegacyModel()
    {
        foreach (Renderer renderer in legacyRenderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
    }

    private void BuildRobot()
    {
        CreateMaterials();

        visualRoot = NewPivot("CORE_RUNNER_Visual", transform);
        // 足パーツの装甲がルートより約0.15下まで伸びるため、床面に合わせて持ち上げる。
        visualRoot.localPosition = Vector3.up * groundOffset;
        visualRoot.localScale = Vector3.one * modelScale;
        baseRootPosition = visualRoot.localPosition;

        BuildLowerBody();
        BuildTorso();
        BuildHead();
        BuildArms();
        BuildBackpack();
        BuildCoreDisplay();
    }

    private void BuildLowerBody()
    {
        Transform pelvis = CreatePart("Pelvis", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 0.92f, 0f), new Vector3(0.58f, 0.24f, 0.32f), darkMaterial);
        CreatePart("PelvisArmor", PrimitiveType.Cube, pelvis,
            new Vector3(0f, 0.05f, 0.11f), new Vector3(0.48f, 0.16f, 0.16f), armorMaterial);

        leftHip = BuildLeg("L", visualRoot, new Vector3(-0.21f, 0.86f, 0f));
        rightHip = BuildLeg("R", visualRoot, new Vector3(0.21f, 0.86f, 0f));
    }

    private Transform BuildLeg(string side, Transform parent, Vector3 hipPosition)
    {
        bool isLeft = side == "L";
        Transform hip = NewPivot(side + "_Hip", parent);
        hip.localPosition = hipPosition;

        CreatePart(side + "_Thigh", PrimitiveType.Capsule, hip,
            new Vector3(0f, -0.25f, 0f), new Vector3(0.19f, 0.25f, 0.19f), darkMaterial);
        CreatePart(side + "_ThighArmor", PrimitiveType.Cube, hip,
            new Vector3(0f, -0.16f, 0.08f), new Vector3(0.25f, 0.27f, 0.16f), armorMaterial,
            new Vector3(isLeft ? 0f : 0f, 0f, isLeft ? -4f : 4f));

        Transform knee = NewPivot(side + "_Knee", hip);
        knee.localPosition = new Vector3(0f, -0.52f, 0f);
        CreatePart(side + "_KneeLight", PrimitiveType.Sphere, knee,
            new Vector3(0f, 0.01f, 0.12f), new Vector3(0.12f, 0.12f, 0.08f), glowMaterial);
        CreatePart(side + "_KneeJointCap", PrimitiveType.Cylinder, knee,
            new Vector3(isLeft ? -0.125f : 0.125f, 0f, 0f), new Vector3(0.10f, 0.035f, 0.10f), jointMaterial,
            new Vector3(0f, 0f, 90f));
        CreatePart(side + "_Shin", PrimitiveType.Capsule, knee,
            new Vector3(0f, -0.23f, 0f), new Vector3(0.16f, 0.24f, 0.16f), darkMaterial);
        CreatePart(side + "_ShinArmor", PrimitiveType.Cube, knee,
            new Vector3(0f, -0.22f, 0.09f), new Vector3(0.21f, 0.30f, 0.14f), armorMaterial);
        CreatePart(side + "_Foot", PrimitiveType.Cube, knee,
            new Vector3(0f, -0.49f, 0.10f), new Vector3(0.25f, 0.14f, 0.42f), darkMaterial);
        CreatePart(side + "_ToeLight", PrimitiveType.Cube, knee,
            new Vector3(0f, -0.49f, 0.31f), new Vector3(0.16f, 0.055f, 0.025f), glowMaterial);

        Transform thruster = CreatePart(side + "_HeelThruster", PrimitiveType.Cylinder, knee,
            new Vector3(0f, -0.39f, -0.12f), new Vector3(0.09f, 0.07f, 0.09f), darkMaterial,
            new Vector3(90f, 0f, 0f));
        Transform thrusterGlow = CreatePart(side + "_ThrusterGlow", PrimitiveType.Sphere, thruster,
            new Vector3(0f, 0.54f, 0f), new Vector3(0.58f, 0.16f, 0.58f), glowMaterial);

        if (isLeft)
        {
            leftKnee = knee;
            leftThrusterGlow = thrusterGlow;
        }
        else
        {
            rightKnee = knee;
            rightThrusterGlow = thrusterGlow;
        }

        return hip;
    }

    private void BuildTorso()
    {
        torsoPivot = NewPivot("TorsoPivot", visualRoot);
        torsoPivot.localPosition = new Vector3(0f, 1.12f, 0f);

        CreatePart("TorsoCore", PrimitiveType.Capsule, torsoPivot,
            new Vector3(0f, 0.22f, 0f), new Vector3(0.36f, 0.36f, 0.26f), darkMaterial);
        CreatePart("ChestArmor", PrimitiveType.Cube, torsoPivot,
            new Vector3(0f, 0.25f, 0.10f), new Vector3(0.72f, 0.50f, 0.28f), armorMaterial);
        CreatePart("ChestInset", PrimitiveType.Cube, torsoPivot,
            new Vector3(0f, 0.24f, 0.255f), new Vector3(0.40f, 0.25f, 0.035f), darkMaterial);
        CreatePart("ChestLight", PrimitiveType.Cube, torsoPivot,
            new Vector3(0f, 0.25f, 0.278f), new Vector3(0.25f, 0.055f, 0.018f), glowMaterial);

        CreatePart("L_Collar", PrimitiveType.Cube, torsoPivot,
            new Vector3(-0.32f, 0.53f, 0.02f), new Vector3(0.28f, 0.12f, 0.34f), jointMaterial,
            new Vector3(0f, 0f, -10f));
        CreatePart("R_Collar", PrimitiveType.Cube, torsoPivot,
            new Vector3(0.32f, 0.53f, 0.02f), new Vector3(0.28f, 0.12f, 0.34f), jointMaterial,
            new Vector3(0f, 0f, 10f));

        for (int i = -1; i <= 1; i++)
        {
            CreatePart("AbSegment_" + i, PrimitiveType.Cube, torsoPivot,
                new Vector3(0f, -0.02f + i * 0.075f, 0.06f),
                new Vector3(0.30f - Mathf.Abs(i) * 0.025f, 0.045f, 0.22f), darkMaterial);
        }
    }

    private void BuildHead()
    {
        headPivot = NewPivot("HeadPivot", torsoPivot);
        headPivot.localPosition = new Vector3(0f, 0.76f, 0f);

        CreatePart("Neck", PrimitiveType.Cylinder, headPivot,
            new Vector3(0f, -0.10f, 0f), new Vector3(0.12f, 0.12f, 0.12f), darkMaterial);
        CreatePart("HeadShell", PrimitiveType.Sphere, headPivot,
            new Vector3(0f, 0.10f, 0f), new Vector3(0.52f, 0.36f, 0.40f), armorMaterial);
        CreatePart("FacePlate", PrimitiveType.Cube, headPivot,
            new Vector3(0f, 0.09f, 0.195f), new Vector3(0.43f, 0.20f, 0.045f), darkMaterial);
        CreatePart("Visor", PrimitiveType.Cube, headPivot,
            new Vector3(0f, 0.12f, 0.223f), new Vector3(0.31f, 0.075f, 0.018f), glowMaterial);
        CreatePart("L_Ear", PrimitiveType.Cylinder, headPivot,
            new Vector3(-0.29f, 0.10f, 0f), new Vector3(0.10f, 0.055f, 0.10f), jointMaterial,
            new Vector3(0f, 0f, 90f));
        CreatePart("R_Ear", PrimitiveType.Cylinder, headPivot,
            new Vector3(0.29f, 0.10f, 0f), new Vector3(0.10f, 0.055f, 0.10f), jointMaterial,
            new Vector3(0f, 0f, 90f));

        Transform antenna = NewPivot("Antenna", headPivot);
        antenna.localPosition = new Vector3(0.18f, 0.30f, -0.02f);
        antenna.localRotation = Quaternion.Euler(0f, 0f, -16f);
        CreatePart("AntennaStem", PrimitiveType.Cylinder, antenna,
            new Vector3(0f, 0.10f, 0f), new Vector3(0.025f, 0.10f, 0.025f), darkMaterial);
        antennaTip = CreatePart("AntennaTip", PrimitiveType.Sphere, antenna,
            new Vector3(0f, 0.23f, 0f), Vector3.one * 0.07f, glowMaterial);
    }

    private void BuildArms()
    {
        leftShoulder = BuildArm("L", torsoPivot, new Vector3(-0.48f, 0.46f, 0f), false);
        rightShoulder = BuildArm("R", torsoPivot, new Vector3(0.48f, 0.46f, 0f), true);
    }

    private Transform BuildArm(string side, Transform parent, Vector3 shoulderPosition, bool hasCannon)
    {
        bool isLeft = side == "L";
        Transform shoulder = NewPivot(side + "_Shoulder", parent);
        shoulder.localPosition = shoulderPosition;

        CreatePart(side + "_ShoulderJoint", PrimitiveType.Sphere, shoulder,
            Vector3.zero, Vector3.one * 0.23f, darkMaterial);
        CreatePart(side + "_ShoulderJointCap", PrimitiveType.Cylinder, shoulder,
            new Vector3(isLeft ? -0.13f : 0.13f, 0f, 0f), new Vector3(0.105f, 0.035f, 0.105f), jointMaterial,
            new Vector3(0f, 0f, 90f));
        CreatePart(side + "_ShoulderArmor", PrimitiveType.Cube, shoulder,
            new Vector3(isLeft ? -0.09f : 0.09f, 0.03f, 0f), new Vector3(0.28f, 0.22f, 0.34f), armorMaterial,
            new Vector3(0f, 0f, isLeft ? -8f : 8f));
        CreatePart(side + "_UpperArm", PrimitiveType.Capsule, shoulder,
            new Vector3(0f, -0.25f, 0f), new Vector3(0.14f, 0.25f, 0.14f), darkMaterial);

        Transform elbow = NewPivot(side + "_Elbow", shoulder);
        elbow.localPosition = new Vector3(0f, -0.51f, 0f);
        CreatePart(side + "_ElbowJoint", PrimitiveType.Sphere, elbow,
            Vector3.zero, Vector3.one * 0.15f, glowMaterial);

        if (hasCannon)
        {
            BuildCannon(elbow);
            rightElbow = elbow;
        }
        else
        {
            CreatePart("L_Forearm", PrimitiveType.Capsule, elbow,
                new Vector3(0f, -0.22f, 0f), new Vector3(0.15f, 0.23f, 0.15f), darkMaterial);
            CreatePart("L_ForearmArmor", PrimitiveType.Cube, elbow,
                new Vector3(0f, -0.21f, 0.07f), new Vector3(0.21f, 0.29f, 0.18f), armorMaterial);
            CreatePart("L_Hand", PrimitiveType.Sphere, elbow,
                new Vector3(0f, -0.47f, 0f), new Vector3(0.16f, 0.14f, 0.18f), darkMaterial);
            leftElbow = elbow;
        }

        return shoulder;
    }

    private void BuildCannon(Transform elbow)
    {
        CreatePart("CannonBody", PrimitiveType.Cylinder, elbow,
            new Vector3(0f, -0.25f, 0f), new Vector3(0.20f, 0.27f, 0.20f), darkMaterial);
        CreatePart("CannonArmor", PrimitiveType.Cube, elbow,
            new Vector3(0f, -0.23f, 0.08f), new Vector3(0.29f, 0.36f, 0.24f), armorMaterial);
        CreatePart("CannonEnergyCell", PrimitiveType.Cube, elbow,
            new Vector3(0.16f, -0.22f, 0.08f), new Vector3(0.035f, 0.20f, 0.09f), glowMaterial);

        Transform barrelPivot = NewPivot("CannonBarrelPivot", elbow);
        barrelPivot.localPosition = new Vector3(0f, -0.48f, 0.02f);
        barrelPivot.localRotation = Quaternion.Euler(90f, 0f, 0f);
        CreatePart("CannonBarrel", PrimitiveType.Cylinder, barrelPivot,
            new Vector3(0f, 0.13f, 0f), new Vector3(0.13f, 0.17f, 0.13f), darkMaterial);
        CreateRing("CannonRing", barrelPivot, new Vector3(0f, 0.31f, 0f), 0.16f, 0.035f, glowMaterial, 20);

        muzzle = NewPivot("EnergyMuzzle", barrelPivot);
        muzzle.localPosition = new Vector3(0f, 0.36f, 0f);
        muzzle.localRotation = Quaternion.Euler(-90f, 0f, 0f);
    }

    private void BuildBackpack()
    {
        Transform backpack = NewPivot("CoreCarrierPack", torsoPivot);
        backpack.localPosition = new Vector3(0f, 0.26f, -0.23f);

        CreatePart("PackBody", PrimitiveType.Cube, backpack,
            Vector3.zero, new Vector3(0.43f, 0.43f, 0.22f), darkMaterial);
        CreatePart("PackArmor", PrimitiveType.Cube, backpack,
            new Vector3(0f, 0f, -0.13f), new Vector3(0.34f, 0.32f, 0.10f), armorMaterial);
        backpackGlow = CreateRing("PackEnergyRing", backpack,
            new Vector3(0f, 0f, -0.195f), 0.17f, 0.035f, glowMaterial, 24);
        backpackGlow.localRotation = Quaternion.Euler(90f, 0f, 0f);

        CreatePart("L_PackRail", PrimitiveType.Cube, backpack,
            new Vector3(-0.21f, 0f, -0.03f), new Vector3(0.045f, 0.36f, 0.10f), accentMaterial);
        CreatePart("R_PackRail", PrimitiveType.Cube, backpack,
            new Vector3(0.21f, 0f, -0.03f), new Vector3(0.045f, 0.36f, 0.10f), accentMaterial);
    }

    private void BuildCoreDisplay()
    {
        coreDisplay = NewPivot("CarriedCorePreview", torsoPivot);
        coreDisplay.localPosition = new Vector3(0f, 0.06f, 0.50f);

        CreatePart("CoreInner", PrimitiveType.Sphere, coreDisplay,
            Vector3.zero, Vector3.one * 0.23f, glowMaterial);
        Transform outer = NewPivot("CoreOuterCage", coreDisplay);
        CreateRing("CoreRing_X", outer, Vector3.zero, 0.19f, 0.025f, accentMaterial, 20)
            .localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreateRing("CoreRing_Y", outer, Vector3.zero, 0.19f, 0.025f, accentMaterial, 20);
        CreateRing("CoreRing_Z", outer, Vector3.zero, 0.19f, 0.025f, accentMaterial, 20)
            .localRotation = Quaternion.Euler(90f, 0f, 0f);
        outer.gameObject.AddComponent<CoreOrbitMarker>();
        coreDisplay.gameObject.SetActive(showCarriedCore);
    }

    private void AnimateRobot()
    {
        Vector3 velocity = body != null ? body.velocity : Vector3.zero;
        velocity.y = 0f;
        float targetSpeed = Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.1f, controller.walkSpeed));
        smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, targetSpeed, animationResponsiveness * Time.deltaTime);

        float time = Time.time;
        float runPhase = time * Mathf.Lerp(3.5f, 10f, smoothedSpeed);
        float stride = Mathf.Sin(runPhase) * runLimbAngle * smoothedSpeed;
        float bob = Mathf.Abs(Mathf.Sin(runPhase)) * runBobHeight * smoothedSpeed;
        float idleBob = Mathf.Sin(time * 2.2f) * 0.012f * (1f - smoothedSpeed);

        visualRoot.localPosition = baseRootPosition + Vector3.up * (bob + idleBob);

        Quaternion torsoTarget = Quaternion.Euler(
            Mathf.Lerp(0f, 7f, smoothedSpeed) - fireKick * 3f,
            Mathf.Sin(time * 1.3f) * 1.2f * (1f - smoothedSpeed),
            -Mathf.Sin(runPhase) * 2.5f * smoothedSpeed);
        torsoPivot.localRotation = SmoothRotation(torsoPivot.localRotation, torsoTarget);

        Quaternion headTarget = Quaternion.Euler(
            Mathf.Sin(time * 1.7f) * 1.5f,
            Mathf.Sin(time * 0.85f) * 4f * (1f - smoothedSpeed),
            0f);
        headPivot.localRotation = SmoothRotation(headPivot.localRotation, headTarget);

        bool carrying = showCarriedCore;
        Quaternion leftArmTarget = carrying
            ? Quaternion.Euler(-68f, -18f, -18f)
            : Quaternion.Euler(-stride, 0f, -4f);
        Quaternion rightArmTarget = Quaternion.Euler(-68f - fireKick * 9f, 4f, 8f);
        leftShoulder.localRotation = SmoothRotation(leftShoulder.localRotation, leftArmTarget);
        rightShoulder.localRotation = SmoothRotation(rightShoulder.localRotation, rightArmTarget);

        Quaternion leftElbowTarget = carrying ? Quaternion.Euler(-42f, 8f, 20f) : Quaternion.identity;
        leftElbow.localRotation = SmoothRotation(leftElbow.localRotation, leftElbowTarget);
        rightElbow.localRotation = SmoothRotation(rightElbow.localRotation,
            Quaternion.Euler(68f + fireKick * 7f, 0f, 0f));

        leftHip.localRotation = SmoothRotation(leftHip.localRotation, Quaternion.Euler(stride, 0f, 0f));
        rightHip.localRotation = SmoothRotation(rightHip.localRotation, Quaternion.Euler(-stride, 0f, 0f));
        leftKnee.localRotation = SmoothRotation(leftKnee.localRotation,
            Quaternion.Euler(Mathf.Max(0f, -stride) * 0.65f, 0f, 0f));
        rightKnee.localRotation = SmoothRotation(rightKnee.localRotation,
            Quaternion.Euler(Mathf.Max(0f, stride) * 0.65f, 0f, 0f));

        fireKick = Mathf.MoveTowards(fireKick, 0f, Time.deltaTime * 9f);
        hitFlash = Mathf.MoveTowards(hitFlash, 0f, Time.deltaTime * 5f);

        float pulse = 1f + Mathf.Sin(time * 5f) * 0.08f;
        float alert = 1f + Mathf.Abs(Mathf.Sin(time * 9f)) * 0.28f * alertPulse;
        if (antennaTip != null)
            antennaTip.localScale = Vector3.one * 0.07f * pulse * alert;
        if (backpackGlow != null)
            backpackGlow.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(time * 3.5f));

        float thrusterScale = Mathf.Lerp(0.6f, 1.35f, smoothedSpeed);
        if (leftThrusterGlow != null)
            leftThrusterGlow.localScale = new Vector3(0.58f, 0.16f * thrusterScale, 0.58f);
        if (rightThrusterGlow != null)
            rightThrusterGlow.localScale = new Vector3(0.58f, 0.16f * thrusterScale, 0.58f);

        ApplyHitFlash();
    }

    private Quaternion SmoothRotation(Quaternion current, Quaternion target)
    {
        float blend = 1f - Mathf.Exp(-animationResponsiveness * Time.deltaTime);
        return Quaternion.Slerp(current, target, blend);
    }

    private void ApplyPalette(bool isLocal, bool force)
    {
        if (!force && paletteIsLocal == isLocal) return;
        paletteIsLocal = isLocal;

        Color armor = isLocal ? new Color(0.08f, 0.22f, 0.31f) : new Color(0.32f, 0.13f, 0.055f);
        Color accent = isLocal ? new Color(0.12f, 0.62f, 0.78f) : new Color(0.95f, 0.38f, 0.08f);
        Color glow = isLocal ? new Color(0.08f, 0.92f, 1f) : new Color(1f, 0.48f, 0.10f);

        SetMaterialColor(armorMaterial, armor, false);
        SetMaterialColor(accentMaterial, accent, false);
        SetMaterialColor(glowMaterial, glow, true);
    }

    private void ApplyHitFlash()
    {
        if (armorMaterial == null) return;

        Color normal = paletteIsLocal ? new Color(0.08f, 0.22f, 0.31f) : new Color(0.32f, 0.13f, 0.055f);
        Color color = Color.Lerp(normal, Color.white, hitFlash);
        SetMaterialColor(armorMaterial, color, false);
    }

    private void CreateMaterials()
    {
        armorMaterial = CreateMaterial("CR_Armor", new Color(0.08f, 0.22f, 0.31f), 0.72f, 0.18f, false);
        darkMaterial = CreateMaterial("CR_DarkMetal", new Color(0.025f, 0.035f, 0.045f), 0.86f, 0.55f, false);
        accentMaterial = CreateMaterial("CR_Accent", new Color(0.12f, 0.62f, 0.78f), 0.58f, 0.20f, false);
        glowMaterial = CreateMaterial("CR_Energy", new Color(0.08f, 0.92f, 1f), 0.2f, 0f, true);
        jointMaterial = CreateMaterial("CR_JointTrim", new Color(0.92f, 0.39f, 0.09f), 0.65f, 0.62f, false);
    }

    private static Material CreateMaterial(string materialName, Color color, float smoothness, float metallic, bool emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = materialName };
        SetMaterialColor(material, color, emission);

        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        return material;
    }

    private static void SetMaterialColor(Material material, Color color, bool emission)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 3.2f);
        }
    }

    private static Transform NewPivot(string objectName, Transform parent)
    {
        GameObject pivot = new GameObject(objectName);
        pivot.transform.SetParent(parent, false);
        return pivot.transform;
    }

    private static Transform CreatePart(
        string objectName,
        PrimitiveType primitive,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Vector3 localEuler = default(Vector3))
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEuler);
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            // Destroyはフレーム終端まで遅延するため、先に無効化しないと
            // 生成直後だけ親Rigidbodyの複合Colliderになり、プレイヤーを押し出してしまう。
            collider.enabled = false;
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return part.transform;
    }

    private static Transform CreateRing(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        float radius,
        float thickness,
        Material material,
        int segments)
    {
        GameObject ring = new GameObject(objectName);
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = localPosition;

        MeshFilter filter = ring.AddComponent<MeshFilter>();
        MeshRenderer renderer = ring.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        int vertexCount = segments * 4;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            float nextAngle = Mathf.PI * 2f * (i + 1) / segments;
            Vector3 innerA = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius - thickness);
            Vector3 outerA = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 innerB = new Vector3(Mathf.Cos(nextAngle), 0f, Mathf.Sin(nextAngle)) * (radius - thickness);
            Vector3 outerB = new Vector3(Mathf.Cos(nextAngle), 0f, Mathf.Sin(nextAngle)) * radius;

            int vertex = i * 4;
            vertices[vertex] = innerA;
            vertices[vertex + 1] = outerA;
            vertices[vertex + 2] = innerB;
            vertices[vertex + 3] = outerB;
            normals[vertex] = normals[vertex + 1] = normals[vertex + 2] = normals[vertex + 3] = Vector3.up;

            int triangle = i * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 1;
            triangles[triangle + 2] = vertex + 2;
            triangles[triangle + 3] = vertex + 2;
            triangles[triangle + 4] = vertex + 1;
            triangles[triangle + 5] = vertex + 3;
        }

        Mesh mesh = new Mesh { name = objectName + "Mesh" };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
        return ring.transform;
    }

    private void CreateMuzzleFlash()
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "CORE_RUNNER_MuzzleFlash";
        flash.transform.position = MuzzlePosition;
        flash.transform.localScale = Vector3.one * 0.18f;

        Collider collider = flash.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
        Renderer renderer = flash.GetComponent<Renderer>();
        if (renderer != null) renderer.material = glowMaterial;

        flash.AddComponent<RobotMuzzleFlash>();
    }

    private void ProtectBodyDuringBuild()
    {
        if (body == null || body.isKinematic) return;

        restoreDynamicBodyAfterBuild = true;
        bodyUsedGravityBeforeBuild = body.useGravity;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
    }

    private IEnumerator FinishBuildSafely()
    {
        // 生成したPrimitiveのCollider削除がUnity側へ反映されるまで物理演算を止める。
        yield return null;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();

        if (body != null && restoreDynamicBodyAfterBuild)
        {
            RestoreBodyAfterBuild();
        }
    }

    private void RestoreBodyAfterBuild()
    {
        if (body == null || !restoreDynamicBodyAfterBuild) return;

        body.isKinematic = false;
        body.useGravity = bodyUsedGravityBeforeBuild;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        restoreDynamicBodyAfterBuild = false;
    }

    private static void DestroyMaterial(Material material)
    {
        if (material != null)
            Destroy(material);
    }
}
