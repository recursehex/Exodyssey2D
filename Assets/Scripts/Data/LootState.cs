using System;
using System.Collections.Generic;

/// <summary>
/// Mutable loot generation state for the current run: Rare pity, recent-drop
/// history, Anomalous pacing, unique-per-run tracking, and the fuel meter.
/// All counters advance when items generate, never on player actions.
/// Serializable so it can ride along with future save data
/// </summary>
[Serializable]
public class LootState
{
	public const int neverSpawned = 9999;			// Sentinel for gridsSinceAnomalous before any Anomalous drop
	public float rarePityOffset;					// Added to the base Rare weight, grows on non-Rare drops
	public bool hasRarePlusSpawned;					// If any Rare+ item has generated this run (backstop check)
	public int anomalousSpawnedThisRegion;			// Counts toward the region's AnomalousCap
	public int gridsSinceAnomalous = neverSpawned;	// 0 = this grid, drives the anti-chain multiplier
	public int fuelMeter;							// Percent chance to convert a slot into fuel this grid
	public int fuelSpawnedThisRegion;				// Fuel items generated this region (budget backstop)
	public int globalGridNumber;					// Grids generated across the whole run
	public int runSeed;								// XORed with globalGridNumber to seed each grid's RNG
	public List<int> RecentItemHistory = new();		// Ring buffer of recent non-Common item indices
	public List<int> UniqueItemsSpawned = new();	// uniquePerRun item indices already generated
	/// <summary>
	/// Resets counters that pace loot within a single region
	/// </summary>
	public void ResetForNewRegion()
	{
		anomalousSpawnedThisRegion = 0;
		fuelSpawnedThisRegion = 0;
	}
	/// <summary>
	/// Resets all state for a fresh run
	/// </summary>
	public void ResetForNewRun(int newRunSeed, float pityStart)
	{
		rarePityOffset = pityStart;
		hasRarePlusSpawned = false;
		gridsSinceAnomalous = neverSpawned;
		fuelMeter = 0;
		globalGridNumber = 0;
		runSeed = newRunSeed;
		RecentItemHistory.Clear();
		UniqueItemsSpawned.Clear();
		ResetForNewRegion();
	}
	/// <summary>
	/// Records an item into recent history, keeping at most historySize entries
	/// </summary>
	public void PushHistory(int itemIndex, int historySize)
	{
		RecentItemHistory.Add(itemIndex);
		while (RecentItemHistory.Count > historySize)
			RecentItemHistory.RemoveAt(0);
	}
}
