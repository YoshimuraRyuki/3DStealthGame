using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCoreCarrier : MonoBehaviour
{
    #region 宣言

    [Header("アタッチ設定")]
    [Tooltip("コアを保持・配置する位置")]
    [SerializeField] private Transform holdPoint;
    
    [Tooltip("コアを検知できる距離")]
    [SerializeField] private float interactRadius = 2.0f;

    [Tooltip("Coreのレイヤー（必要に応じて設定）")]
    [SerializeField] private LayerMask coreLayer = ~0;

    [Header("Core所持時の移動制限")]
    [SerializeField, Range(0.1f, 1.0f)] private float moveSpeedMultiplier = 0.6f;

    [Header("制限対象スクリプト")]
    [SerializeField] private MonoBehaviour playerCombatController;

    [Header("デバッグ情報")]
    [SerializeField] private Core currentHeldCore = null;

    private PlayerController playerController;
    private CoopPlayerStateController coopState;

    private float originalWalkSpeed; // 元の移動速度を記憶する変数
    private float pickupCooldownTimer = 0f;
    public bool HasCore => currentHeldCore != null;

    #endregion

    #region インプットアクション処理

    /// <summary>
    /// Aボタン押下時に実行するメソッド
    /// PlayerInputのアクションイベントまたはMessage送信に対応
    /// </summary>
    private void ExecuteInteract()
    {
        // 強奪直後のクールダウン中は入力を受け付けない（ワープ防止）
        if (pickupCooldownTimer > 0f) return;

        // ローカルプレイヤー以外は処理しない
        if (playerController != null && !playerController.isLocalPlayer) return;

        // 移動停止中・スタン中などの場合は操作をキャンセル
        if (playerController != null && playerController.isPlayerMoveStop) return;
        if (coopState != null && coopState.IsStunned) return;

        if (currentHeldCore != null)
        {
            // すでにコアを持っている場合は手放す
            ReleaseCore();
        }
        else
        {
            // コアを持っていない場合は周囲から取得を試みる
            TryPickUpCore();
        }
    }

    #endregion

    #region コアを持つ離す処理

    /// <summary>
    /// 周囲のCoreを探して取得を試みる
    /// </summary>
    private void TryPickUpCore()
    {
        if (pickupCooldownTimer > 0f) return;

        Core targetCore = null;
        float closestDistance = float.MaxValue;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactRadius, coreLayer);

        foreach (var col in hitColliders)
        {
            Core core = col.GetComponent<Core>();
            if (core == null) core = col.GetComponentInParent<Core>();

            if (core != null)
            {
                float dist = Vector3.Distance(transform.position, core.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    targetCore = core;
                }
            }
        }

        if (targetCore == null) return;

        bool isCarriedByActiveEnemy = false;
        if (targetCore.isCarried && targetCore.carrier != null)
        {
            EnemyManager enemy = targetCore.carrier.GetComponent<EnemyManager>();
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                isCarriedByActiveEnemy = true;
            }
        }

        // 生きている敵が手に持っている場合以外は、安全に拾う
        if (!isCarriedByActiveEnemy)
        {
            // 残存している古い参照があればリセット
            if (targetCore.isCarried && targetCore.carrier != transform)
            {
                targetCore.ResetCore();
            }

            targetCore.AttachToEnemy(holdPoint);
            currentHeldCore = targetCore;

            // 速度ペナルティの適用
            ApplySpeedPenalty();

            // 射撃スクリプトをオフにする
            SetCombatActive(false);

            Debug.Log($"[PlayerCoreCarrier] Core ({targetCore.name}) を無事に取得しました。");
        }
    }

    /// <summary>
    /// 保持しているCoreを手放す
    /// </summary>
    public void ReleaseCore()
    {
        if (currentHeldCore == null) return;

        Core coreToRelease = currentHeldCore;
        currentHeldCore = null;

        // コアのアタッチ・ターゲット状態を解除
        coreToRelease.ReleaseCore(transform);
        coreToRelease.ResetCore();

        // プレイヤーの少し前に配置
        Vector3 dropPosition = transform.position + transform.forward * 0.8f + Vector3.up * 0.3f;
        coreToRelease.transform.position = dropPosition;

        // 速度のリセット
        RestoreOriginalSpeed();
        
        // 射撃のリセット
        SetCombatActive(true);

        Debug.Log($"[PlayerCoreCarrier] Core ({coreToRelease.name}) を離しました。");
    }

    /// <summary>
    /// 敵などにコアを強奪された際に外部から呼ばれる処理
    /// </summary>
    public void ForceStealCore()
    {
        if (currentHeldCore == null) return;

        currentHeldCore = null;
        RestoreOriginalSpeed();
        SetCombatActive(true);

        // 強奪直後の連打や誤検知によるワープを防ぐためクールダウンを設定 (0.5秒)
        pickupCooldownTimer = 0.5f;

        Debug.Log($"[PlayerCoreCarrier] 敵にCoreを奪われました！");
    }

    #endregion

    #region 移動ペナルティ

    /// <summary>
    /// 移動速度にペナルティを適用する
    /// </summary>
    private void ApplySpeedPenalty()
    {
        if (playerController != null)
        {
            // 既に適用済みでなければ元の速度を保存
            if (Mathf.Approximately(playerController.walkSpeed, originalWalkSpeed))
            {
                originalWalkSpeed = playerController.walkSpeed;
            }
            playerController.walkSpeed = originalWalkSpeed * moveSpeedMultiplier;
        }
    }

    /// <summary>
    /// 移動速度を元の値に戻す
    /// </summary>
    private void RestoreOriginalSpeed()
    {
        if (playerController != null)
        {
            playerController.walkSpeed = originalWalkSpeed;
        }
    }

    #endregion

    #region 射撃ペナルティ

    /// <summary>
    /// 射撃用スクリプト
    /// </summary>
    private void SetCombatActive(bool isActive)
    {
        if (playerCombatController != null)
        {
            playerCombatController.enabled = isActive;
        }
    }

    #endregion

    private void OnDisable()
    {
        // オブジェクトが無効化された時などに元の速度に戻す安全処理
        if (currentHeldCore != null)
        {
            RestoreOriginalSpeed();
            SetCombatActive(true);
        }
    }

    #region Unityイベント
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        coopState = GetComponent<CoopPlayerStateController>();

        if (holdPoint == null)
        {
            holdPoint = transform;
        }

        // 初期状態の移動速度を保持
        if (playerController != null)
        {
            originalWalkSpeed = playerController.walkSpeed;
        }

        if (playerCombatController == null)
        {
            playerCombatController = GetComponent("PlayerCombatController") as MonoBehaviour;
        }
    }

    private void Update()
    {
        // タイマーのカウントダウン更新
        if (pickupCooldownTimer > 0f)
        {
            pickupCooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Send Messages モード用（PlayerInputのBehaviorがSend Messagesの場合に自動で呼ばれる）
    /// </summary>
    public void OnInteract(InputValue value)
    {
        // ボタンが押された瞬間のみ実行
        if (value.isPressed)
        {
            ExecuteInteract();
        }
    }

    /// <summary>
    /// Invoke Unity Events モード用（Eventsで直接指定する場合用）
    /// </summary>
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ExecuteInteract();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
    #endregion
}
