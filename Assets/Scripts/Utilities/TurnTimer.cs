using UnityEngine;
using UnityEngine.UI;

public class TurnTimer : MonoBehaviour
{
	public float timeRemaining = 15;
	private readonly float timerLimit = 15;
	public bool timerIsRunning = false;
	public Text TimeText;
	private static Color NormalColor = new(115/255f, 119/255f, 160/255f);
	private static Color OutOfTimeColor = new(172/255f, 22/255f, 45/255f);
	private int lastDisplayedSeconds = -1;
	public void StartTimer() => timerIsRunning = true;
	// Pauses the countdown without expiring it: remaining time and display are kept
	public void StopTimer() => timerIsRunning = false;
	private void ExpireTimer()
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
		lastDisplayedSeconds = -1;
		DisplayTime(timeRemaining);
	}
	public void RefundElapsedTime()
	{
		timeRemaining = timerLimit;
		timerIsRunning = true;
		TimeText.color = NormalColor;
		lastDisplayedSeconds = -1;
		DisplayTime(timeRemaining);
	}
	void Update()
	{
		if (!timerIsRunning)
			return;
		timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
		DisplayTime(timeRemaining);
		if (timeRemaining <= 0f)
			ExpireTimer();
	}
	private void DisplayTime(float timeToDisplay)
	{
		int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeToDisplay) % 60);
		if (seconds == lastDisplayedSeconds)
			return;
		lastDisplayedSeconds = seconds;
		TimeText.text = string.Format(":{0:00}", seconds);
	}
}
