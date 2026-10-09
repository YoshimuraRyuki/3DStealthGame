using UnityEngine;

/// <summary>短時間だけ拡大・消滅する発射光。</summary>
public sealed class RobotMuzzleFlash : MonoBehaviour
{
    private float elapsed;
    private const float Duration = 0.09f;

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Duration);
        transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 0.42f, progress);

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = renderer.material;
            Color color = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.color;
            color.a = 1f - progress;

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        if (progress >= 1f)
            Destroy(gameObject);
    }
}
