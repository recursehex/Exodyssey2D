#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// Cheat-menu-only setters for LevelManager's private state.
/// </summary>
public partial class LevelManager
{
	/// <summary>
	/// Forces the time of day, updating the label and notifying lighting.
	/// </summary>
	public void Debug_SetTimeOfDay(TimeOfDay Time)
	{
		CurrentTimeOfDay = Time;
		if (LevelText != null)
			LevelText.text = timeOfDayNames[(int)Time];
		OnTimeOfDayChanged?.Invoke(Time);
	}

	public void Debug_SetLevel(int value)
	{
		Level = Mathf.Max(0, value);
		// Derive day and time of day from the level the same way natural progression does
		Day = Level / timeOfDayNames.Length + 1;
		if (DayText != null)
			DayText.text = $"DAY {Day}";
		UpdateTimeOfDay(emitEvent: true);
	}

	public void Debug_SetDay(int value)
	{
		Day = Mathf.Max(1, value);
		if (DayText != null)
			DayText.text = $"DAY {Day}";
	}

	/// <summary>
	/// Clears both tilemaps so the level can be regenerated in place.
	/// </summary>
	public void Debug_ClearTilemaps() => ClearTilemaps();
}
#endif
