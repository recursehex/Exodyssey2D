using UnityEngine;
using UnityEngine.UI;

public class TurnTimer : MonoBehaviour
{
	public float timeRemaining = 15;
	private readonly float timerLimit = 15;
	public bool timerIsRunning = false;
	public Text TimeText;
	private Color NormalColor = new((float)(115.0 / 256.0), (float)(119.0 / 256.0), (float)(160.0 / 256.0), 1);
	private Color OutOfTimeColor = new((float)(172.0 / 256.0), (float)(22.0 / 256.0), (float)(45.0 / 256.0), 1);
	public void StartTimer()
	{
		timerIsRunning = true;
	}
	public void ResetTimer()
	{
		timeRemaining = timerLimit;
		TimeText.color = NormalColor;
		DisplayTime(timeRemaining);
	}
	void Update()
	{
		if (!timerIsRunning)
		{
			return;
		}
		float dt = Time.deltaTime;
		if (timeRemaining - dt > 1)
		{
			timeRemaining -= dt;
			DisplayTime(timeRemaining);
		}
		else
		{
			timeRemaining = 0;
			timerIsRunning = false;
			TimeText.color = OutOfTimeColor;
			DisplayTime(timeRemaining);
			GameManager.Instance.OnTurnTimerEnd();
		}
	}
	private void DisplayTime(float timeToDisplay)
	{
		float seconds = Mathf.FloorToInt(timeToDisplay % 60);
		TimeText.text = string.Format(":{0:00}", seconds);
	}
}