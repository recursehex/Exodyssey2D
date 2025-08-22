using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public AudioSource EfxSource;
	public AudioSource MusicSource;
	public static SoundManager Instance = null;
	private readonly float defaultMusicVolume = 1.0f;
	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
	}
	/// <summary>
	/// Plays background music
	/// </summary>
	/// <param name="loop"></param>
	public void PlayMusic(bool loop = true)
	{
		MusicSource.loop = loop;
		MusicSource.volume = defaultMusicVolume;
		MusicSource.Play();
	}
	/// <summary>
	/// Plays sound effect with pitch variation
	/// </summary>
	/// <param name="Clip"></param>
	public void PlaySound(AudioClip Clip)
	{
		EfxSource.pitch = Random.Range(0.95f, 1.05f);
		EfxSource.PlayOneShot(Clip);
	}
	/// <summary>
	/// Fades out music over specified duration
	/// </summary>
	/// <param name="duration"></param>
	/// <returns></returns>
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
