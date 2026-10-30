using UnityEngine;
using UnityEngine.UI;

/// <summary>EnemyHealthを読むだけの頭上HP表示。敵AI・HP判定・通信には介入しない。</summary>
[DisallowMultipleComponent]
public sealed class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector2 barSize = new Vector2(110f, 14f);
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 10f);
    [SerializeField, Min(1f)] private float maxDistance = 45f;
    [SerializeField] private bool hideBehindWalls = true;
    [SerializeField, Min(0f)] private float damageTrailDelay = 0.2f;
    [SerializeField, Min(0.01f)] private float damageTrailSpeed = 1.5f;

    private static Canvas sharedCanvas;
    private static Sprite roundedSprite;
    private EnemyHealth health;
    private Transform enemyRoot;
    private RectTransform panel, fillRect, trailRect, shineRect;
    private Image frame, fill;
    private float lastRatio = -1f, trailRatio, trailAt, hitAt = -10f;
    private float nextVisibilityCheck;
    private bool wallVisible = true;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        EnemyManager enemy = GetComponentInParent<EnemyManager>();
        enemyRoot = enemy != null ? enemy.transform : transform;
        if (health == null) { enabled = false; return; }
        CreateView();
    }

    private static void EnsureCanvas()
    {
        if (sharedCanvas != null) return;
        GameObject canvas = new GameObject("EnemyHealthBars", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        sharedCanvas = canvas.GetComponent<Canvas>();
        sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        sharedCanvas.sortingOrder = 190;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        // No GraphicRaycaster: the bars never consume mouse/pad UI input.
    }

    private static Sprite RoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 32;
        const float radius = 9f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "EnemyHP_Rounded";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius));
            float dy = Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius));
            float outside = Mathf.Sqrt(Mathf.Max(0f, dx) * Mathf.Max(0f, dx) + Mathf.Max(0f, dy) * Mathf.Max(0f, dy));
            pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius + 0.5f - outside));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedSprite;
    }

    private Image ImageLayer(string name, RectTransform parent, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        var image = obj.GetComponent<Image>();
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        image.color = color;
        return image;
    }

    private void CreateView()
    {
        EnsureCanvas();
        GameObject root = new GameObject("EnemyHP", typeof(RectTransform));
        panel = root.GetComponent<RectTransform>();
        panel.SetParent(sharedCanvas.transform, false);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = barSize;
        var shadow = ImageLayer("Shadow", panel, barSize + new Vector2(4f, 4f), new Color(0f, 0f, 0f, 0.45f));
        shadow.rectTransform.anchoredPosition = new Vector2(0f, -1.5f);
        frame = ImageLayer("Frame", panel, barSize, new Color(0.3f, 0.4f, 0.43f, 0.95f));
        Vector2 inside = new Vector2(Mathf.Max(1f, barSize.x - 3f), Mathf.Max(1f, barSize.y - 3f));
        ImageLayer("Track", panel, inside, new Color(0.025f, 0.045f, 0.055f, 1f));
        trailRect = ImageLayer("DamageTrail", panel, inside, new Color(1f, 0.37f, 0.3f, 0.9f)).rectTransform;
        fill = ImageLayer("Health", panel, inside, HealthColor(1f));
        fillRect = fill.rectTransform;
        shineRect = ImageLayer("Highlight", panel, new Vector2(inside.x - 4f, 1.5f), new Color(1f, 1f, 1f, 0.28f)).rectTransform;
        foreach (RectTransform rect in new[] { trailRect, fillRect, shineRect })
        {
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(-inside.x * 0.5f, 0f);
        }
        shineRect.anchoredPosition += new Vector2(2f, inside.y * 0.25f);
        panel.gameObject.SetActive(false);
    }

    internal static Color HealthColor(float ratio)
    {
        if (ratio <= 0.25f) return new Color(1f, 0.27f, 0.28f);
        if (ratio <= 0.5f) return new Color(1f, 0.72f, 0.2f);
        return new Color(0.15f, 0.9f, 0.68f);
    }

    private void LateUpdate()
    {
        if (health == null || enemyRoot == null || !enemyRoot.gameObject.activeInHierarchy || health.CurrentHealth <= 0)
        { Hide(); return; }
        Camera camera = Camera.main;
        if (camera == null || Vector3.Distance(camera.transform.position, enemyRoot.position) > maxDistance)
        { Hide(); return; }
        if (!AutoAttackTarget.TryLockScreenBounds(enemyRoot, camera, out Vector2 frameMin, out Vector2 frameMax))
        { Hide(); return; }
        Vector3 anchor = enemyRoot.position + Vector3.up * 2f;
        if (AutoAttackTarget.TryBounds(enemyRoot, out Bounds bounds))
            anchor = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        if (hideBehindWalls && Time.unscaledTime >= nextVisibilityCheck)
        {
            nextVisibilityCheck = Time.unscaledTime + 0.12f;
            wallVisible = IsVisible(camera.transform.position, anchor);
        }
        if (hideBehindWalls && !wallVisible) { Hide(); return; }
        if (panel == null || sharedCanvas == null) CreateView();
        float scale = Mathf.Max(0.01f, sharedCanvas.scaleFactor);
        // Include the shadow, bracket stroke and denial shake in the clearance.
        float halfHeight = (barSize.y + 4f) * scale * 0.5f;
        float halfWidth = (barSize.x + 4f) * scale * 0.5f;
        float gap = Mathf.Max(10f, screenOffset.y * scale);
        Vector2 screen = new Vector2((frameMin.x + frameMax.x) * 0.5f + screenOffset.x * scale,
            frameMax.y + gap + halfHeight);
        if (screen.y + halfHeight > Screen.height)
            screen.y = frameMin.y - gap - halfHeight;
        if (screen.y - halfHeight < 0f || screen.y + halfHeight > Screen.height || halfWidth * 2f > Screen.width)
        { Hide(); return; }
        screen.x = Mathf.Clamp(screen.x, halfWidth, Screen.width - halfWidth);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(sharedCanvas.GetComponent<RectTransform>(),
            screen, null, out Vector2 position)) { Hide(); return; }
        panel.anchoredPosition = position;
        float ratio = Mathf.Clamp01((float)health.CurrentHealth / Mathf.Max(1, health.MaxHealth));
        UpdateFill(ratio);
        panel.gameObject.SetActive(true);
    }

    private void UpdateFill(float ratio)
    {
        if (lastRatio < 0f || ratio > trailRatio) trailRatio = ratio;
        else if (ratio < lastRatio)
        {
            trailAt = Time.unscaledTime + damageTrailDelay;
            hitAt = Time.unscaledTime;
        }
        if (Time.unscaledTime >= trailAt)
            trailRatio = Mathf.MoveTowards(trailRatio, ratio, Time.unscaledDeltaTime * damageTrailSpeed);
        lastRatio = ratio;
        float width = Mathf.Max(1f, barSize.x - 3f);
        fillRect.sizeDelta = new Vector2(width * ratio, fillRect.sizeDelta.y);
        trailRect.sizeDelta = new Vector2(width * trailRatio, trailRect.sizeDelta.y);
        shineRect.sizeDelta = new Vector2(Mathf.Max(0f, width * ratio - 4f), shineRect.sizeDelta.y);
        fill.color = HealthColor(ratio);
        float flash = Mathf.Clamp01(1f - (Time.unscaledTime - hitAt) / 0.18f);
        frame.color = Color.Lerp(new Color(0.3f, 0.4f, 0.43f, 0.95f), new Color(0.9f, 1f, 1f, 1f), flash);
    }

    private bool IsVisible(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        foreach (RaycastHit hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == null || hit.transform == enemyRoot || hit.transform.IsChildOf(enemyRoot)) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null || hit.collider.GetComponentInParent<EnemyManager>() != null) continue;
            return false;
        }
        return true;
    }

    private void Hide() { if (panel != null) panel.gameObject.SetActive(false); }
    private void OnDisable() { Hide(); }
    private void OnDestroy() { if (panel != null) Destroy(panel.gameObject); }
}
