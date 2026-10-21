using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>元の敵AIを動かしたまま、位置・HP・生死をホストから同期する。</summary>
public partial class WebSocketClient
{
    private sealed class EnemySyncEntry
    {
        public string Key;
        public int Index;
        public GameObject Object;
        public EnemyHealth Health;
        public Vector3 LastPosition;
        public float LastAngle;
        public int MaxHealth;
    }

    private readonly Dictionary<string, EnemySyncEntry> enemySyncByKey = new Dictionary<string, EnemySyncEntry>();
    private readonly List<EnemySyncEntry> enemySyncEntries = new List<EnemySyncEntry>();

    private void ResetEnemySync()
    {
        _enemyObjects = null;
        enemySyncByKey.Clear();
        enemySyncEntries.Clear();
        enemyTargetPositions.Clear();
        enemyTargetAngles.Clear();
        enemySendTimer = 0f;
    }

    private void UpdateEnemySync()
    {
        if (!isGameSceneLoaded) return;
        EnsureEnemyObjectsLoaded();
        if (IsHostPlayer()) SendEnemyStatesAtInterval();
        else if (IsGuestPlayer()) InterpolateEnemyStates();
    }

    private void EnsureEnemyObjectsLoaded()
    {
        if (!isGameSceneLoaded || enemySyncEntries.Count > 0) return;
        // Scene generators finish in Awake. Capture the spawn positions before AI movement.
        var candidates = new Dictionary<string, EnemyManager>();
        var duplicates = new HashSet<string>();
        foreach (EnemyManager enemy in FindObjectsOfType<EnemyManager>(true))
        {
            if (enemy.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) continue;
            Vector3 spawn = enemy.transform.position;
            string key = enemy.enemyID.ToString(CultureInfo.InvariantCulture) + ":" +
                Mathf.RoundToInt(spawn.x * 100f).ToString(CultureInfo.InvariantCulture) + ":" +
                Mathf.RoundToInt(spawn.y * 100f).ToString(CultureInfo.InvariantCulture) + ":" +
                Mathf.RoundToInt(spawn.z * 100f).ToString(CultureInfo.InvariantCulture);
            if (candidates.ContainsKey(key)) duplicates.Add(key);
            else candidates.Add(key, enemy);
        }
        var keys = new List<string>(candidates.Keys);
        keys.Sort(StringComparer.Ordinal);
        foreach (string key in keys)
        {
            if (duplicates.Contains(key))
            {
                Debug.LogError("[EnemySync] 敵IDと初期位置が重複しています: " + key);
                continue;
            }
            GameObject obj = candidates[key].gameObject;
            EnemyHealth health = obj.GetComponent<EnemyHealth>();
            if (health == null) health = obj.AddComponent<EnemyHealth>();
            var entry = new EnemySyncEntry {
                Key = key, Index = enemySyncEntries.Count, Object = obj, Health = health,
                LastPosition = obj.transform.position, LastAngle = obj.transform.eulerAngles.y,
                MaxHealth = health.MaxHealth
            };
            enemySyncEntries.Add(entry);
            enemySyncByKey.Add(key, entry);
        }
        _enemyObjects = new GameObject[enemySyncEntries.Count];
        foreach (EnemySyncEntry entry in enemySyncEntries) _enemyObjects[entry.Index] = entry.Object;
    }

    private void SendEnemyStatesAtInterval()
    {
        if (string.IsNullOrEmpty(currentSessionId)) return;
        enemySendTimer += Time.deltaTime;
        if (enemySendTimer < enemySendInterval) return;
        enemySendTimer = 0f;
        // Keep defeated enemies in this registry and keep sending their death state.
        foreach (EnemySyncEntry entry in enemySyncEntries) SendEnemyState(entry);
    }

    private void InterpolateEnemyStates()
    {
        foreach (var target in enemyTargetPositions)
        {
            int index = target.Key;
            if (_enemyObjects == null || index < 0 || index >= _enemyObjects.Length) continue;
            GameObject obj = _enemyObjects[index];
            if (obj == null || !obj.activeInHierarchy) continue;
            obj.transform.position = Vector3.Lerp(obj.transform.position, target.Value, Time.deltaTime * 25f);
            if (enemyTargetAngles.TryGetValue(index, out float angle))
                obj.transform.rotation = Quaternion.Lerp(obj.transform.rotation, Quaternion.Euler(0f, angle, 0f), Time.deltaTime * 25f);
        }
    }

    private async void SendEnemyState(EnemySyncEntry entry)
    {
        if (!IsSocketOpen || !IsHostPlayer()) return;
        GameObject enemy = entry.Object;
        Color color = Color.white;
        string reaction = "";
        Vector3 lastSound = Vector3.zero;
        int health = 0;
        bool active = false;
        if (enemy != null)
        {
            entry.LastPosition = enemy.transform.position;
            entry.LastAngle = enemy.transform.eulerAngles.y;
            if (entry.Health != null)
            {
                health = entry.Health.CurrentHealth;
                entry.MaxHealth = entry.Health.MaxHealth;
            }
            active = enemy.activeSelf && health > 0;
            var light = enemy.GetComponentInChildren<Light>();
            if (light != null) color = light.color;
            var manager = enemy.GetComponent<EnemyManager>();
            if (active && manager != null)
            {
                reaction = manager.GetReactionState();
                lastSound = manager.GetLastSoundPosition();
            }
        }
        await SendTextIfConnectedAsync(SendMessageBuilder.EnemyMove(
            entry.Index, entry.LastPosition, entry.LastAngle, color, reaction, lastSound,
            entry.Key, currentSessionId, health, entry.MaxHealth, active));
    }
}
