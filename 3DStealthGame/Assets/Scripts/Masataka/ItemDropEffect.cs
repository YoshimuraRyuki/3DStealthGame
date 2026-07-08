using UnityEngine;

/// <summary>
/// ドロップアイテムの放物線演出。
/// 物理演算を使わず座標を直接動かすため、両クライアントで着地点が一致する
/// </summary>
public class ItemDropEffect : MonoBehaviour
{
	Vector3 startPos;   // 発射位置
	Vector3 endPos;     // 着地位置
	float duration = 0.6f;  // 飛んでいる時間
	float arcHeight = 1.2f; // 放物線の高さ（頂点の盛り上がり）
	float elapsed = 0f;

	/// <summary>
	/// 発射位置と着地位置を設定する
	/// </summary>
	public void Init(Vector3 start, Vector3 end)
	{
		startPos = start;
		endPos = end;
		transform.position = start;
	}

	void Update()
	{
		elapsed += Time.deltaTime;
		float t = Mathf.Clamp01(elapsed / duration);

		// 水平方向は等速で移動
		Vector3 pos = Vector3.Lerp(startPos, endPos, t);

		// 高さは放物線を上乗せする（t=0.5で最大
		pos.y += arcHeight * 4f * t * (1f - t);

		transform.position = pos;

		// 着地したら演出終了
		if (t >= 1f)
		{
			transform.position = endPos;
			Destroy(this); 
		}
	}
}