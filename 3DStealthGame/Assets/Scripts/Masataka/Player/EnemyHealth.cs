using System.Collections;
using UnityEngine;

/// <summary>
/// 敵AIと独立した仮HP処理。
/// 敵担当の正式実装後もIDamageableを維持すれば、弾側を変更せず接続できる。
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.08f;

    private int currentHealth;
    private Coroutine hitFlashCoroutine;

    private bool initialized;

    public int CurrentHealth { get { EnsureInitialized(); return currentHealth; } }
    public int MaxHealth => maxHealth;

    public void ApplyCoopHealth(int value)
    {
        ApplyCoopHealth(value, maxHealth, value > 0);
    }

    // Called only for a host snapshot; never sends damage back to the host.
    public void ApplyCoopHealth(int value, int synchronizedMax, bool active)
    {
        EnsureInitialized();
        int previous = currentHealth;
        if (synchronizedMax > 0) maxHealth = synchronizedMax;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        GameObject root = EnemyObject();
        if (!active || currentHealth == 0)
        {
            if (root.activeSelf) root.SetActive(false);
            return;
        }
        if (!root.activeSelf) root.SetActive(true);
        if (currentHealth < previous) PlayHitFlash();
    }

    private void Awake() { EnsureInitialized(); }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        EnsureInitialized();
        if (CoopGameSession.Instance != null && !CoopGameSession.Instance.IsHost) return;
        if (damage <= 0 || currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        PlayHitFlash();

        Debug.Log($"[EnemyHealth] {name}: {currentHealth}/{maxHealth}");

        if (currentHealth == 0)
            OnDefeated();
    }

    private void PlayHitFlash()
    {
        // Repeated hits must not stop the coroutine while the material is white.
        if (hitFlashCoroutine == null && gameObject.activeInHierarchy)
            hitFlashCoroutine = StartCoroutine(FlashOnHit());
    }

    private IEnumerator FlashOnHit()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;
            originalColors[i] = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.color;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            else
                material.color = Color.white;
        }

        yield return new WaitForSeconds(hitFlashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material material = renderers[i].material;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", originalColors[i]);
            else
                material.color = originalColors[i];
        }

        hitFlashCoroutine = null;
    }

    private void OnDefeated()
    {
        // HP0のまま見た目とAIだけ残って動く状態にしない。
        Debug.Log($"[EnemyHealth] {name}を倒しました");
        EnemyObject().SetActive(false);
    }

    private GameObject EnemyObject()
    {
        EnemyManager ai = GetComponentInParent<EnemyManager>();
        GameObject enemyObject = ai != null ? ai.gameObject : gameObject;
        if (ai == null)
        {
            for (Transform t = transform; t != null; t = t.parent)
                if (t.tag == "Enemy" || t.tag == "StrongEnemy") { enemyObject = t.gameObject; break; }
        }
        return enemyObject;
    }
}
