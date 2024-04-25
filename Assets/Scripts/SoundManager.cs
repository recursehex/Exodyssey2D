using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public AudioSource efxSource;
	public AudioSource musicSource;
	public static SoundManager instance = null;

	// Start is called before the first frame update
	void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
	}

	public void PlaySound(AudioClip clip)
	{
		efxSource.pitch = Random.Range(0.95f, 1.05f);
		efxSource.PlayOneShot(clip);
	}
}
