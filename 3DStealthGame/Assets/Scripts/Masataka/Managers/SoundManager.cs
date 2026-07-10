using UnityEngine;

/// <summary>
/// BGMとSEをまとめて扱う。
/// シーンをまたいでも1つだけ残す。
/// </summary>
public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance;

	#region インスペクター設定

	[Header("BGM")]
	public AudioClip bgm;

	[Header("SE")]
	public AudioClip seGimmickClear;
	public AudioClip seClear;
	public AudioClip sePunch;
	public AudioClip seRespawn;
	public AudioClip seButton;
	public AudioClip sePickup;
	public AudioClip seWalk;
	public AudioClip seDetected;
	public AudioClip seNotification;
	public AudioClip seLostSight;

	[Header("音量")]
	[Range(0f, 1f)] public float bgmVolume = 0.5f;
	[Range(0f, 1f)] public float seVolume = 1.0f;

	#endregion

	#region 内部状態

	private AudioSource _bgmSource;
	private AudioSource _seSource;
	private AudioSource _walkSource;

	#endregion

	#region Unityイベント

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		InitializeAudioSources();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	#endregion

	#region 初期化

	private void InitializeAudioSources()
	{
		if (_bgmSource != null) return;

		_bgmSource = gameObject.AddComponent<AudioSource>();
		_bgmSource.loop = true;
		_bgmSource.volume = bgmVolume;

		_seSource = gameObject.AddComponent<AudioSource>();
		_seSource.volume = seVolume;

		_walkSource = gameObject.AddComponent<AudioSource>();
		_walkSource.loop = true;
		_walkSource.volume = seVolume * 0.6f;
		_walkSource.clip = seWalk;
	}

	#endregion

	#region BGM

	public void PlayBGM()
	{
		if (bgm == null || _bgmSource == null) return;

		if (_bgmSource.clip == bgm && _bgmSource.isPlaying)
		{
			return;
		}

		_bgmSource.clip = bgm;
		_bgmSource.loop = true;
		_bgmSource.volume = bgmVolume;
		_bgmSource.Play();
	}

	public void StopBGM()
	{
		if (_bgmSource == null) return;

		_bgmSource.Stop();
	}

	#endregion

	#region SE

	public void PlayGimmickClear() => PlaySE(seGimmickClear);
	public void PlayClear() => PlaySE(seClear);
	public void PlayPunch() => PlaySE(sePunch);
	public void PlayRespawn() => PlaySE(seRespawn);
	public void PlayButton() => PlaySE(seButton);
	public void PlayPickup() => PlaySE(sePickup);
	public void PlayDetected() => PlaySE(seDetected);
	public void PlayNotification() => PlaySE(seNotification);
	public void PlayLostSight() => PlaySE(seLostSight);

	#endregion

	#region 歩行音

	public void StartWalk()
	{
		if (seWalk == null || _walkSource == null) return;
		if (_walkSource.isPlaying) return;

		_walkSource.clip = seWalk;
		_walkSource.loop = true;
		_walkSource.volume = seVolume * 0.6f;
		_walkSource.Play();
	}

	public void StopWalk()
	{
		if (_walkSource == null) return;

		if (_walkSource.isPlaying)
		{
			_walkSource.Stop();
		}
	}

	#endregion

	#region 内部処理

	private void PlaySE(AudioClip clip)
	{
		if (clip == null || _seSource == null) return;

		_seSource.PlayOneShot(clip, seVolume);
	}

	#endregion
}