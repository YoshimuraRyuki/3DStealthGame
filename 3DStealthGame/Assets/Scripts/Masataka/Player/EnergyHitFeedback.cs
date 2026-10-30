using UnityEngine;
using UnityEngine.Rendering;

/// <summary>命中地点だけの短い火花とSE。ダメージ・移動・UIには介入しない。</summary>
public sealed class EnergyHitFeedback : MonoBehaviour
{
    private const float HitVolume = 0.32f;
    private static AudioClip hitClip;
    private LineRenderer ring;
    private LineRenderer[] sparks;
    private Transform core;
    private Material colorMaterial, coreMaterial;
    private Color color;
    private float age, duration, radius;
    private int kind;

    public static void Play(Vector3 point, Vector3 normal, AttackElement element, int kind)
    {
        var obj = new GameObject("EnergyHitFeedback");
        if (normal.sqrMagnitude < 0.001f) normal = Vector3.up;
        obj.transform.position = point + normal.normalized * 0.035f;
        var effect = obj.AddComponent<EnergyHitFeedback>();
        effect.Initialize(element, kind);
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        var material = new Material(shader);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 2f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.One);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        Tint(material, color);
        return material;
    }

    private static void Tint(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    private LineRenderer MakeLine(string name, int points)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = points;
        line.sharedMaterial = colorMaterial;
        line.numCapVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    private void Initialize(AttackElement element, int hitKind)
    {
        kind = hitKind;
        duration = kind == 1 ? 0.2f : 0.14f;
        radius = kind == 1 ? 0.36f : 0.16f;
        color = kind == 1 ? (element == AttackElement.Fire ? new Color(1f, 0.55f, 0.18f) : new Color(0.2f, 0.85f, 1f)) :
            (kind == 2 ? new Color(0.65f, 0.4f, 0.42f) : new Color(0.5f, 0.62f, 0.7f));
        colorMaterial = MakeMaterial(color);
        coreMaterial = MakeMaterial(new Color(1f, 0.96f, 0.85f));
        ring = MakeLine("ImpactRing", 25);
        ring.loop = true;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
        }
        sparks = new LineRenderer[kind == 1 ? 6 : 3];
        for (int i = 0; i < sparks.Length; i++) sparks[i] = MakeLine("ImpactSpark", 2);
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "ImpactCore";
        flash.transform.SetParent(transform, false);
        Collider collider = flash.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        flash.GetComponent<Renderer>().sharedMaterial = coreMaterial;
        core = flash.transform;
        UpdateView(0f);
        PlaySound();
        // Guard against an interrupted component update loop.
        Destroy(gameObject, 0.5f);
    }

    private void PlaySound()
    {
        if (hitClip == null) hitClip = Resources.Load<AudioClip>("MasatakaHitFeedback/impact_hit");
        if (hitClip == null) return;
        float volume = HitVolume * (SoundManager.Instance != null ? SoundManager.Instance.seVolume : 1f);
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        source.pitch = kind == 1 ? 1f : (kind == 2 ? 0.72f : 1.15f);
        source.PlayOneShot(hitClip, volume * (kind == 1 ? 1f : 0.3f));
    }

    private void Update()
    {
        age += Time.unscaledDeltaTime;
        if (age >= duration) { Destroy(gameObject); return; }
        UpdateView(age / duration);
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera != null) transform.rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
    }

    private void UpdateView(float t)
    {
        float alpha = (1f - t) * (1f - t);
        Tint(colorMaterial, new Color(color.r, color.g, color.b, alpha));
        Tint(coreMaterial, new Color(1f, 0.96f, 0.85f, alpha));
        ring.transform.localScale = Vector3.one * Mathf.Lerp(0.04f, radius, t);
        ring.startWidth = ring.endWidth = Mathf.Lerp(0.025f, 0f, t);
        for (int i = 0; i < sparks.Length; i++)
        {
            float angle = (i + 0.15f) * Mathf.PI * 2f / sparks.Length;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            float start = Mathf.Lerp(0.02f, radius * 0.85f, t);
            float end = start + radius * (1f - t) * 0.55f;
            sparks[i].SetPosition(0, direction * start);
            sparks[i].SetPosition(1, direction * end);
            sparks[i].startWidth = 0.022f * (1f - t);
            sparks[i].endWidth = 0f;
        }
        core.localScale = Vector3.one * (kind == 1 ? 0.11f : 0.065f) * Mathf.Max(0f, 1f - t * 3f);
    }

    private void OnDestroy()
    {
        if (colorMaterial != null) Destroy(colorMaterial);
        if (coreMaterial != null) Destroy(coreMaterial);
    }
}
