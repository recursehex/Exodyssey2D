using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public AudioSource EfxSource;
	public AudioSource MusicSource;
	public static SoundManager Instance = null;
	private readonly float defaultMusicVolume = 1.0f;
	private AudioClip DefaultMusicClip;
	[Header("Audio Tuning")]
	[SerializeField] private float pitchJitterMin = 0.95f;
	[SerializeField] private float pitchJitterMax = 1.05f;
	[SerializeField] private float clipVolume = 1.0f;
	[SerializeField] private float fadeDuration = 0.5f;
	void Awake()
	{
		if (Instance == null)
			Instance = this;
		else if (Instance != this)
			Destroy(gameObject);
		DontDestroyOnLoad(gameObject);
		if (MusicSource != null)
			DefaultMusicClip = MusicSource.clip;
	}
	/// <summary>
	/// Plays background music
	/// </summary>
	public void PlayMusic(bool loop = true)
	{
		if (MusicSource == null)
			return;
		if (DefaultMusicClip == null)
			DefaultMusicClip = MusicSource.clip;
		if (DefaultMusicClip != null && MusicSource.clip != DefaultMusicClip)
			MusicSource.clip = DefaultMusicClip;
		MusicSource.loop = loop;
		MusicSource.volume = defaultMusicVolume;
		MusicSource.Play();
	}
	/// <summary>
	/// Plays sound effect with pitch variation
	/// </summary>
	public void PlaySound(AudioClip Clip)
	{
		EfxSource.pitch = Random.Range(pitchJitterMin, pitchJitterMax);
		EfxSource.PlayOneShot(Clip);
	}
	/// <summary>
	/// Fades out music over specified duration
	/// </summary>
	public IEnumerator FadeOutMusic(float duration)
	{
		if (MusicSource == null)
			yield break;
		float startVolume = MusicSource.volume;
		for (float t = 0; t < duration; t += Time.deltaTime)
		{
			MusicSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
			yield return null; // Wait for the next frame
		}
		MusicSource.volume = 0;
		MusicSource.Stop();
	}
	/// <summary>
	/// Fades out current music and plays a non-looping clip
	/// </summary>
	public void PlayGameOver(AudioClip Clip)
	{
		if (MusicSource == null)
			return;
		StartCoroutine(FadeOutMusicThenPlay(Clip, fadeDuration));
	}
	/// <summary>
	/// Fades out current music and plays a non-looping clip
	/// </summary>
	private IEnumerator FadeOutMusicThenPlay(AudioClip Clip, float duration)
	{
		if (duration > 0f && MusicSource.isPlaying)
			yield return FadeOutMusic(duration);
		else
			MusicSource.Stop();
		if (Clip == null)
			yield break;
		if (DefaultMusicClip == null)
			DefaultMusicClip = MusicSource.clip;
		MusicSource.clip = Clip;
		MusicSource.loop = false;
		MusicSource.volume = 0f;
		MusicSource.Play();
		if (fadeDuration <= 0f)
		{
			MusicSource.volume = clipVolume;
			yield break;
		}
		for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
		{
			MusicSource.volume = Mathf.Lerp(0f, clipVolume, t / fadeDuration);
			yield return null;
		}
		MusicSource.volume = clipVolume;
	}
}
