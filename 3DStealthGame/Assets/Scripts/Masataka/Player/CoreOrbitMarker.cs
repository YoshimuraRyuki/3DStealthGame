using UnityEngine;

/// <summary>運搬中のコア外装を緩やかに回転させる。</summary>
public sealed class CoreOrbitMarker : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(new Vector3(23f, 41f, 17f) * Time.deltaTime, Space.Self);
    }
}
