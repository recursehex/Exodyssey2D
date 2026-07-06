using System;

/// <summary>
/// Tuning constants for the loot director, loaded from the Tuning block of
/// LootTables.json. Field defaults double as fallbacks when the JSON omits
/// a value
/// </summary>
[Serializable]
public class LootTuning
{
	// Rare pity: effective Rare weight = base weight + pity offset
	public float rarePityStart = -2f;			// Offset at run start, keeps grid 1 free of Rare drops
	public float rarePityPerItem = 1.2f;		// Offset gained per non-Rare item generated
	public float rarePityCap = 20f;				// Max offset from drought protection
	public string BackstopRegion = "FragmentedCoast";	// Region whose backstop grid guarantees the first Rare weapon
	public int backstopGrid = 4;				// 1-based grid within BackstopRegion that forces the guarantee
	// Anomalous pacing after one generates
	public int anomalousBlockedGrids = 2;		// Grids with Anomalous weight x0 after a drop (plus the drop's own grid)
	public int anomalousHalvedGrids = 2;		// Grids at reduced Anomalous weight after the blocked window
	public float anomalousHalvedMultiplier = 0.5f;	// Anomalous weight multiplier during the halved window
	// Duplicate suppression
	public int historySize = 10;				// Recent non-Common items remembered across grids
	public float historyMultiplierScarce = 0.35f;		// Weight multiplier for a Scarce item in recent history
	public float historyMultiplierRarePlus = 0.25f;		// Weight multiplier for a Rare+ item in recent history
	public float withinGridMultiplierCommon = 0.6f;		// Weight multiplier for a Common repeat within one grid
	public float withinGridMultiplierLimited = 0.25f;	// Weight multiplier for a Limited repeat within one grid
	// Out-of-depth escape valve
	public float oodEscalationChance = 0.03f;	// Chance a Scarce roll escalates one tier to Rare
	public string OodEscalationMinRegion = "RainforestRavines";	// Earliest region escalation can occur
	// Fuel meter (Power Cell pacing). With gain 40 and floor -5 the meter
	// passes 100 by the third dry grid, so a fuel drought can never exceed
	// 3 grids while fuel still averages one drop per 2-3 grids
	public int fuelMeterGainPerDryGrid = 40;	// Meter gained per grid generated without fuel
	public int fuelMeterCostOnSpawn = 80;		// Meter spent when a fuel item generates
	public int fuelMeterFloor = -5;				// Lowest the meter can go after a spawn
	public int minFuelItemsPerRegion = 2;		// Region fuel budget backstop
	public int fuelSurvivalFloorGrids = 2;		// Grids stranded with a vehicle and no fuel before a guaranteed drop
	// Grid flavor
	public float flavorCategoryMultiplier = 1.75f;	// Hidden per-grid bias for one rolled category on generic grids
	/// <summary>
	/// Region whose backstop grid guarantees the first Rare weapon
	/// </summary>
	public RegionInfo.Tags GetBackstopRegionTag() =>
		Enum.TryParse(BackstopRegion, out RegionInfo.Tags Parsed) ? Parsed : RegionInfo.Tags.FragmentedCoast;
	/// <summary>
	/// Earliest region where the Scarce-to-Rare escalation can occur
	/// </summary>
	public RegionInfo.Tags GetOodEscalationMinRegionTag() =>
		Enum.TryParse(OodEscalationMinRegion, out RegionInfo.Tags Parsed) ? Parsed : RegionInfo.Tags.RainforestRavines;
}
