using UnityEngine;

/// <summary>被弾モーションを通常速度で一度だけ再生し、通常動作へ戻す。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class PlayerStaggerAnimation : MonoBehaviour
{
    private const string LayerName = "PlayerStagger";
    private const string TimeParameter = "StaggerPoseTime";
    private const float ClipSeconds = 1.333333333f;
    private static readonly int TimeParameterId = Animator.StringToHash(TimeParameter);
    private static readonly int PoseStateId = Animator.StringToHash("PlayerStagger.StaggerPose");

    [SerializeField, Min(0.01f)] private float blendInSeconds = 0.1f;
    [SerializeField, Min(0.01f)] private float blendOutSeconds = 0.18f;

    private CoopPlayerStateController state;
    private Animator animator;
    private RuntimeAnimatorController checkedController;
    private int layerIndex = -1;
    private bool wasStunned, hasReaction;
    private float startedAt, initialDuration, weight;

    public void Initialize(CoopPlayerStateController owner)
    {
        if (state == owner && animator != null) return;
        ResetPlayback();
        state = owner;
        animator = GetComponent<Animator>();
        checkedController = null;
        layerIndex = -1;
    }

    private bool PrepareAnimator()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return false;

        if (checkedController != animator.runtimeAnimatorController)
        {
            checkedController = animator.runtimeAnimatorController;
            layerIndex = animator.GetLayerIndex(LayerName);
            bool hasTime = false;
            foreach (var parameter in animator.parameters)
                if (parameter.nameHash == TimeParameterId && parameter.type == AnimatorControllerParameterType.Float)
                    hasTime = true;

            if (layerIndex < 0 || !hasTime || !animator.HasState(layerIndex, PoseStateId))
            {
                layerIndex = -1;
                Debug.LogWarning("[PlayerStaggerAnimation] 怯み用レイヤーがありません。同梱のPlayerAnimation.controllerを反映してください。", this);
            }
            weight = 0f;
            wasStunned = hasReaction = false;
        }
        return layerIndex >= 0;
    }

    private void Update()
    {
        if (state == null || !PrepareAnimator()) return;

        bool stunned = state.IsStunned;
        bool starting = stunned && !wasStunned;
        if (starting)
        {
            startedAt = Time.time;
            initialDuration = state.StunRemaining;
            hasReaction = true;
        }

        float elapsed = Mathf.Max(0f, Time.time - startedAt);
        // タイマーの長さで再生速度を変えず、途中のフレームも保持しない。
        animator.SetFloat(TimeParameterId, hasReaction ? Mathf.Clamp01(elapsed / ClipSeconds) : 1f);
        if (starting) animator.Play(PoseStateId, layerIndex, 0f);

        float fadeIn = Mathf.Max(0.001f, Mathf.Min(blendInSeconds, initialDuration * 0.25f));
        float fadeOut = Mathf.Max(0.001f, Mathf.Min(blendOutSeconds, initialDuration * 0.35f));
        float remaining = Mathf.Min(ClipSeconds - elapsed, state.StunRemaining);
        if (stunned && hasReaction && remaining > 0f)
        {
            float enter = Mathf.SmoothStep(0f, 1f, elapsed / fadeIn);
            float exit = Mathf.SmoothStep(0f, 1f, remaining / fadeOut);
            weight = Mathf.Min(enter, exit);
        }
        else
            weight = Mathf.MoveTowards(weight, 0f, Time.deltaTime / fadeOut);

        animator.SetLayerWeight(layerIndex, weight);
        wasStunned = stunned;
    }

    private void ResetPlayback()
    {
        if (animator != null && animator.isActiveAndEnabled &&
            animator.runtimeAnimatorController == checkedController && layerIndex >= 0 && layerIndex < animator.layerCount)
            animator.SetLayerWeight(layerIndex, 0f);
        weight = 0f;
        wasStunned = hasReaction = false;
    }

    private void OnDisable() { ResetPlayback(); }
    private void OnDestroy() { ResetPlayback(); }
}
