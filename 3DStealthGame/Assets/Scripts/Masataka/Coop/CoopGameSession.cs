using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-50)]
public sealed class CoopGameSession : MonoBehaviour
{
    public static CoopGameSession Instance { get; private set; }
    [Header("ルール調整")]
    [Min(0.1f)] public float snapshotInterval = 0.1f;
    [Min(0f)] public float staggerSeconds = 3f;
    [Min(0f)] public float staggerRecoverySeconds = 1f;
    public bool Cleared { get; private set; }
    public bool Ready { get; private set; }
    public bool IsHost => network != null && network.IsHostPlayer();
    public CoopChain Chain { get; private set; }
    private WebSocketClient network;
    private readonly Dictionary<string, CoopCore> cores = new Dictionary<string, CoopCore>();
    private readonly Dictionary<string, ElementalGimmick> gimmicks = new Dictionary<string, ElementalGimmick>();
    private readonly Dictionary<string, CoopPlayerStateController> players = new Dictionary<string, CoopPlayerStateController>();
    private readonly Dictionary<string, float> fireTimes = new Dictionary<string, float>();
    private CoopReturnZone[] zones;
    private float nextSnapshot, nextPlayerScan, lastSnapshot, startedAt;
    private int sequence, appliedSequence = -1;
    private string sessionId;
    private bool announcedClear;
    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene") return;
        WebSocketClient client = FindObjectOfType<WebSocketClient>();
        if (client != null && client.enableCoreCoop && Instance == null)
            new GameObject("CoopGameSession").AddComponent<CoopGameSession>();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        Instance = null;
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this; startedAt = Time.time;
        network = FindObjectOfType<WebSocketClient>();
        Chain = GetComponent<CoopChain>();
        if (Chain == null) Chain = gameObject.AddComponent<CoopChain>();
    }
    private void Start()
    {
        // Scene generators run in Awake; enumerate only after all of them finished.
        foreach (GoalScript oldGoal in FindObjectsOfType<GoalScript>()) oldGoal.enabled = false;
        // Enemy movement/attacks remain entirely with the original EnemyManager.
        foreach (CoopCore core in FindObjectsOfType<CoopCore>())
        {
            string key = Key("C", core.transform.position);
            if (cores.ContainsKey(key)) { Debug.LogError("[Coop] コアの初期位置が重複: " + key); continue; }
            core.Initialize(key); cores.Add(key, core);
        }
        foreach (ElementalGimmick gimmick in FindObjectsOfType<ElementalGimmick>())
        {
            gimmick.Initialize();
            if (gimmicks.ContainsKey(gimmick.Key)) { Debug.LogError("[Coop] ギミックIDが重複: " + gimmick.Key); continue; }
            gimmicks.Add(gimmick.Key, gimmick);
        }
        zones = FindObjectsOfType<CoopReturnZone>();
        if (cores.Count == 0) Debug.LogError("コアがない");
    }
    public static string Key(string prefix, Vector3 position)
    { return prefix + ":" + Mathf.RoundToInt(position.x * 100f) + ":" + Mathf.RoundToInt(position.y * 100f) + ":" + Mathf.RoundToInt(position.z * 100f); }
    public GameObject Player(string id) => network != null ? network.CoopPlayer(id) : null;
    public Transform PlayerByNumber(int number)
    {
        foreach (var p in players.Values) if (p != null && p.CompareTag("Player" + number)) return p.transform;
        return null;
    }
    public Transform OtherPlayer(Transform player)
    {
        foreach (var p in players.Values) if (p != null && p.transform != player) return p.transform;
        return null;
    }
    public CoopCore PlayerCore(string id) => cores.Values.FirstOrDefault(c => c.Mode == 1 && c.Owner == id);
    private void Update()
    {
        if (network == null || !network.CoopConnected) { Ready = false; return; }
        if (string.IsNullOrEmpty(sessionId)) sessionId = network.CoopSessionId;
        if (Time.time >= nextPlayerScan)
        {
            nextPlayerScan = Time.time + 0.1f;
            var active = new HashSet<string>();
            foreach (var entry in network.CoopPlayers)
            {
                if (entry.Value == null) continue;
                active.Add(entry.Key);
                if (players.ContainsKey(entry.Key)) continue;
                var state = entry.Value.GetComponent<CoopPlayerStateController>();
                if (state == null) state = entry.Value.AddComponent<CoopPlayerStateController>();
                state.Initialize(entry.Key); players[entry.Key] = state;
            }
            foreach (string id in players.Keys.Where(id => !active.Contains(id)).ToArray())
            {
                if (IsHost) { CoopCore core = PlayerCore(id); if (core != null) core.Drop(core.transform.position); }
                players.Remove(id);
            }
            if (zones.Length == 0 && network.TryGetCoopSpawn(1, out Vector3 start))
            {
                GameObject area = new GameObject("CoopReturnArea_Auto"); area.transform.position = Ground(start);
                var box = area.AddComponent<BoxCollider>(); box.size = new Vector3(4f, 5f, 4f); box.center = Vector3.up * 1.5f;
                zones = new[] { area.AddComponent<CoopReturnZone>() };
            }
        }
        if (IsHost) Ready = !string.IsNullOrEmpty(sessionId);
        else if (Time.time - lastSnapshot > 2f) Ready = false;
        if (!IsHost || !Ready) return;
        foreach (CoopCore core in cores.Values)
        {
            if (core.Mode == 1 && players.TryGetValue(core.Owner, out var owner) && owner != null)
            {
                if (!owner.gameObject.activeInHierarchy || owner.GetComponent<PlayerController>().IsFading) core.Drop(owner.transform.position);
                else if (!owner.IsStunned && zones.Any(z => z != null && z.Contains(owner.transform.position))) core.Set(3, "", "", core.transform.position);
            }
            else if (core.Mode == 0 && core.transform.position.y < -10f) core.Set(0, "", "", core.Home);
        }
        bool done = cores.Count > 0 && cores.Values.All(c => c.Mode == 3);
        if (done && !Cleared) { Cleared = true; AnnounceClear(); }
        if (Time.time >= nextSnapshot)
        {
            nextSnapshot = Time.time + snapshotInterval;
            network.SendCoop(new CoopState {
                session_id = sessionId, sequence = ++sequence, cleared = Cleared,
                cores = cores.Values.Select(c => c.Capture()).ToArray(),
                players = players.Values.Where(p => p != null).Select(p => new CoopPlayerState { id = p.PlayerId, stun = p.StunRemaining, immune = p.ImmuneRemaining }).ToArray(),
                gimmicks = gimmicks.Values.Select(g => new CoopGimmickState { key = g.Key, activated = g.Activated }).ToArray()
            });
        }
    }
    // EnemyManager's existing capture callbacks are translated here; no enemy AI changes.
    public void StaggerPlayer(GameObject target)
    {
        if (!IsHost || !Ready || Cleared || target == null) return;
        CoopPlayerStateController state = target.GetComponent<CoopPlayerStateController>();
        if (state == null || state.IsImmune) return;
        state.Hit(staggerSeconds, staggerRecoverySeconds);
        nextSnapshot = 0f;
    }

    public void StaggerGuest()
    {
        Transform guest = PlayerByNumber(2);
        if (guest != null) StaggerPlayer(guest.gameObject);
    }

    public void Interact(string id)
    {
        if (!Ready || Cleared) return;
        CoopCore held = PlayerCore(id);
        var request = new CoopRequest { session_id = sessionId, sender_id = id, op = held != null ? "release" : "pickup" };
        if (held != null) request.target = held.Key;
        else
        {
            GameObject player = Player(id); if (player == null) return;
            CoopCore core = cores.Values.Where(c => c.CanPick).OrderBy(c => (c.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();
            if (core == null) return; request.target = core.Key;
        }
        if (IsHost) Handle(request); else network.SendCoop(request);
    }
    public void RequestShot(Vector3 direction)
    {
        var request = new CoopRequest { session_id = sessionId, sender_id = network.myId, op = "shoot", direction = direction };
        if (IsHost) Handle(request); else network.SendCoop(request);
    }
    public void Receive(string type, string json)
    {
        if (type == "coop_host_lost") { Ready = false; lastSnapshot = -100f; return; }
        if (type == "coop_request")
        { if (IsHost) Handle(JsonUtility.FromJson<CoopRequest>(json)); return; }
        if (type == "coop_shot")
        {
            if (IsHost) return;
            var shot = JsonUtility.FromJson<CoopShot>(json);
            if (shot.session_id != network.CoopSessionId || !Ready) return;
            CoopProjectile.Spawn(shot, Player(shot.owner), false); return;
        }
        if (type != "coop_state" || IsHost) return;
        CoopState state = JsonUtility.FromJson<CoopState>(json);
        if (state.session_id != network.CoopSessionId || state.sequence <= appliedSequence) return;
        appliedSequence = state.sequence; lastSnapshot = Time.time; Ready = true;
        // Enemy state is handled by the existing WebSocket enemy synchronization.
        if (state.cores != null) foreach (var c in state.cores) if (cores.TryGetValue(c.key, out var core)) core.Set(c.mode, c.owner, c.enemy, c.position);
        if (state.players != null) foreach (var p in state.players) if (players.TryGetValue(p.id, out var actor)) actor.ApplyTimers(p.stun, p.immune);
        if (state.gimmicks != null) foreach (var g in state.gimmicks) if (gimmicks.TryGetValue(g.key, out var gimmick)) gimmick.Apply(g.activated);
        if (state.cleared && !Cleared) { Cleared = true; AnnounceClear(); }
    }
    private void Handle(CoopRequest request)
    {
        if (!IsHost || !Ready || Cleared || request.session_id != sessionId || !players.TryGetValue(request.sender_id ?? "", out var player) || player == null) return;
        PlayerController controller = player.GetComponent<PlayerController>();
        if (player.IsStunned || controller.IsFading || controller.isAction || controller.isPlayerMoveStop) return;
        if (request.op == "shoot")
        {
            if (player.AttackBlocked || (fireTimes.TryGetValue(player.PlayerId, out float next) && Time.time < next)) return;
            if (!Finite(request.direction) || request.direction.sqrMagnitude < 0.1f) return;
            var combat = player.GetComponent<PlayerCombatController>(); if (combat == null) return;
            fireTimes[player.PlayerId] = Time.time + Mathf.Max(0.12f, combat.CoopFireInterval);
            CoopShot shot = combat.MakeCoopShot(player.PlayerId, player.Element, request.direction.normalized, sessionId);
            CoopProjectile.Spawn(shot, player.gameObject, true); network.SendCoop(shot); return;
        }
        if (!cores.TryGetValue(request.target ?? "", out var core)) return;
        if (request.op == "pickup")
        {
            if (!core.CanPick || PlayerCore(player.PlayerId) != null || PlanarDistance(player.transform.position, core.transform.position) > core.pickupDistance ||
                !Visible(player.transform.position + Vector3.up, core.transform.position + Vector3.up * 0.2f, player.transform)) return;
            core.Set(1, player.PlayerId, "", core.transform.position); return;
        }
        if (request.op != "release" || core.Mode != 1 || core.Owner != player.PlayerId) return;
        var other = players.Values.FirstOrDefault(p => p != null && p != player && !p.IsStunned && !p.IsCarrying && p.gameObject.activeInHierarchy &&
            PlanarDistance(player.transform.position, p.transform.position) <= 1.8f && Visible(player.transform.position + Vector3.up, p.transform.position + Vector3.up, player.transform));
        if (other != null) core.Set(1, other.PlayerId, "", core.transform.position);
        else core.Drop(player.transform.position);
    }
    private static float PlanarDistance(Vector3 a, Vector3 b)
    { a.y = b.y = 0f; return Vector3.Distance(a, b); }
    private static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    public bool Visible(Vector3 from, Vector3 to, Transform ignore)
    {
        Vector3 delta = to - from;
        foreach (RaycastHit hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == ignore || hit.transform.IsChildOf(ignore) || hit.collider.GetComponentInParent<PlayerController>() != null ||
                hit.collider.GetComponentInParent<CoopCore>() != null || hit.collider.GetComponentInParent<EnemyManager>() != null) continue;
            return false;
        }
        return true;
    }
    public static Vector3 Ground(Vector3 at)
    {
        var hits = Physics.RaycastAll(at + Vector3.up * 3f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
            if (hit.normal.y > 0.5f && hit.collider.GetComponentInParent<PlayerController>() == null && hit.collider.GetComponentInParent<EnemyManager>() == null && hit.collider.GetComponentInParent<CoopCore>() == null)
                return hit.point;
        return at;
    }
    private void AnnounceClear()
    {
        if (announcedClear) return; announcedClear = true;
        foreach (CoopReturnZone zone in zones) if (zone != null) zone.onCleared?.Invoke();
        MissionManager.Instance?.OnGoal();
        MissionManager.Instance?.StopTimer();
        MissionManager.Instance?.ShowClearMessage();
        if (IsHost) network.SendCoop(new CoopFinish { session_id = sessionId, elapsed = Time.time - startedAt });
    }
    [Serializable] private sealed class CoopFinish { public string type = "coop_finish", session_id; public float elapsed; }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
