using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public AudioSource EfxSource;
	public AudioSource MusicSource;
	public static SoundManager Instance = null;

	// Start is called before the first frame update
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

	public void PlaySound(AudioClip clip)
	{
		EfxSource.pitch = Random.Range(0.95f, 1.05f);
		EfxSource.PlayOneShot(clip);
	}
}
