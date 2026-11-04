using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemyManager : MonoBehaviour
{
    #region 状態管理

    public enum EnemyState
    {
        Patrol,
        FocusPlayer,
        LookSoundPoint,
        FocusCore,
        CarryCore
    }

    [SerializeField] EnemyState enemyState;

    #endregion

    #region コンポーネント参照

    Light Sl;
    PlayerController Pc;
    public TextMeshProUGUI reactionText;
    private Transform cameraTransform;

    Animator animWall;
    Animator animEnemy;

    #endregion

    #region 巡回設定

    // 巡回ポイント
    GameObject startPoint;   // 最初のポイント
    GameObject targetPoint;  // 今向かっているポイント

    private List<GameObject> patrolPointList = new List<GameObject>();
    private int currentPointIndex = 0;

    [Header("巡回ポイント")]
    public GameObject[] movePoints;

    // 移動
    [Header("移動速度")]
    public float speed = 3f;
    [Header("追従時に停止する距離")]
    public float stoppingDistance = 3f;
    [Header("プレイヤー追従時の移動速度")]
    public float chaseSpeed = 1.5f;

    [Header("回転速度")]
    public float rotateSpeed = 3f;
    public float stoprotateSpeed = 50f;

    // 停止
    [Header("停止時間")]
    public float stopMoveCooldown = 3f;
    float currentTime = 0f;

    [Header("巡回エリア設定")]
    [Tooltip("敵が巡回ポイントを探す最大距離")]
    public float maxPatrolDistance = 20f;

    // 追従後に別のルートに行かないよう固定するグループID
    private int myGroupID = -1;

    #endregion

    #region 視界検知

    Transform targetPlayer; // プレイヤー検知

    [Header("検知する距離")]
    public float viewRadius = 10f;
    [Header("検知する角度")]
    public float viewAngle = 90f;

    [Header("警戒カウント")]
    public float alertCount = 6;
    public float currentAlertCount;

    [Header("見失ってから巡回に戻る時間")]
    public float lostPlayerTime = 2f;
    float lostTimer = 0f;
    private Transform _alertTarget; // 警戒度を上げたプレイヤー
    [Header("貫通例キャスト"), SerializeField]
    private LayerMask visionMask;

    #endregion

    #region 音検知

    [Header("プレイヤー")]
    public Transform player;
    [Header("敵が足音の聞こえる範囲")]
    public float hearingRange = 15f;
    [Header("音が聞こえなくなってからあきらめるまでの時間")]
    public float alertDuration = 3f;
    private float alertTimer = 0f;

    public Vector3 GetLastSoundPosition() => lastSoundPosition;
    private bool isHearingSound = false;     // 今、音が聞こえているかどうかのフラグ
    private Vector3 lastSoundPosition;       // 最後に音が聞こえた場所
    private float _remoteSoundCooldown = 0f; // Player2音検知用クールダウン

    #endregion

    #region コア設定

    [Header("Core設定")]
    public float coreRadius = 8f;
    public Transform coreHoldPoint; // 手元や背中にCoreを付ける位置
    private Core targetCore = null;
    private Core carriedCore = null; // 現在持っているCore

    #endregion

    #region コア追尾用ナビメッシュ

    private NavMeshAgent agent;

    #endregion

    #region フラグ管理

    bool isAlerted = false;
    bool isFoundPlayer = false;
    bool isReaction = false;
    public bool isStopMove = false; // 敵が止まっているときに動きを完全に止める

    private bool _soundRegistered = false;
    private bool _isRespawning = false;

    [HideInInspector] public bool isRemoteControlled = false; // player2側はtrue


    #endregion

    #region ギミック連携

    [Header("スイッチギミック用ID")]
    public int enemyID;
    public int gimmickCount = 0;
    [Header("いくつのボタンでギミック解除するか"), SerializeField]
    public int switchCount;

    #endregion

    #region デバック

    [Header("Gizmos Debug")]
    [SerializeField] bool showViewDebug = true;   // 視界・レイキャスト
    [SerializeField] bool showSoundDebug = true;  // 音検知
    private Vector3 debugSoundPos;
    private float debugSoundRange;
    private float debugTimer;

    #endregion

    #region 巡回ポイント決定

    /// <summary>
    /// 一番近いポイントを取得
    /// </summary>
    /// <returns></returns>
    void StartPoint()
    {
        GameObject[] allPoints = GameObject.FindGameObjectsWithTag("Point");
        patrolPointList.Clear();

        if (allPoints.Length == 0) return;

        if (myGroupID == -1)
        {
            GameObject nearestPoint = GetNearestPoint(allPoints);
            if (nearestPoint == null) return;

            int nearestID = GetIDFromName(nearestPoint.name);
            if (nearestID == -1)
            {
                BackupPatrolSetup(allPoints);
                return;
            }

            myGroupID = nearestID / 100; // グループIDを決定して保存
        }

        // 保存された myGroupID に属するポイントのみを集める
        foreach (GameObject pt in allPoints)
        {
            int ptID = GetIDFromName(pt.name);
            if (ptID != -1 && (ptID / 100) == myGroupID)
            {
                patrolPointList.Add(pt);
            }
        }

        // IDの昇順にソート (101 -> 102 -> 103...)
        patrolPointList.Sort((a, b) =>
        {
            int idA = GetIDFromName(a.name);
            int idB = GetIDFromName(b.name);
            return idA.CompareTo(idB);
        });

        // 巡回ポイントの中で復帰時に一番近いポイントから再開
        if (patrolPointList.Count > 0)
        {
            GameObject nearestInGroup = GetNearestPoint(patrolPointList.ToArray());
            currentPointIndex = patrolPointList.IndexOf(nearestInGroup);
            if (currentPointIndex == -1) currentPointIndex = 0;

            targetPoint = patrolPointList[currentPointIndex];
        }
    }

    int GetIDFromName(string objectName)
    {
        string[] parts = objectName.Split('_');
        if (parts.Length >= 2 && int.TryParse(parts[1], out int id))
        {
            return id;
        }
        return -1;
    }

    GameObject GetNearestPoint(GameObject[] points)
    {
        GameObject nearest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (var pt in points)
        {
            float dist = Vector3.SqrMagnitude(pt.transform.position - currentPos);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = pt;
            }
        }
        return nearest;
    }

    void BackupPatrolSetup(GameObject[] allPoints)
    {
        Vector3 spawnPos = transform.position;
        foreach (GameObject pt in allPoints)
        {
            if (Vector3.Distance(spawnPos, pt.transform.position) <= maxPatrolDistance)
            {
                patrolPointList.Add(pt);
            }
        }
        patrolPointList.Sort((a, b) => string.Compare(a.name, b.name));

        if (patrolPointList.Count > 0)
        {
            currentPointIndex = 0;
            targetPoint = patrolPointList[currentPointIndex];
        }
    }

    /// <summary>
    /// 次のポイントを順番通りに取得する
    /// </summary>
    GameObject GetNextPoint()
    {
        if (patrolPointList.Count == 0) return null;

        // 次のインデックスへ
        currentPointIndex = (currentPointIndex + 1) % patrolPointList.Count;

        return patrolPointList[currentPointIndex];
    }

    #endregion

    #region 巡回ポイント移動

    /// <summary>
    /// ポイントに向かって移動
    /// </summary>
    void MoveEnemy()
    {
        if (targetPoint == null) return;

        if (!isStopMove)
        {
            MoveToPoint(targetPoint); // 実際に移動させる処理
        }

        // 近づいたら切り替え
        if (Vector3.Distance(transform.position, targetPoint.transform.position) < 0.1f)
        {
            isStopMove = true;
            currentTime += Time.deltaTime; // 目的の位置に着いたら少し止める
            //SearchPlayer();                // 止まっている間視点を動かす

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            if (currentTime >= stopMoveCooldown)
            {
                // 次のポイントをランダム取得
                targetPoint = GetNextPoint();
                currentTime = 0;
                isStopMove = false;
            }
        }
    }

    /// <summary>
    /// 取得したポイントに向かって移動
    /// </summary>
    /// <param name="point"></param>
    void MoveToPoint(GameObject point)
    {
        // 目的地方向
        Vector3 targetPos = point.transform.position;
        Vector3 direction = targetPos - transform.position;
        direction.y = 0;

        float distance = direction.magnitude;
        if (distance < 0.01f) return;

        // 向きたい回転を計算
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // まず視点を巡回ポイントの方向になめらかに向け直す
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);

        // 現在の向きと目的地の角度差を取得
        float angle = Quaternion.Angle(transform.rotation, lookRotation);

        // 前方に壁があるかレイキャストでチェック
        bool isWallAhead = Physics.Raycast(transform.position + Vector3.up * 0.5f, direction.normalized, distance, visionMask);
        // 壁がある場合、またはNavMeshAgentがまだ目的地追尾中の場合はNavMeshAgentで迂回移動
        if (isWallAhead)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = speed;
                agent.SetDestination(targetPos);

                // NavMeshAgentの移動方向にも視点を合わせる
                if (agent.desiredVelocity != Vector3.zero)
                {
                    Quaternion agentRot = Quaternion.LookRotation(new Vector3(agent.desiredVelocity.x, 0, agent.desiredVelocity.z));
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, agentRot, rotateSpeed * Time.deltaTime);
                }
            }
            return;
        }
        // 直線上に壁がない場合はNavMeshAgentを停止して、従来の向き調整と直線移動を行う
        if (agent != null && agent.isOnNavMesh && agent.hasPath)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (angle < 10f)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    #endregion

    #region プレイヤー発見処理

    /// <summary>
    /// プレイヤーを発見するさせる時の処理
    /// Player1とPlayer2両方を検知するように変更
    /// </summary>
    void PlayerFound()
    {
        if (enemyState == EnemyState.FocusCore)
        {
            isFoundPlayer = false;
            return;
        }

        // 両方のプレイヤーを確認して近い方をターゲットに
        GameObject p1 = GameObject.FindWithTag("Player1");
        GameObject p2 = GameObject.FindWithTag("Player2");

        if (enemyState == EnemyState.FocusPlayer && isFoundPlayer && targetPlayer != null)
        {
            // 現在ロックオン中のターゲットがまだ視界内にいるかをチェックする
            bool stillVisible = CheckSinglePlayerVision(targetPlayer);

            if (stillVisible)
            {
                _alertTarget = targetPlayer;

                //（TowardThePlayerの中の isHearingSound 条件に縛られないように、本来は別処理にするか、フラグ管理を見直すのが理想です）
                FocusPlayer();
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);                
                if (distanceToPlayer > stoppingDistance)
                {
                    // chaseSpeed（遅くした追従速度）を使って移動させる
                    transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, chaseSpeed * Time.deltaTime);
                }
                return;
            }
            else
            {
                // 見失った瞬間、目視フラグを一旦折る
                isFoundPlayer = false;

                if (reactionText != null)
                {
                    reactionText.text = "?";
                    reactionText.gameObject.SetActive(true);
                }

                lastSoundPosition = targetPlayer.position;
                currentTime = 0; // タイマーリセット
                enemyState = EnemyState.LookSoundPoint; // 音を確認するステートへ
                
                return;
            }
        }

        Transform closest = null;
        float minDist = Mathf.Infinity;
        foreach (var p in new[] { p1, p2 })
        {
            if (p == null) continue;

            // 単に距離が近いだけでなく、視界に入っているかも同時にチェックする
            if (CheckSinglePlayerVision(p.transform))
            {
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = p.transform;
                }
            }
        }

        // 視界に入っているプレイヤーがいればターゲットに設定
        if (closest != null)
        {
            if (!isFoundPlayer)
            {
                if (reactionText != null)
                {
                    reactionText.text = "!";
                    reactionText.gameObject.SetActive(true); // ビックリマーク表示
                    isReaction = true;
                }

                // 待機タイマーなどをリセット（音検知と同様に、動きを再開させるため）
                currentTime = 0;
            }

            targetPlayer = closest;
            isFoundPlayer = true;
            _alertTarget = targetPlayer;

            if (gameObject.tag == "StrongEnemy") return;

            enemyState = EnemyState.FocusPlayer;
        }
    }

    /// <summary>
    /// 特定のプレイヤーが視界に入っているかどうかを判定
    /// </summary>
    /// <param name="playerTransform"></param>
    /// <returns></returns>
    bool CheckSinglePlayerVision(Transform playerTransform)
    {
        if (playerTransform == null) return false;

        Vector3 dirToTarget = (playerTransform.position - transform.position).normalized;
        float dstToTarget = Vector3.Distance(transform.position, playerTransform.position);

        if (dstToTarget < viewRadius)
        {
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);
            if (angleToTarget < viewAngle / 2f)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, dirToTarget, out hit, dstToTarget, visionMask))
                {
                    if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    #endregion

    #region 警戒状態処理

    /// <summary>
    /// 警戒状態処理
    /// </summary>
    void AlertFunction()
    {
        // 誰も目視しておらず、かつ音での警戒状態でもない（完全な通常巡回中）なら、警戒度計算もリスポーン判定も一切行わない
        if (!isFoundPlayer && !isAlerted)
        {
            currentAlertCount = alertCount; // 常に最大値で安定させる
            if (Sl != null) Sl.color = new Color(0.827f, 0.851f, 0.439f); // 通常時のライト色
            return;
        }

        // 警戒時間
        if (isFoundPlayer)
        {
            currentAlertCount -= Time.deltaTime;
        }
        else
        {
            currentAlertCount += Time.deltaTime;
        }

        currentAlertCount = Mathf.Clamp(currentAlertCount, 0, alertCount);

        // 見失った時の処理
        if (enemyState == EnemyState.FocusPlayer && !isAlerted)
        {
            if (isFoundPlayer)
            {
                // 見えている間はリセット
                lostTimer = lostPlayerTime;
            }
            else
            {
                // 見失ったら減算
                lostTimer -= Time.deltaTime;

                // 一定時間見失ったら巡回へ
                if (lostTimer <= 0f)
                {
                    ResetPatrolState();
                    enemyState = EnemyState.Patrol;
                }
            }
        }

        AlertColor(); // スポットライト色変更

        // 警戒状態のときのみタイマーを減算
        if (isAlerted)
        {
            alertTimer -= Time.deltaTime;

            // 音が聞こえなくなって一定時間経った
            if (alertTimer <= 0)
            {
                CancelAlert();
            }
        }
    }

    /// <summary>
    /// スポットライトの色変更
    /// </summary>
    void AlertColor()
    {
        if (Sl == null) return;
        if (currentAlertCount <= 0 && !_isRespawning)
        {
            _isRespawning = true;
            MissionManager.Instance?.OnEnemyFound();
            var wsClient = FindObjectOfType<WebSocketClient>();
            if (wsClient != null)
            {
                wsClient.SendEnemyFound();
            }
            SoundManager.Instance?.PlayDetected();
			Debug.Log($"捕まった: _alertTarget={_alertTarget?.name} isLocalPlayer={_alertTarget?.GetComponent<PlayerController>()?.isLocalPlayer}");
            if (_alertTarget != null)
            {
                var pc = _alertTarget.GetComponent<PlayerController>();
                if (pc != null && pc.isLocalPlayer)
                {
                    pc.Respawn();
                    currentAlertCount = alertCount;
                    _isRespawning = false;
                }
                else
                {
                    if (wsClient != null) wsClient.SendRemoteRespawn();
                    currentAlertCount = alertCount; // 警戒度リセット
                    _isRespawning = false; // ここでリセット
                }
            }
        }
        if (currentAlertCount <= 1)
            Sl.color = Color.red;
        else if (currentAlertCount < 3)
            Sl.color = new Color(1f, 0.5f, 0f);
        else
            Sl.color = new Color(0.827f, 0.851f, 0.439f);
    }

    /// <summary>
    /// 音が聞こえなくなった時の処理
    /// </summary>
    void CancelAlert()
    {
        isAlerted = false;
        isStopMove = false;

        // 最後に聞こえた音の地点に視点を向ける
        enemyState = EnemyState.LookSoundPoint;
    }

    public void ResetRespawnFlag()
    {
		SoundManager.Instance?.PlayLostSight();
		_isRespawning = false;
    }

    #endregion

    #region デバック用ギズモ

    // 範囲デバック用
    private void OnDrawGizmos()
    {
        // 視界・レイキャスト
        if (showViewDebug)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;

            Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);

            Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * viewRadius);

            if (targetPlayer != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, targetPlayer.position);
            }
        }

        // 音検知
        if (showSoundDebug)
        {
            // 音検知範囲
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            // 最後に聞いた音
            if (lastSoundPosition != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastSoundPosition, 0.5f);

                Gizmos.DrawLine(transform.position, lastSoundPosition);
            }

            // 検知した音の有効範囲
            if (debugTimer > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(debugSoundPos, debugSoundRange);

                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(debugSoundPos, 0.3f);

                Gizmos.DrawLine(transform.position, debugSoundPos);
            }
        }
    }

    #endregion

    #region 音検知関数

    /// <summary>
    /// プレイヤーの方向に向かせる処理
    /// </summary>
    void FocusPlayer()
    {
        if (targetPlayer == null) return;
        Vector3 direction = targetPlayer.position - transform.position;

        // 高さ無視
        direction.y = 0;

        // 向きたい回転
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // なめらか回転
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);
    }

    /// <summary>
    ///  プレイヤーの音検知
    /// </summary>
    /// <param name="soundPosition"></param>
    /// <param name="volume"></param>
    void HandleSound(Vector3 soundPosition, float volume)
    {
        // すでにプレイヤーが目視で追従しているなら、遠くの音には気を取られない
        if (enemyState == EnemyState.FocusCore || (enemyState == EnemyState.FocusPlayer && isFoundPlayer)) return; 

        // 音が聞こえる範囲内かどうかを計算
        float distanceToSound = Vector3.Distance(transform.position, soundPosition);

        // 音量と距離から最終的な検知距離を計算
        if (distanceToSound <= hearingRange * volume)
        {
            // 音を検知したら、警戒状態にしてタイマーを最大値にリセット
            isAlerted = true;
            alertTimer = alertDuration;

            isHearingSound = true;           // 音が聞こえている状態にする
            lastSoundPosition = soundPosition; // 音の位置を記憶

            // デバック用
            debugSoundPos = soundPosition;
            debugSoundRange = hearingRange * volume;
            debugTimer = 2f; // 2秒表示

            // 音を出したプレイヤーを特定してalertTargetに設定
            var p1 = GameObject.FindWithTag("Player1");
            var p2 = GameObject.FindWithTag("Player2");
            foreach (var p in new[] { p1, p2 })
            {
                if (p == null) continue;
                if (Vector3.Distance(p.transform.position, soundPosition) < 1f)
                {
                    _alertTarget = p.transform;
                    break;
                }
            }

            if (reactionText != null)
            {
                reactionText.text = "!";
                reactionText.gameObject.SetActive(true); // ビックリマーク表示
                isReaction = true;
            }
            
            enemyState = EnemyState.LookSoundPoint;
        }
    }

    #region 澤田作：サーバ側音検知

    /// <summary>
    /// Player2の音検知（WebSocketClient経由で呼ばれる）
    /// クールダウン付きでHandleSoundを呼ぶ
    /// </summary>
    public void HandleSoundFromRemote(Vector3 position, float volume)
    {
        if (_remoteSoundCooldown > 0) return; // クールダウン中は無視
        HandleSound(position, volume);
        _remoteSoundCooldown = 0.1f;
    }

    #endregion

    /// <summary>
    /// 最後に聞こえた音の方向を見る
    /// </summary>
    void LookSoundPoint()
    {
        Vector3 direction = lastSoundPosition - transform.position;

        direction.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // なめらか回転
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);

        // 向き終わったか判定
        float angle = Quaternion.Angle(transform.rotation, lookRotation);

        if (angle < 3f)
        {
            // 少し待ってから巡回へ戻す
            currentTime += Time.deltaTime;
            reactionText.text = "?";

            if (currentTime >= 1.5f)
            {
                ResetPatrolState();
                currentTime = 0;
                reactionText.gameObject.SetActive(false); // ビックリマーク非表示
                enemyState = EnemyState.Patrol;
            }
        }
    }

    #endregion

    #region コア用関数

    /// <summary>
    /// 範囲内にあるCoreを検索する
    /// </summary>
    void SearchCore()
    {
        // 自分が既にコアを手に保持しているなら何もしない
        if (carriedCore != null) return;

        // タグが "Core" のオブジェクトを取得
        GameObject coreObj = GameObject.FindGameObjectWithTag("Core");
        if (coreObj == null) return;

        Core core = coreObj.GetComponent<Core>();
        if (core == null) return;

        // すでに自分が手元にアタッチ済みの場合は除外
        if (core.transform.IsChildOf(transform))
        {
            carriedCore = core;
            return;
        }

        Vector3 enemyPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 corePos = new Vector3(core.transform.position.x, 0, core.transform.position.z);
        float distance = Vector3.Distance(enemyPos, corePos);

        // すでにこのコアに向かって追従中の場合→範囲外チェック
        if (targetCore != null && enemyState == EnemyState.FocusCore)
        {
            if (distance > coreRadius)
            {
                Debug.Log("Coreが範囲外に出たため巡回に戻ります");
               // core.ReleaseCore(transform);
                targetCore = null;

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                ResetPatrolState();
                enemyState = EnemyState.Patrol;
            }
            return;
        }

        // 他の敵が既に持っている、または他の敵が追従中なら無視
        if (distance <= coreRadius)
        {
            bool isCarriedByOtherEnemy = core.isCarried && core.carrier != null &&
                                               core.carrier != transform &&
                                               core.carrier.GetComponent<EnemyManager>() != null;

            // 敵が自分以外にコアを手に取っていない状態であれば狙う
            if (!isCarriedByOtherEnemy)
            {
                if (core.ClaimCore(transform))
                {
                    targetCore = core;
                    isFoundPlayer = false; // プレイヤー検知フラグ解除
                    enemyState = EnemyState.FocusCore;
                    isStopMove = false;

                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                    }

                    Debug.Log($"Coreを検知！追従を開始します (距離: {distance:F2}m)");
                }
            }
        }
    }

    void FocusCore()
    {
        if (targetCore == null)
        {
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
            enemyState = EnemyState.Patrol;
            return;
        }

        // 他の敵が既にアタッチして運んでいる場合は諦めて巡回に戻る
        if (targetCore.isCarried && targetCore.carrier != null && targetCore.carrier != transform && targetCore.carrier.GetComponent<EnemyManager>() != null)
        {
            targetCore = null;
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
            ResetPatrolState();
            enemyState = EnemyState.Patrol;
            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(targetCore.transform.position);
        }

        // リアルタイム距離チェック
        float currentDist = Vector3.Distance(
              new Vector3(transform.position.x, 0, transform.position.z),
              new Vector3(targetCore.transform.position.x, 0, targetCore.transform.position.z)
          );

        if (currentDist <= 1.2f || (agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= 1.2f))
        {
            Debug.Log("Coreを回収（強奪）しました。巡回に戻ります。");

            // 移動停止
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            // プレイヤーが持っていた場合の強奪処理（安全な null チェック付き）
            if (targetCore.carrier != null)
            {
                PlayerCoreCarrier playerCarrier = targetCore.carrier.GetComponent<PlayerCoreCarrier>();
                if (playerCarrier != null)
                {
                    playerCarrier.ForceStealCore();
                }
            }

            // アタッチ先の設定（coreHoldPoint が未設定の場合は自分自身の transform を使用）
            Transform attachTarget = (coreHoldPoint != null) ? coreHoldPoint : transform;

            // コアのアタッチと保持フラグの設定
            Core coreToAttach = targetCore;
            targetCore = null; // ターゲット参照をクリア

            if (coreToAttach != null)
            {
                coreToAttach.AttachToEnemy(attachTarget);
                carriedCore = coreToAttach;
            }

            // 巡回状態へ復帰
            ResetPatrolState();
            isStopMove = false;
            enemyState = EnemyState.Patrol;
        }
    }

    /// <summary>
    /// 敵が持っているコアを地面にドロップする処理
    /// </summary>
    public void DropCarriedCore()
    {
        if (carriedCore != null)
        {
            carriedCore.ResetCore();

            // 敵の少し前に落とす
            carriedCore.transform.position = transform.position + transform.forward * 0.5f + Vector3.up * 0.2f;
            carriedCore = null;
        }
    }

    #endregion

    #region 澤田作：State(サーバー用)

    public string GetReactionState()
    {
        if (reactionText == null || !reactionText.gameObject.activeSelf) return "";
        return reactionText.text;
    }

    public void SetReactionState(string state)
    {
        if (reactionText == null) return;
        if (string.IsNullOrEmpty(state))
        {
            reactionText.gameObject.SetActive(false);
            isReaction = false;
        }
        else
        {
            reactionText.text = state;
            reactionText.gameObject.SetActive(true);
            isReaction = true;
        }
    }

    public void SetLastSoundPosition(Vector3 pos)
    {
        lastSoundPosition = pos;
    }

    #endregion

    #region アニメーション

    /// <summary>
    /// 強化敵の壁をだす処理
    /// </summary>
    public void PlayAnimationWall()
    {
        animWall.SetTrigger("wallUp");
    }

    /// <summary>
    /// 敵が攻撃されてスタン状態にする処理
    /// </summary>
    public void PlayAnimationEnemy()
    {
        animEnemy.SetTrigger("Stun");

        // 攻撃を受けて倒れたら保持しているコアをドロップ
        DropCarriedCore();

        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    public void TextCancel()
    {      
        // 攻撃されたらリアクションテキストを消す
        if (reactionText != null)
        {
            print("呼ばれた");
            reactionText.gameObject.SetActive(false);
            isReaction = false;
        }
    }

    public void StunCancel()
    {
        animEnemy.SetTrigger("StunCancel");
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    #endregion

    #region ギミック

    public void SwitchCountValue(int value)
    {
        gimmickCount += value;
    }

    #endregion

    #region 初期化処理

    /// <summary>
    /// 巡回状態を初期化
    /// </summary>
    public void ResetPatrolState()
    {
        currentTime = 0f;
        targetPlayer = null;
        _alertTarget = null;
        isStopMove = false;
        isFoundPlayer = false;

        isAlerted = false;
        isHearingSound = false;
        alertTimer = 0f;
        currentAlertCount = alertCount;
        lastSoundPosition = Vector3.zero;

        // コア関係をリセット
        targetCore = null;
        //carriedCore = null;

        enemyState = EnemyState.Patrol;

        // 次のポイントを再設定
        StartPoint();

        // テキスト非表示
        reactionText.gameObject.SetActive(false);
    }

    /// <summary>
    /// テキスト初期化
    /// </summary>
    void InitText()
    {
        Transform child = transform.Find("Model/ActionCanvas/ReactionText");
        if (child != null)
        {
            reactionText = child.GetComponent<TextMeshProUGUI>();
        }

        if (reactionText != null)
        {
            reactionText.gameObject.SetActive(false); // 最初は非表示
        }
    }

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    void InitComponent()
    {
        Sl = GetComponentInChildren<Light>();
        animEnemy = GetComponentInChildren<Animator>();
        animWall = GetComponentInChildren<Animator>();
    }

    void InitializeEnemyState()
    {
        enemyState = EnemyState.Patrol;
        currentAlertCount = alertCount;
        gimmickCount = 0;
    }

    /// <summary>
    /// 最初の移動ポイント決定
    /// </summary>
    void InitPoint()
    {
        // 一番近いポイント（スタート地点）
        StartPoint();

        // 最初の停止時間をランダム化
        //stopMoveCooldown = Random.Range(stopMoveCooldownMin, stopMoveCooldownMax);
    }

    /// <summary>
    /// 澤田作：プレイヤー初期化
    /// </summary>
    void InitPlayer()
    {
        Pc = (GameObject.FindWithTag("Player1") ?? GameObject.FindWithTag("Player2"))?.GetComponent<PlayerController>();
        if (Pc != null)
            Pc.OnMakeSound += HandleSound;

        targetPlayer = (GameObject.FindWithTag("Player1") ?? GameObject.FindWithTag("Player2"))?.transform;
    }

    void InitCamera()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    #endregion

    #region 澤田作：サーバ関連Update処理

    void TextUpdate()
    {
        // テキストが表示されている間、常にカメラの方を向かせる
        if (isReaction && reactionText != null && cameraTransform != null)
        {
            Vector3 dir = reactionText.transform.position - cameraTransform.position;
            dir.y = 0; // Y軸回転だけにする
            reactionText.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    #endregion

    #region ステート管理処理

    void StateMachine()
    {
        // ステートマシーン
        switch (enemyState)
        {
            case EnemyState.Patrol:
                MoveEnemy();                // 敵が移動する処理
                break;
            case EnemyState.FocusPlayer:
                FocusPlayer();              // プレイヤーに視点のほうに視点を向ける処理
                break;
            case EnemyState.LookSoundPoint:
                LookSoundPoint();           // 最後に聞こえた音の地点に視点を向ける処理
                break;
            case EnemyState.FocusCore:
                FocusCore();                // Coreに向かって移動する処理
                break;
        }
    }

    #endregion

    #region Update処理

    void UpdateDebugTimer()
    {
        if (debugTimer > 0)
        {
            debugTimer -= Time.deltaTime;
        }
    }

    #endregion

    #region Unityイベント

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.speed = speed;
        agent.angularSpeed = rotateSpeed;
    }

    // Startをプレイヤー生成待ちのために遅延実行に変更
    void Start()
    {
        Invoke("DelayedStart", 0.5f);
    }

    void DelayedStart()
    {
        InitText();                                 // テキスト初期化
        InitComponent();                            // コンポーネント初期化
        InitializeEnemyState();                     // 敵関連初期化
        InitPlayer();                               // プレイヤー初期化(澤田作)
        InitCamera();                               // カメラ初期化
        InitPoint();                                // 開始ポイント決定
    }

    private void OnDisable()
    {
        DropCarriedCore();
    }

    private void OnDestroy()
    {
        DropCarriedCore();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDebugTimer();                         // 音検知デバック
        TextUpdate();                               // テキストを画面の方向に向ける処理(サーバ連携してないので今後実装予定)

        // 澤田作：サーバ関連Update
        if (isRemoteControlled) return;


        if (_remoteSoundCooldown > 0)
            _remoteSoundCooldown -= Time.deltaTime;


        if (!_soundRegistered)
        {
            var p = GameObject.FindWithTag("Player1") ?? GameObject.FindWithTag("Player2");
            if (p != null)
            {
                Pc = p.GetComponent<PlayerController>();
                if (Pc != null)
                {
                    Pc.OnMakeSound += HandleSound;
                    _soundRegistered = true;
                }
            }
        }

        if (enemyState == EnemyState.FocusCore)
        {
            FocusCore();
            return;
        }
        else if (enemyState == EnemyState.Patrol && carriedCore == null)
        {
            SearchCore(); // コアを持っていない巡回中のみ索敵
        }

        if (isRemoteControlled) return;             // ゲスト側はAIを動かさない（WebSocketClientが位置を受信して動かす）

        PlayerFound();                              // プレイヤー発見処理
        AlertFunction();                            // 警戒状態処理

        StateMachine();                             // 敵のステート管理関数
    }

    #endregion

}