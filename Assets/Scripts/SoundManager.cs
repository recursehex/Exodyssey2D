using System.Threading.Tasks;
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
	public void PlayMusic(bool loop = true)
    {
        MusicSource.loop   = loop;
        MusicSource.volume = defaultMusicVolume;
        MusicSource.Play();
    }
	public void PlaySound(AudioClip Clip)
	{
		EfxSource.pitch = Random.Range(0.95f, 1.05f);
		EfxSource.PlayOneShot(Clip);
	}
	public async void FadeOutMusic(float duration)
    {
        float startVolume = MusicSource.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            MusicSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
            await Task.Yield(); // Wait for the next frame
        }
        MusicSource.volume = 0;
        MusicSource.Stop();
    }
}
