using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoreCarryController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Animator animator;

    [Header("コアの位置")]
    [SerializeField] private Vector3 coreLocalPosition = new Vector3(0f, 0.05f, 0.38f);
    [SerializeField] private Vector3 coreLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 coreLocalScale = Vector3.one;

    [Header("手の位置（コア中央からの相対位置）")]
    [SerializeField] private Vector3 leftHandOffset = new Vector3(-0.22f, 0f, 0f);
    [SerializeField] private Vector3 rightHandOffset = new Vector3(0.22f, 0f, 0f);
    [SerializeField] private Vector3 leftHandEulerAngles = new Vector3(0f, 90f, 90f);
    [SerializeField] private Vector3 rightHandEulerAngles = new Vector3(0f, -90f, -90f);

    [Header("肘の向き")]
    [SerializeField] private Vector3 leftElbowOffset = new Vector3(-0.45f, -0.05f, 0.05f);
    [SerializeField] private Vector3 rightElbowOffset = new Vector3(0.45f, -0.05f, 0.05f);

    [Header("補間")]
    [SerializeField, Min(0.01f)] private float ikBlendSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float handPositionWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float handRotationWeight = 0.8f;
    [SerializeField, Range(0f, 1f)] private float elbowHintWeight = 0.65f;

    private Transform carrySocket;
    private Transform leftHandTarget;
    private Transform rightHandTarget;
    private Transform leftElbowHint;
    private Transform rightElbowHint;

    private GameObject carriedCore;
    private Rigidbody carriedRigidbody;
    private bool previousUseGravity;
    private bool previousIsKinematic;
    private readonly List<ColliderState> colliderStates = new List<ColliderState>();

    private float currentIkWeight;
    private float targetIkWeight;
    private CoreRunnerRobotVisual robotVisual;

    public bool IsCarrying => carriedCore != null;
    public GameObject CarriedCore => carriedCore;

    private struct ColliderState
    {
        public Collider Collider;
        public bool WasEnabled;
    }

    private void Awake()
    {
        robotVisual = GetComponent<CoreRunnerRobotVisual>();
        ResolveAnimator();
        CreateCarryRig();
    }

    private void Update()
    {
        currentIkWeight = Mathf.MoveTowards(
            currentIkWeight,
            targetIkWeight,
            ikBlendSpeed * Time.deltaTime);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman || carrySocket == null) return;

        ApplyHandIK(
            AvatarIKGoal.LeftHand,
            leftHandTarget,
            leftHandEulerAngles);

        ApplyHandIK(
            AvatarIKGoal.RightHand,
            rightHandTarget,
            rightHandEulerAngles);

        animator.SetIKHintPositionWeight(
            AvatarIKHint.LeftElbow,
            currentIkWeight * elbowHintWeight);
        animator.SetIKHintPosition(
            AvatarIKHint.LeftElbow,
            leftElbowHint.position);

        animator.SetIKHintPositionWeight(
            AvatarIKHint.RightElbow,
            currentIkWeight * elbowHintWeight);
        animator.SetIKHintPosition(
            AvatarIKHint.RightElbow,
            rightElbowHint.position);
    }

    public void BeginCarry(GameObject core)
    {
        if (core == null || core == carriedCore) return;

        if (IsCarrying)
        {
            ReleaseCore(transform.position + transform.forward);
        }

        ResolveAnimator();
        CreateCarryRig();

        carriedCore = core;
        CacheAndDisablePhysics(core);

        Transform coreTransform = core.transform;
        coreTransform.SetParent(carrySocket, false);
        coreTransform.localPosition = Vector3.zero;
        coreTransform.localRotation = Quaternion.identity;
        coreTransform.localScale = coreLocalScale;

        targetIkWeight = 1f;
        ResolveRobotVisual();
        if (robotVisual != null)
        {
            core.SetActive(false);
            robotVisual.SetCoreHeld(true);
        }
    }

    public GameObject ReleaseCore(Vector3 worldPosition)
    {
        if (!IsCarrying) return null;

        GameObject releasedCore = carriedCore;
        Transform coreTransform = releasedCore.transform;
        coreTransform.SetParent(null, true);
        coreTransform.position = worldPosition;
        releasedCore.SetActive(true);

        RestorePhysics();
        carriedCore = null;
        targetIkWeight = 0f;
        ResolveRobotVisual();
        robotVisual?.SetCoreHeld(false);

        return releasedCore;
    }

    public void SetCarryVisible(bool visible)
    {
        if (carriedCore != null)
        {
            ResolveRobotVisual();
            if (robotVisual != null)
            {
                carriedCore.SetActive(false);
                robotVisual.SetCoreHeld(visible);
            }
            else
            {
                carriedCore.SetActive(visible);
            }
        }
    }

    private void ResolveRobotVisual()
    {
        if (robotVisual == null)
            robotVisual = GetComponent<CoreRunnerRobotVisual>();
    }

    private void ResolveAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void CreateCarryRig()
    {
        if (carrySocket != null || animator == null || !animator.isHuman) return;

        Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null)
        {
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        }
        if (chest == null) return;

        carrySocket = CreateTarget("CoreCarrySocket", chest, coreLocalPosition);
        carrySocket.localRotation = Quaternion.Euler(coreLocalEulerAngles);

        leftHandTarget = CreateTarget("LeftHandTarget", carrySocket, leftHandOffset);
        rightHandTarget = CreateTarget("RightHandTarget", carrySocket, rightHandOffset);
        leftElbowHint = CreateTarget("LeftElbowHint", chest, leftElbowOffset);
        rightElbowHint = CreateTarget("RightElbowHint", chest, rightElbowOffset);
    }

    private static Transform CreateTarget(string targetName, Transform parent, Vector3 localPosition)
    {
        var targetObject = new GameObject(targetName);
        Transform target = targetObject.transform;
        target.SetParent(parent, false);
        target.localPosition = localPosition;
        target.localRotation = Quaternion.identity;
        return target;
    }

    private void ApplyHandIK(AvatarIKGoal goal, Transform target, Vector3 localEulerAngles)
    {
        if (target == null) return;

        float positionWeight = currentIkWeight * handPositionWeight;
        float rotationWeight = currentIkWeight * handRotationWeight;

        animator.SetIKPositionWeight(goal, positionWeight);
        animator.SetIKRotationWeight(goal, rotationWeight);
        animator.SetIKPosition(goal, target.position);
        animator.SetIKRotation(
            goal,
            target.rotation * Quaternion.Euler(localEulerAngles));
    }

    private void CacheAndDisablePhysics(GameObject core)
    {
        colliderStates.Clear();
        foreach (Collider coreCollider in core.GetComponentsInChildren<Collider>(true))
        {
            colliderStates.Add(new ColliderState
            {
                Collider = coreCollider,
                WasEnabled = coreCollider.enabled
            });
            coreCollider.enabled = false;
        }

        carriedRigidbody = core.GetComponent<Rigidbody>();
        if (carriedRigidbody == null) return;

        previousUseGravity = carriedRigidbody.useGravity;
        previousIsKinematic = carriedRigidbody.isKinematic;
        carriedRigidbody.velocity = Vector3.zero;
        carriedRigidbody.angularVelocity = Vector3.zero;
        carriedRigidbody.useGravity = false;
        carriedRigidbody.isKinematic = true;
    }

    private void RestorePhysics()
    {
        foreach (ColliderState state in colliderStates)
        {
            if (state.Collider != null)
            {
                state.Collider.enabled = state.WasEnabled;
            }
        }
        colliderStates.Clear();

        if (carriedRigidbody != null)
        {
            carriedRigidbody.isKinematic = previousIsKinematic;
            carriedRigidbody.useGravity = previousUseGravity;
            carriedRigidbody = null;
        }
    }
}
