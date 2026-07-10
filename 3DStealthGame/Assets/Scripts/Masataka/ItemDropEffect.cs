using UnityEngine;

/// <summary>
/// ドロップアイテムの放物線演出。
/// 物理演算を使わず、指定した着地点へ移動させる。
/// </summary>
public class ItemDropEffect : MonoBehaviour
{
	[SerializeField] private float duration = 0.6f;
	[SerializeField] private float arcHeight = 1.2f;

	private Vector3 _startPos;
	private Vector3 _endPos;
	private float _elapsed = 0f;
	private bool _initialized = false;

	public void Init(Vector3 start, Vector3 end)
	{
		_startPos = start;
		_endPos = end;
		_elapsed = 0f;
		_initialized = true;

		transform.position = start;
	}

	private void Update()
	{
		if (!_initialized) return;
		if (duration <= 0f)
		{
			Finish();
			return;
		}

		_elapsed += Time.deltaTime;

		float t = Mathf.Clamp01(_elapsed / duration);

		Vector3 pos = Vector3.Lerp(_startPos, _endPos, t);

		// t=0.5で一番高くなる
		pos.y += arcHeight * 4f * t * (1f - t);

		transform.position = pos;

		if (t >= 1f)
		{
			Finish();
		}
	}

	private void Finish()
	{
		transform.position = _endPos;

		// アイテム本体は残して、演出用コンポーネントだけ外す
		Destroy(this);
	}
}