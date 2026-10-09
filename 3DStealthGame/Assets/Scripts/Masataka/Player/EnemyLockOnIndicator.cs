using UnityEngine;
using UnityEngine.UI;

// Screen-space brackets stay readable at any camera distance. No external assets required.
public sealed class EnemyLockOnIndicator : MonoBehaviour
{
	private GameObject canvasObject;
	private readonly RectTransform[] corners = new RectTransform[8];
	private RectTransform arrow;
	private readonly Vector3[] worldCorners = new Vector3[8];
	private readonly Color targetColor = new Color(1f, 0.12f, 0.12f);

	private void Awake()
	{
		canvasObject = new GameObject("EnemyLockOnUI", typeof(RectTransform), typeof(Canvas));
		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 200;
		for (int i = 0; i < corners.Length; i++) corners[i] = CreateImage("Corner", targetColor, true);
		arrow = CreateImage("TargetDiamond", targetColor, true);
		arrow.localRotation = Quaternion.Euler(0f, 0f, 45f);
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
		image.raycastTarget = false;
		if (outlined)
		{
			Outline outline = obj.AddComponent<Outline>();
			outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
			outline.effectDistance = new Vector2(1.5f, -1.5f);
		}
		return rect;
	}

	public void Show(Transform target)
	{
		Camera camera = Camera.main;
		if (camera == null || !AutoAttackTarget.IsAlive(target) || !AutoAttackTarget.TryBounds(target, out Bounds bounds))
		{ Hide(); return; }
		Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
		for (int i = 0; i < worldCorners.Length; i++)
		{
			worldCorners[i] = bounds.center + Vector3.Scale(bounds.extents,
				new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
			Vector3 screen = camera.WorldToScreenPoint(worldCorners[i]);
			if (screen.z <= 0f) { Hide(); return; }
			min = Vector2.Min(min, new Vector2(screen.x, screen.y));
			max = Vector2.Max(max, new Vector2(screen.x, screen.y));
		}
		if (max.x < 0f || max.y < 0f || min.x > Screen.width || min.y > Screen.height)
		{ Hide(); return; }
		canvasObject.SetActive(true);
		Vector2 center = (min + max) * 0.5f;
		Vector2 size = max - min + Vector2.one * 14f;
		size.x = Mathf.Max(52f, size.x);
		size.y = Mathf.Max(68f, size.y);
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
		Place(arrow, new Vector2(center.x, max.y + 15f), Vector2.one * 11f);

	}

	private static void Place(RectTransform rect, Vector2 position, Vector2 size)
	{ rect.anchoredPosition = position; rect.sizeDelta = size; }
	public void Hide() { if (canvasObject != null) canvasObject.SetActive(false); }
	private void OnDisable() { Hide(); }
	private void OnDestroy() { if (canvasObject != null) Destroy(canvasObject); }
}
