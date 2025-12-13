using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public AudioSource EfxSource;
	public AudioSource MusicSource;
	public static SoundManager Instance = null;
	private readonly float defaultMusicVolume = 1.0f;
	[Header("Audio Tuning")]
	[SerializeField] private float pitchJitterMin = 0.95f;
	[SerializeField] private float pitchJitterMax = 1.05f;
	void Awake()
	{
		if (Instance == null)
			Instance = this;
		else if (Instance != this)
			Destroy(gameObject);
		DontDestroyOnLoad(gameObject);
	}
	/// <summary>
	/// Plays background music
	/// </summary>
	public void PlayMusic(bool loop = true)
	{
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
		float startVolume = MusicSource.volume;
		for (float t = 0; t < duration; t += Time.deltaTime)
		{
			MusicSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
			yield return null; // Wait for the next frame
		}
		MusicSource.volume = 0;
		MusicSource.Stop();
	}
}
