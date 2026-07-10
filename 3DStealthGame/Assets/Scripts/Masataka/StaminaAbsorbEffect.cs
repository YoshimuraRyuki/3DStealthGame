using UnityEngine;
using System.Collections;

/// <summary>
/// スタミナ回復時の吸い込み演出。
/// 小さな粒を生成し、対象プレイヤーへ向かって移動させる。
/// </summary>
public class StaminaAbsorbEffect : MonoBehaviour
{
	[Header("粒の設定")]
	public int particleCount = 5;
	public float speed = 5f;
	public Color color = Color.blue;

	private int _aliveCount = 0;
	private bool _isPlaying = false;

	public void Play(Vector3 itemPos, Transform target, Color col)
	{
		if (_isPlaying) return;

		if (target == null || particleCount <= 0)
		{
			Destroy(gameObject);
			return;
		}

		_isPlaying = true;
		color = col;
		_aliveCount = particleCount;

		for (int i = 0; i < particleCount; i++)
		{
			StartCoroutine(SpawnParticle(itemPos, target, color));
		}
	}

	private IEnumerator SpawnParticle(Vector3 startPos, Transform target, Color particleColor)
	{
		GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		particle.transform.SetParent(transform);
		particle.transform.localScale = Vector3.one * 0.08f;
		particle.transform.position = startPos + Random.insideUnitSphere * 0.4f;

		var collider = particle.GetComponent<Collider>();
		if (collider != null)
		{
			Destroy(collider);
		}

		var renderer = particle.GetComponent<Renderer>();
		if (renderer != null)
		{
			renderer.material.color = particleColor;
		}

		var trail = particle.AddComponent<TrailRenderer>();
		trail.time = 0.3f;
		trail.startWidth = 0.08f;
		trail.endWidth = 0f;
		trail.material = new Material(Shader.Find("Sprites/Default"));
		trail.startColor = particleColor;
		trail.endColor = new Color(particleColor.r, particleColor.g, particleColor.b, 0f);
		trail.autodestruct = false;

		yield return new WaitForSeconds(Random.Range(0f, 0.25f));

		float currentSpeed = speed;

		while (particle != null && target != null)
		{
			currentSpeed += 10f * Time.deltaTime;

			Vector3 targetPos = target.position + Vector3.up;
			particle.transform.position = Vector3.MoveTowards(
				particle.transform.position,
				targetPos,
				currentSpeed * Time.deltaTime
			);

			Vector3 particleFlat = new Vector3(particle.transform.position.x, 0f, particle.transform.position.z);
			Vector3 targetFlat = new Vector3(target.position.x, 0f, target.position.z);

			if (Vector3.Distance(particleFlat, targetFlat) < 0.5f)
			{
				break;
			}

			yield return null;
		}

		if (particle != null)
		{
			Destroy(particle);
		}

		OnParticleFinished();
	}

	private void OnParticleFinished()
	{
		_aliveCount--;

		if (_aliveCount <= 0)
		{
			Destroy(gameObject);
		}
	}
}