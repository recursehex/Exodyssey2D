using UnityEngine;
using UnityEngine.UI;

public class TurnTimer : MonoBehaviour
{
	public float timeRemaining = 15;
	private readonly float timerLimit = 15;
	public bool timerIsRunning = false;
	public Text TimeText;
	private Color NormalColor = new(115f / 256f, 119f / 256f, 160f / 256f, 1);
	private Color OutOfTimeColor = new(172f / 256f, 22f / 256f, 45f / 256f, 1);
	public void StartTimer()
	{
		timerIsRunning = true;
	}
	public void StopTimer()
	{
		timeRemaining = 0;
		timerIsRunning = false;
		TimeText.color = OutOfTimeColor;
		DisplayTime(timeRemaining);
		GameManager.Instance.OnTurnTimerEnd();
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
			StopTimer();
		}
	}
	private void DisplayTime(float timeToDisplay)
	{
		float seconds = Mathf.FloorToInt(timeToDisplay % 60);
		TimeText.text = string.Format(":{0:00}", seconds);
	}
}