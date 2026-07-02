using UnityEngine;
using System.Collections;

public class StaminaAbsorbEffect : MonoBehaviour
{
	public int particleCount = 5;
	public float speed = 5f;
	public Color color = Color.blue;

	private int _aliveCount = 0; // ìÆÇ¢ÇƒÇÈó±ÇÃêî

	public void Play(Vector3 itemPos, Transform target, Color col)
	{
		color = col;
		_aliveCount = particleCount;
		for (int i = 0; i < particleCount; i++)
			StartCoroutine(SpawnParticle(itemPos, target));
	}

	private IEnumerator SpawnParticle(Vector3 startPos, Transform target)
	{
		GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		Destroy(p.GetComponent<Collider>());
		p.transform.localScale = Vector3.one * 0.08f;

		var mat = p.GetComponent<Renderer>().material;
		mat.color = color;

		var trail = p.AddComponent<TrailRenderer>();
		trail.time = 0.3f;
		trail.startWidth = 0.08f;
		trail.endWidth = 0f;
		trail.material = new Material(Shader.Find("Sprites/Default"));
		trail.startColor = color;
		trail.endColor = new Color(color.r, color.g, color.b, 0f);
		trail.autodestruct = false;

		p.transform.position = startPos + Random.insideUnitSphere * 0.4f;

		yield return new WaitForSeconds(Random.Range(0f, 0.25f));

		float currentSpeed = speed;

		while (p != null && target != null)
		{
			currentSpeed += 10f * Time.deltaTime;

			Vector3 targetPos = target.position + Vector3.up * 1.0f;
			p.transform.position = Vector3.MoveTowards(
				p.transform.position, targetPos, currentSpeed * Time.deltaTime);

			Vector3 pFlat = new Vector3(p.transform.position.x, 0, p.transform.position.z);
			Vector3 tFlat = new Vector3(target.position.x, 0, target.position.z);
			if (Vector3.Distance(pFlat, tFlat) < 0.5f)
			{
				Destroy(p);
				break;
			}
			yield return null;
		}

		if (p != null) Destroy(p);

		// ëSó±Ç™è¡Ç¶ÇΩÇÁñ{ëÃÇîjä¸
		_aliveCount--;
		if (_aliveCount <= 0)
			Destroy(gameObject);
	}
}