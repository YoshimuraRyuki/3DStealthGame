using UnityEngine;

/// <summary>
/// ゲーム内のBGM・SEを一元管理するクラス。
/// どこからでも SoundManager.Instance?.Play〇〇() で呼び出せる。
/// </summary>
public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance;

	#region インスペクター設定

	[Header("BGM")]
	public AudioClip bgm;

	[Header("SE")]
	public AudioClip seGimmickClear;  // ギミック作動時
	public AudioClip seClear;         // ゲームクリア時
	public AudioClip sePunch;         // パンチアニメ再生時
	public AudioClip seRespawn;       // リスポーン時
	public AudioClip seButton;        // UIボタン押下時
	public AudioClip sePickup;        // アイテム取得時（スタミナアイテムと共用）
	public AudioClip seWalk;          // 歩行音
	public AudioClip seDetected;      // 敵に発見されたとき
	public AudioClip seNotification;  // ログ通知時
	public AudioClip seLostSight;     // 敵がプレイヤーを見失ったとき

	[Header("音量設定")]
	[Range(0f, 1f)] public float bgmVolume = 0.5f;
	[Range(0f, 1f)] public float seVolume = 1.0f;

	#endregion

	#region フィールド

	private AudioSource _bgmSource;
	private AudioSource _seSource;
	private AudioSource _walkSource; // 歩行音用（ループ再生）

	#endregion

	#region Unityイベント

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		// BGM用AudioSource
		_bgmSource = gameObject.AddComponent<AudioSource>();
		_bgmSource.loop = true;
		_bgmSource.volume = bgmVolume;

		// SE用AudioSource
		_seSource = gameObject.AddComponent<AudioSource>();
		_seSource.volume = seVolume;

		// 歩行音用AudioSource（ループ）
		_walkSource = gameObject.AddComponent<AudioSource>();
		_walkSource.loop = true;
		_walkSource.volume = seVolume * 0.6f;
		_walkSource.clip = seWalk;
	}

	#endregion

	#region BGM

	/// <summary>
	/// BGMを再生する
	/// </summary>
	public void PlayBGM()
	{
		if (bgm == null) return;
		_bgmSource.clip = bgm;
		_bgmSource.Play();
	}

	/// <summary>
	/// BGMを停止する
	/// </summary>
	public void StopBGM()
	{
		_bgmSource.Stop();
	}

	#endregion

	#region SE

	/// <summary>ギミック作動音</summary>
	public void PlayGimmickClear() => PlaySE(seGimmickClear);

	/// <summary>ゲームクリア音</summary>
	public void PlayClear() => PlaySE(seClear);

	/// <summary>パンチ音</summary>
	public void PlayPunch() => PlaySE(sePunch);

	/// <summary>リスポーン音</summary>
	public void PlayRespawn() => PlaySE(seRespawn);

	/// <summary>UIボタン音</summary>
	public void PlayButton() => PlaySE(seButton);

	/// <summary>アイテム取得音（スタミナアイテムと共用）</summary>
	public void PlayPickup() => PlaySE(sePickup);

	/// <summary>発見音</summary>
	public void PlayDetected() => PlaySE(seDetected);

	/// <summary>ログ通知音</summary>
	public void PlayNotification() => PlaySE(seNotification);

	/// <summary>見失い音</summary>
	public void PlayLostSight() => PlaySE(seLostSight);

	#endregion

	#region 歩行音（ループ）

	/// <summary>
	/// 歩行音を開始する（移動中に呼ぶ）
	/// </summary>
	public void StartWalk()
	{
		if (seWalk == null || _walkSource.isPlaying) return;
		_walkSource.Play();
	}

	/// <summary>
	/// 歩行音を停止する（停止時に呼ぶ）
	/// </summary>
	public void StopWalk()
	{
		if (_walkSource.isPlaying)
			_walkSource.Stop();
	}

	#endregion

	#region 内部処理

	private void PlaySE(AudioClip clip)
	{
		if (clip == null) return;
		_seSource.PlayOneShot(clip, seVolume);
	}

	#endregion
}