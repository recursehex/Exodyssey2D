#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Cheat-menu-only readouts and setters for the loot director's run state,
/// so pity, pacing, and fuel behavior can be inspected and manipulated live.
/// </summary>
public partial class LootManager
{
	public LootState Debug_State => Director?.State;

	/// <summary>
	/// Live loot-state readout for the cheat menu and the `loot` command.
	/// </summary>
	public string Debug_GetStatus()
	{
		LootState State = Debug_State;
		if (State == null)
			return "Loot director not initialized";
		string sinceAnomalous = State.gridsSinceAnomalous >= LootState.neverSpawned
			? "never" : State.gridsSinceAnomalous.ToString();
		StringBuilder Builder = new();
		Builder.AppendLine($"Grid #: {State.globalGridNumber}   Seed: {State.runSeed}");
		Builder.AppendLine($"Rare pity: {State.rarePityOffset:F1}   Rare+ seen: {State.hasRarePlusSpawned}");
		Builder.AppendLine($"Anomalous this region: {State.anomalousSpawnedThisRegion}   Grids since Anomalous: {sinceAnomalous}");
		Builder.AppendLine($"Fuel meter: {State.fuelMeter}   Fuel this region: {State.fuelSpawnedThisRegion}");
		Builder.AppendLine($"History: {FormatItems(State.RecentItemHistory)}");
		Builder.Append($"Uniques spawned: {FormatItems(State.UniqueItemsSpawned)}");
		return Builder.ToString();
	}

	private static string FormatItems(List<int> Indices)
	{
		if (Indices.Count == 0)
			return "(none)";
		List<string> Names = new();
		foreach (int index in Indices)
			Names.Add(((ItemInfo.Tags)index).ToString());
		return string.Join(", ", Names);
	}

	/// <summary>
	/// Overrides the Rare pity offset (clamped to the tuning cap).
	/// </summary>
	public void Debug_SetPity(float value)
	{
		if (Debug_State != null)
			Debug_State.rarePityOffset = System.Math.Min(value, LootProfileInfo.Tuning.rarePityCap);
	}

	/// <summary>
	/// Clears duplicate-suppression memory: recent history and unique flags.
	/// </summary>
	public void Debug_ClearHistory()
	{
		if (Debug_State == null)
			return;
		Debug_State.RecentItemHistory.Clear();
		Debug_State.UniqueItemsSpawned.Clear();
	}
}
#endif
