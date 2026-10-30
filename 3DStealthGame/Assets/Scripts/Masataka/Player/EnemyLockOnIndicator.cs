using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

// Screen-space brackets stay readable at any camera distance. No external assets required.
public sealed class EnemyLockOnIndicator : MonoBehaviour
{
	private GameObject canvasObject;
	private readonly RectTransform[] corners = new RectTransform[8];
	[SerializeField] private Color canAttackColor = new Color(0.15f, 1f, 0.25f);
    [SerializeField] private Color cannotAttackColor = new Color(1f, 0.12f, 0.12f);
    [SerializeField, Min(0.01f)] private float deniedShakeDuration = 0.2f;
    [SerializeField, Min(0f)] private float deniedShakePixels = 8f;
    private readonly List<Image> images = new List<Image>();
    private Transform shakeTarget;
    private float shakeStartedAt = -100f;

	private void Awake()
	{
		canvasObject = new GameObject("EnemyLockOnUI", typeof(RectTransform), typeof(Canvas));
		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 200;
		for (int i = 0; i < corners.Length; i++) corners[i] = CreateImage("Corner", canAttackColor, true);
		canvasObject.SetActive(false);
	}

	private RectTransform CreateImage(string name, Color color, bool outlined)
	{
		GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
		RectTransform rect = obj.GetComponent<RectTransform>();
		rect.SetParent(canvasObject.transform, false);
		rect.anchorMin = rect.anchorMax = Vector2.zero;
		rect.pivot = Vector2.one * 0.5f;
		Image image = obj.GetComponent<Image>();
		image.color = color;
        images.Add(image);
		image.raycastTarget = false;
		if (outlined)
		{
			Outline outline = obj.AddComponent<Outline>();
			outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
			outline.effectDistance = new Vector2(1.5f, -1.5f);
		}
		return rect;
	}

	public void Show(Transform target) { Show(target, true); }

    public void ShakeDenied(Transform target)
    {
        shakeTarget = target;
        shakeStartedAt = Time.unscaledTime;
    }

    internal static Vector2 DeniedOffset(float elapsed, float duration, float pixels)
    {
        if (elapsed < 0f || duration <= 0f || elapsed >= duration) return Vector2.zero;
        float strength = pixels * (1f - elapsed / duration);
        return new Vector2(Mathf.Sin(elapsed * 150f) * strength, Mathf.Sin(elapsed * 110f) * strength * 0.35f);
    }

    public void Show(Transform target, bool canAttack)
	{
        if (!AutoAttackTarget.TryLockScreenBounds(target, Camera.main, out Vector2 min, out Vector2 max))
        { Hide(); return; }
		canvasObject.SetActive(true);
        Color color = canAttack ? canAttackColor : cannotAttackColor;
        foreach (Image image in images) image.color = color;
		Vector2 center = (min + max) * 0.5f;
        if (target == shakeTarget)
            center += DeniedOffset(Time.unscaledTime - shakeStartedAt, deniedShakeDuration, deniedShakePixels);
		Vector2 size = max - min;
		min = center - size * 0.5f;
		max = center + size * 0.5f;
		float length = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.25f, 14f, 26f);
		for (int i = 0; i < 4; i++)
		{
			bool left = (i & 1) == 0, bottom = (i & 2) == 0;
			Vector2 corner = new Vector2(left ? min.x : max.x, bottom ? min.y : max.y);
			Place(corners[i * 2], corner + new Vector2(left ? length * 0.5f : -length * 0.5f, 0f), new Vector2(length, 4f));
			Place(corners[i * 2 + 1], corner + new Vector2(0f, bottom ? length * 0.5f : -length * 0.5f), new Vector2(4f, length));
		}

	}

	private static void Place(RectTransform rect, Vector2 position, Vector2 size)
	{ rect.anchoredPosition = position; rect.sizeDelta = size; }
	public void Hide()
    {
        if (canvasObject != null) canvasObject.SetActive(false);
        shakeTarget = null;
        shakeStartedAt = -100f;
    }
	private void OnDisable() { Hide(); }
	private void OnDestroy() { if (canvasObject != null) Destroy(canvasObject); }
}
