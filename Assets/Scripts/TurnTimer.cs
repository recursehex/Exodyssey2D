using UnityEngine;
using UnityEngine.UI;

public class TurnTimer : MonoBehaviour
{
	public float timeRemaining = 15;
	public float timerLimit = 15;
	public bool timerIsRunning = false;
	public Text TimeText;
	public void StartTimer()
	{
		timerIsRunning = true;
	}
	public void ResetTimer()
	{
		timeRemaining = timerLimit;
		TimeText.color = new Color((float)(115.0 / 256.0), (float)(119.0 / 256.0), (float)(160.0 / 256.0), 1);
		DisplayTime(timeRemaining - 1);
	}
	void Update()
	{
		if (!timerIsRunning)
		{
			return;
		}
		float dt = Time.deltaTime;
		if (timeRemaining - dt > 0)
		{
			timeRemaining -= dt;
			DisplayTime(timeRemaining);
		}
		else
		{
			timeRemaining = 0;
			timerIsRunning = false;
			TimeText.color = new Color((float)(172.0 / 256.0), (float)(22.0 / 256.0), (float)(45.0 / 256.0), 1);
			DisplayTime(-1);
			GameManager.Instance.OnTurnTimerEnd();
		}
	}
	void DisplayTime(float timeToDisplay)
	{
		timeToDisplay += 1;
		float seconds = Mathf.FloorToInt(timeToDisplay % 60);
		TimeText.text = string.Format(":{0:00}", seconds);
	}
}