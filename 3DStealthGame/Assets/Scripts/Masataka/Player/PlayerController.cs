using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの移動・動作・足音を管理するクラス。
/// 自分のプレイヤーのときだけ入力を受け付ける。
/// 相手のプレイヤーはサーバーから受け取った位置情報で動く。
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region インスペクター設定

    [Header("操作設定")]
    public bool isLocalPlayer = false;

    [Header("移動速度")]
    public float walkSpeed = 5.0f;
    [HideInInspector]
    public float crouchSpeed = 2.5f;

    [Header("移動の手触り")]
    [SerializeField, Min(0.1f)] private float acceleration = 34f;
    [SerializeField, Min(0.1f)] private float deceleration = 42f;
    [SerializeField, Min(0.1f)] private float turnSharpness = 18f;
    [SerializeField, Range(0f, 0.5f)] private float inputDeadZone = 0.1f;

    [Header("足音設定")]
    public float sneakVolume = 5f;
    public float walkVolume = 15f;
    public delegate void SoundEventHandler(Vector3 position, float volume);
    public event SoundEventHandler OnMakeSound;

    [Header("リスポーン演出")]
    public Image catchFadePanel;
    public Text catchText;

    #endregion

    #region フィールド

    private Rigidbody _rb;
    private Vector3 _aimDirection;
    private bool _hasAimDirection;
    private float shotLockUntil;
    private Vector3 shotFacing;
    public bool IsShotLocked => Time.time < shotLockUntil;
    public Vector3 GetMoveDirection() => new Vector3(_moveInput.x, 0f, _moveInput.y);
    public void BeginShotLock(Vector3 direction, float duration)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
        shotFacing = direction.normalized;
        shotLockUntil = Time.time + duration;
        Quaternion facing = Quaternion.LookRotation(shotFacing, Vector3.up);
        if (_rb != null && !_rb.isKinematic)
        {
            _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            _rb.rotation = facing;
        }
        else transform.rotation = facing;
    }
    public void CancelShotLock() { shotLockUntil = 0f; }

    // 入力管理
    private Vector2 _moveInput;
    private PlayerInput playerInput;
    private InputAction moveAction;
    // 旧パンチ処理との互換用。入力自体は後期版では購読しない。
    public System.Action OnPunchInput;

    Animator Am;
    GlobalCamera Ca;
    public bool isAnimationStart = false;

    public bool isAction = false;
    public bool isPlayerMoveStop = false; // 移動停止フラグ（スイッチ操作中など）
    public bool isSneaking = false;

    public bool _isFading = false; // フェード中フラグ
    public bool IsFading => _isFading;

    public string lastTrigger = ""; // 最後に発火した動作トリガー（同期用）

    private Transform currentRespawnPoint; // リスポーン地点

    #endregion

    #region Unityイベント

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        Am = GetComponent<Animator>();
        if (Am != null) Am.applyRootMotion = false;
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
            Ca = mainCamera.GetComponent<GlobalCamera>();

        // 入力管理の初期化
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            moveAction = playerInput.actions.FindAction("Move", false);


        // AIに生成させたプレイヤーの見た目変更。　ごちゃごちゃしてるので要検討
        /*if (GetComponent<CoreRunnerRobotVisual>() == null)
            gameObject.AddComponent<CoreRunnerRobotVisual>();*/

        if (playerInput != null && GetComponent<PlayerCombatController>() == null)
            gameObject.AddComponent<PlayerCombatController>();

        catchFadePanel = GameObject.Find("RespawnFadePanel")?.GetComponent<Image>();
        catchText = GameObject.Find("リスポーン時テキスト")?.GetComponent<Text>();
    }

    private void OnEnable()
    {
        // 後期版ではパンチとスニーク入力を使用しない。
        isSneaking = false;
    }

    private void OnDisable()
    {
        if (isLocalPlayer)
            SoundManager.Instance?.StopWalk();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        CaptureInput();

        bool isActuallyMoving = GetHorizontalSpeed() > 0.15f;

        if (Am != null)
        {
            Am.SetBool("Run", isActuallyMoving);
            Am.SetBool("Sneak", false);
        }

        if (isActuallyMoving)
            SoundManager.Instance?.StartWalk();
        else
            SoundManager.Instance?.StopWalk();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        ApplyMovement();
    }

    #endregion

    #region 入力管理

    /// <summary>攻撃ボタンが押されたとき</summary>
    public void OnPunch(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnPunchInput?.Invoke();
    }

    /// <summary>忍び歩きボタンが押されたとき</summary>
    public void OnSneakStart(InputAction.CallbackContext context)
    {
        isSneaking = true;
    }

    /// <summary>忍び歩きボタンが離されたとき</summary>
    public void OnSneakEnd(InputAction.CallbackContext context)
    {
        isSneaking = false;
    }

    #endregion

    #region 入力処理

    /// <summary>
    /// 入力を取得してアニメーションと足音を制御する
    /// </summary>
    private void CaptureInput()
    {
        // 移動停止中
        if (isPlayerMoveStop)
        {
            _moveInput = Vector2.zero;
            if (_rb != null) _rb.velocity = Vector3.zero;
            //Am.SetBool("Run", false);
            //Am.SetBool("Sneak", false);
            return;
        }

        _moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        if (_moveInput.sqrMagnitude < inputDeadZone * inputDeadZone)
            _moveInput = Vector2.zero;
        else
            _moveInput = Vector2.ClampMagnitude(_moveInput, 1f);

        if (Ca != null && Ca.IsTransitioning)
        {
            _moveInput = Vector2.zero;
        }

    }

    #endregion

    #region 移動処理

    /// <summary>
    /// 実際の移動・向き変更・足音発生を行う
    /// </summary>
    private void ApplyMovement()
    {
        if (IsShotLocked || isPlayerMoveStop)
        {
            if (_rb != null && !_rb.isKinematic)
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            return;
        }
        Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);

        if (_rb != null && !_rb.isKinematic)
        {
            Vector3 currentHorizontalVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            Vector3 targetHorizontalVelocity = moveDir * walkSpeed;
            float changeRate = moveDir.sqrMagnitude > 0f ? acceleration : deceleration;

            Vector3 nextHorizontalVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                targetHorizontalVelocity,
                changeRate * Time.fixedDeltaTime);

            _rb.velocity = new Vector3(
                nextHorizontalVelocity.x,
                _rb.velocity.y,
                nextHorizontalVelocity.z);
        }

        Vector3 facingDirection = moveDir;

        if (facingDirection.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
            float rotationBlend = 1f - Mathf.Exp(-turnSharpness * Time.fixedDeltaTime);

            if (_rb != null)
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, rotationBlend));
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationBlend);

        }

        if (moveDir.sqrMagnitude > 0f)
            MakeSound(transform.position, walkVolume);
    }

    private float GetHorizontalSpeed()
    {
        if (_rb == null) return 0f;
        return new Vector2(_rb.velocity.x, _rb.velocity.z).magnitude;
    }

    /// <summary>照準中に、移動方向とは別の方向へプレイヤーを向ける。</summary>
    public void SetAimDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.0001f) return;

        _aimDirection = worldDirection.normalized;
        _hasAimDirection = true;
    }

    /// <summary>照準を解除し、再び移動方向を向くようにする。</summary>
    public void ClearAimDirection()
    {
        _hasAimDirection = false;
    }

    public Vector3 GetFacingDirection()
    {
        return _hasAimDirection ? _aimDirection : transform.forward;
    }

    #endregion

    #region アクション処理

    /// <summary>
    /// 敵を攻撃するアニメーションを再生する
    /// </summary>
    public void PunchEnemy()
    {
        if (isAction) return;
        //isAction = true;
        Ca.ActionCameraTrue();
        Am.SetTrigger("PunchEnemy");
        lastTrigger = "PunchEnemy";
    }

    /// <summary>
    /// 敵への攻撃アニメーションの開始フラグを立てる
    /// </summary>
    public void StartAnimationEnemy()
    {
        isAnimationStart = true;
    }

    /// <summary>
    /// スイッチを操作するアニメーションを再生する
    /// </summary>
    public void PunchSwitch()
    {
        //if (isAction) return;
        print("スイッチアニメーション起動");
        //Am.SetBool("Run", false);
        //Am.SetBool("Sneak", false);
        Ca.ActionCameraTrue();
        Am.SetTrigger("PunchSwitch");
        lastTrigger = "PunchSwitch";
    }

    /// <summary>
    /// 現在の動作状態を文字列で返す（同期用）
    /// </summary>
    public string GetAnimState()
    {
        if (Am.GetBool("Run")) return "run";
        if (Am.GetBool("Sneak")) return "sneak";
        return "idle";
    }

    /// <summary>
    /// アクション終了時にアニメーションイベントから呼ばれる
    /// </summary>
    public void EndAction()
    {
        isAction = false;
    }

    /// <summary>
    /// 移動停止フラグを解除する
    /// </summary>
    public void EndMove()
    {
        isPlayerMoveStop = false;
        Am.SetTrigger("Idle");
        Ca.ActionCameraFalse();
    }

    #endregion

    #region 足音処理

    void MakeSound(Vector3 position, float volume)
    {
        OnMakeSound?.Invoke(position, volume);
    }

    public bool IsSneaking => Input.GetKey(KeyCode.LeftShift);

    #endregion

    #region リスポーン処理

    /// <summary>
    /// リスポーン地点を保存する
    /// </summary>
    public void SetRespawnPoint(Transform point)
    {
        currentRespawnPoint = point;
        Debug.Log("リスポーン地点更新：" + point.position);
    }

    /// <summary>
    /// リスポーン演出を開始する（自分が捕まった本人）
    /// </summary>
    public void Respawn()
    {
        if (currentRespawnPoint != null)
            StartCoroutine(RespawnWithEffect(true));
    }

    /// <summary>
    /// 相手プレイヤーがつかまったときにサーバーから呼ばれるリスポーン演出（通知なし）
    /// </summary>
    public void RespawnWithEffectPublic()
    {
        StartCoroutine(RespawnWithEffect(false));
    }

    /// <summary>
    /// 画面を暗転させてリスポーン位置に移動し、フェードで復帰する演出
    /// </summary>
    /// <param name="sendToServer">trueなら自分が捕まった本人としてサーバーに通知する</param>
    private IEnumerator RespawnWithEffect(bool sendToServer)
    {
        if (_isFading) yield break; // フェード中なら無視
        _isFading = true;

        SoundManager.Instance?.PlayRespawn();

        // 死亡時にアクションを封じてIdleに戻す
        isAction = true;
        isPlayerMoveStop = true;
        Am.SetBool("Run", false);
        Am.SetBool("Sneak", false);
        //Am.SetTrigger("Idle");

        if (sendToServer && StaminaManager.Instance != null)
        {
            if (StaminaManager.Instance.GetCurrentStamina() <= 4)
            {
                StaminaManager.Instance.SetStamina(5);
            }
        }


        // 発見時のテキストを表示
        if (catchText != null)
        {
            var c = catchText.color;
            c.a = 1f;
            catchText.color = c;
        }

        // 画面を暗転させる
        if (catchFadePanel != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                var c = catchFadePanel.color;
                c.a = Mathf.Lerp(0, 1, elapsed / 0.5f);
                catchFadePanel.color = c;
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.5f);

        // リスポーン位置に移動
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.position = currentRespawnPoint.position;
        transform.rotation = currentRespawnPoint.rotation;

        if (sendToServer)
        {
            PlayMetrics.AddDeath();

            var wsClient = FindObjectOfType<WebSocketClient>();
            if (wsClient != null) wsClient.SendRespawn(transform.position);
        }

        // 敵の警戒度をリセット
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            var em = e.GetComponent<EnemyManager>();
            if (em != null)
            {
                em.ResetRespawnFlag();
                em.currentAlertCount = em.alertCount;
            }
        }

        yield return new WaitForSeconds(0.3f);

        isAction = false;
        isPlayerMoveStop = false;

        if (catchFadePanel != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, elapsed / 0.5f);

                var c = catchFadePanel.color;
                c.a = alpha;
                catchFadePanel.color = c;

                if (catchText != null)
                {
                    var tc = catchText.color;
                    tc.a = alpha;
                    catchText.color = tc;
                }

                yield return null;
            }
        }

        if (catchFadePanel != null)
        {
            var c = catchFadePanel.color;
            c.a = 0f;
            catchFadePanel.color = c;
        }
        if (catchText != null)
        {
            var c = catchText.color;
            c.a = 0f;
            catchText.color = c;
        }


        // フェードイン完了後
        isAction = false;
        isPlayerMoveStop = false;
        _isFading = false; // フラグ解除
    }

    private IEnumerator CheckPosition()
    {
        yield return new WaitForSeconds(0.1f);
        Debug.Log("0.1秒後：" + transform.position);

        yield return new WaitForSeconds(0.4f);
        Debug.Log("0.5秒後：" + transform.position);
    }

    #endregion
}
