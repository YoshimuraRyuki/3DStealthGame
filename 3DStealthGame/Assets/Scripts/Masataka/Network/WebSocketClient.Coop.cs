using UnityEngine;
using System.Collections.Generic;

public partial class WebSocketClient
{
    [Header("コア協力ルール（両クライアントでON）")]
    public bool enableCoreCoop = true;
    public string CoopSessionId => currentSessionId;
    public bool CoopConnected => IsSocketOpen && !string.IsNullOrEmpty(myId);
    public IEnumerable<KeyValuePair<string, GameObject>> CoopPlayers => playerObjects;
    public GameObject CoopPlayer(string id) => id != null && playerObjects.TryGetValue(id, out var obj) ? obj : null;
    public bool TryGetCoopSpawn(int number, out Vector3 position) => spawnPositions.TryGetValue(number, out position);
    public async void SendCoop(object packet)
    {
        try { await SendTextIfConnectedAsync(JsonUtility.ToJson(packet)); }
        catch (System.Exception e) { Debug.LogWarning("[Coop] 送信失敗: " + e.Message); }
    }
    private void ReceiveCoop(string type, string json)
    {
        if (!enableCoreCoop || CoopGameSession.Instance == null) return;
        CoopGameSession.Instance.Receive(type, json);
    }
}
