using UnityEngine;

/// <summary>通常攻撃に合わせて上半身の発射モーションを重ねる。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class PlayerFireAnimation : MonoBehaviour
{
    private const string LayerName = "PlayerFire";
    private const string TimeParameter = "FirePoseTime";
    private const float ClipSeconds = 0.45f;
    private const float ReleasePoseSeconds = 7f / 60f;
    private const float RecoveryIdleSeconds = 0.01f;

    public float GetMovementLockSeconds(float windup)
    {
        return PrepareAnimator()
            ? Mathf.Max(0f, windup) + ClipSeconds - ReleasePoseSeconds + RecoveryIdleSeconds
            : Mathf.Max(0f, windup);
    }
    private static readonly int TimeParameterId = Animator.StringToHash(TimeParameter);
    private static readonly int StateId = Animator.StringToHash("PlayerFire.PalmShot");

    private Animator animator;
    private PlayerController player;
    private RuntimeAnimatorController checkedController;
    private int layerIndex = -1;
    private bool playing, waitingForRelease;
    private float startedAt, releasedAt, windupSeconds, weight, poseTime;

    public static PlayerFireAnimation GetOrCreate(GameObject actor)
    {
        if (actor == null || actor.GetComponent<Animator>() == null) return null;
        var animation = actor.GetComponent<PlayerFireAnimation>();
        return animation != null ? animation : actor.AddComponent<PlayerFireAnimation>();
    }

    public static void NotifyProjectile(GameObject owner)
    {
        if (owner == null) return;
        var controller = owner.GetComponent<PlayerController>();
        // 自分の入力で既に再生している分を、受信した弾でもう一度再生しない。
        if (controller != null && controller.isLocalPlayer) return;
        var animation = GetOrCreate(owner);
        if (animation != null) animation.Begin(0f, true);
    }

    public void BeginWindup(float seconds) { Begin(seconds, false); }

    public void MarkReleased()
    {
        if (!playing || !waitingForRelease) return;
        waitingForRelease = false;
        releasedAt = Time.time;
        // 実際の発射がフレーム待ちで遅れた場合も、モーション終了前に歩き出さない。
        if (player != null && player.isLocalPlayer)
            player.ExtendShotLock(ClipSeconds - ReleasePoseSeconds + RecoveryIdleSeconds);
    }

    public void Cancel()
    {
        playing = false;
        waitingForRelease = false;
    }

    private bool CanAnimate()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (player != null && (player.IsDashing || player.IsFading || player.isAction || player.isPlayerMoveStop))
            return false;
        var state = GetComponent<CoopPlayerStateController>();
        return state == null || !state.AttackBlocked;
    }

    private bool PrepareAnimator()
    {
        if (animator == null) animator = GetComponent<Animator>();
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
            if (layerIndex < 0 || !hasTime || !animator.HasState(layerIndex, StateId))
            {
                layerIndex = -1;
                Debug.LogWarning("[PlayerFireAnimation] 発射用レイヤーがありません。同梱のPlayerAnimation.controllerを反映してください。", this);
            }
            playing = waitingForRelease = false;
            weight = 0f;
        }
        return layerIndex >= 0;
    }

    private void Begin(float seconds, bool released)
    {
        if (!CanAnimate() || !PrepareAnimator()) return;
        startedAt = releasedAt = Time.time;
        windupSeconds = Mathf.Max(0f, seconds);
        waitingForRelease = !released;
        playing = true;
        poseTime = released ? ReleasePoseSeconds / ClipSeconds : 0f;
        weight = 0f;
        animator.SetFloat(TimeParameterId, poseTime);
        animator.Play(StateId, layerIndex, 0f);
        animator.SetLayerWeight(layerIndex, 0f);
    }

    private void Update()
    {
        if (!PrepareAnimator()) return;
        if (playing && !CanAnimate()) Cancel();

        if (playing)
        {
            float elapsed = Mathf.Max(0f, Time.time - startedAt);
            float remaining = ClipSeconds - ReleasePoseSeconds - Mathf.Max(0f, Time.time - releasedAt);
            if (waitingForRelease)
                poseTime = ReleasePoseSeconds / ClipSeconds * (windupSeconds > 0f ? Mathf.Clamp01(elapsed / windupSeconds) : 1f);
            else
                poseTime = Mathf.Clamp01((ReleasePoseSeconds + Time.time - releasedAt) / ClipSeconds);

            float enter = Mathf.SmoothStep(0f, 1f, elapsed / 0.035f);
            float exit = waitingForRelease ? 1f : Mathf.SmoothStep(0f, 1f, remaining / 0.1f);
            weight = Mathf.Min(enter, exit);
            if (!waitingForRelease && remaining <= 0f) playing = false;
        }
        else
            weight = Mathf.MoveTowards(weight, 0f, Time.deltaTime / 0.08f);

        animator.SetFloat(TimeParameterId, poseTime);
        animator.SetLayerWeight(layerIndex, weight);
    }

    private void OnDisable()
    {
        Cancel();
        weight = 0f;
        if (animator != null && animator.isActiveAndEnabled &&
            animator.runtimeAnimatorController == checkedController && layerIndex >= 0 && layerIndex < animator.layerCount)
            animator.SetLayerWeight(layerIndex, 0f);
    }
}
