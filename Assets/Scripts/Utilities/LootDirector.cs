using System;
using System.Collections.Generic;

/// <summary>
/// Pure loot-selection logic: rolls rarities from the region's interpolated
/// weight tables, then picks an item within the tier by loot weight. Enforces
/// the Anomalous region gate, per-region cap, and anti-chain pacing window.
/// Owns no scene or UnityEngine references so editor tests and simulations
/// can drive it directly
/// </summary>
public class LootDirector
{
	/// <summary>
	/// Loot-relevant snapshot of one spawnable item from ItemDefinitions
	/// </summary>
	public class Candidate
	{
		public int index;								// Template/Tags index used to spawn the item
		public Rarity Rarity = Rarity.Common;
		public List<LootCategory> Categories = new();
		public int lootWeight = 100;
		public bool uniquePerRun;
		public int minRegionIndex;
		public bool isWeapon;
	}
	/// <summary>
	/// Everything one grid's rolls need to know about where the run is
	/// </summary>
	public class Context
	{
		public int regionIndex;
		public int gridsCompleted;						// Region progress when this grid generates
		public int gridsRequired;
		public IReadOnlyList<int> RarityWeightsStart;	// C/L/S/R/A weights at region start
		public IReadOnlyList<int> RarityWeightsEnd;		// C/L/S/R/A weights at region end
		public int anomalousCap;
		public LootProfileInfo Profile;
	}
	public const int commonTier = 0;
	public const int limitedTier = 1;
	public const int scarceTier = 2;
	public const int rareTier = 3;
	public const int anomalousTier = 4;
	private const int tierCount = 5;
	private readonly List<Candidate> Candidates;
	private readonly LootTuning Tuning;
	private readonly List<LootCategory> FlavorableCategories;
	private LootCategory? FlavorCategory;	// Hidden per-grid bias rolled while planning
	public LootState State { get; }
	public LootDirector(List<Candidate> Candidates, LootTuning Tuning, LootState State = null)
	{
		this.Candidates = Candidates;
		this.Tuning = Tuning;
		this.State = State ?? new LootState();
		// Flavor only ever biases toward categories that exist in the pool
		HashSet<LootCategory> Present = new();
		foreach (Candidate Candidate in Candidates)
			foreach (LootCategory Category in Candidate.Categories)
				Present.Add(Category);
		FlavorableCategories = new List<LootCategory>(Present);
		FlavorableCategories.Sort();
	}
	/// <summary>
	/// Plans all ground-scatter items for one grid, advancing run state as
	/// each item is decided. Item count comes from the grid's loot profile.
	/// If the run's first Rare+ item is overdue, one slot is converted into
	/// a guaranteed Rare weapon (the "taste" backstop)
	/// </summary>
	public List<int> PlanGridLoot(Context Ctx, Random Rng)
	{
		State.globalGridNumber++;
		if (State.gridsSinceAnomalous < LootState.neverSpawned)
			State.gridsSinceAnomalous++;
		RollFlavorCategory(Ctx, Rng);
		int itemCount = Rng.Next(Ctx.Profile.MinItems, Ctx.Profile.MaxItems + 1);
		List<int> Plan = new();
		HashSet<int> PlacedThisGrid = new();
		if (!State.hasRarePlusSpawned && IsBackstopDue(Ctx))
		{
			int forcedIndex = PickFromTier(rareTier, Ctx, Rng, PlacedThisGrid, Candidate => Candidate.isWeapon);
			if (forcedIndex < 0)
				forcedIndex = PickFromTier(rareTier, Ctx, Rng, PlacedThisGrid);
			PlanForced(forcedIndex, Ctx, Plan, PlacedThisGrid, ref itemCount);
		}
		// The profile's guaranteed category slots are planned before random
		// rolls (this is how Fuel Line Yard always has fuel, Triage Corridor
		// always has a medkit, etc.)
		foreach (LootProfileInfo.GuaranteedSlot Slot in Ctx.Profile.Guaranteed)
		{
			for (int i = 0; i < Slot.count; i++)
				PlanForced(PickByCategory(Slot.Category, Ctx, Rng, PlacedThisGrid), Ctx, Plan, PlacedThisGrid, ref itemCount);
		}
		// Fuel meter: ground-scatter fuel comes off the rarity ramp entirely.
		// The meter converts one slot when it wins its roll (guaranteed by the
		// third dry grid), and the region fuel budget forces conversion when
		// the region is about to end short of fuel
		if (!PlanContainsFuel(Plan) && (IsRegionFuelBudgetDue(Ctx) || Rng.Next(100) < State.fuelMeter))
			PlanForced(PickByCategory(LootCategory.Fuel, Ctx, Rng, PlacedThisGrid), Ctx, Plan, PlacedThisGrid, ref itemCount);
		for (int i = 0; i < itemCount; i++)
		{
			int index = RollItem(Ctx, Rng, PlacedThisGrid);
			if (index >= 0)
			{
				Plan.Add(index);
				PlacedThisGrid.Add(index);
			}
		}
		UpdateFuelMeter(Plan);
		FlavorCategory = null;
		return Plan;
	}
	/// <summary>
	/// Adds a forced pick (backstop, guaranteed slot, or fuel conversion) to
	/// the plan, consuming one of the grid's item slots when any remain
	/// </summary>
	private void PlanForced(int index, Context Ctx, List<int> Plan, HashSet<int> PlacedThisGrid, ref int itemCount)
	{
		if (index < 0)
			return;
		Plan.Add(index);
		PlacedThisGrid.Add(index);
		RecordSpawn(index, Ctx);
		if (itemCount > 0)
			itemCount--;
	}
	/// <summary>
	/// Generic grids roll one hidden bias category so two plain grids in a
	/// row still feel different; the multiplier applies for this plan only
	/// </summary>
	private void RollFlavorCategory(Context Ctx, Random Rng)
	{
		FlavorCategory = null;
		if (Ctx.Profile.RollFlavorCategory && FlavorableCategories.Count > 0)
			FlavorCategory = FlavorableCategories[Rng.Next(FlavorableCategories.Count)];
	}
	private bool PlanContainsFuel(List<int> Plan)
	{
		foreach (int index in Plan)
		{
			Candidate Candidate = Candidates.Find(Candidate => Candidate.index == index);
			if (Candidate != null && Candidate.Categories.Contains(LootCategory.Fuel))
				return true;
		}
		return false;
	}
	/// <summary>
	/// True when the grids left in the region are exactly enough to still hit
	/// the region's minimum fuel budget, so each of them must carry fuel
	/// </summary>
	private bool IsRegionFuelBudgetDue(Context Ctx)
	{
		int gridsRemaining = Math.Max(1, Ctx.gridsRequired - Ctx.gridsCompleted);
		int fuelStillNeeded = Tuning.minFuelItemsPerRegion - State.fuelSpawnedThisRegion;
		return fuelStillNeeded >= gridsRemaining;
	}
	private void UpdateFuelMeter(List<int> Plan)
	{
		if (PlanContainsFuel(Plan))
			State.fuelMeter = Math.Max(Tuning.fuelMeterFloor, State.fuelMeter - Tuning.fuelMeterCostOnSpawn);
		else
			State.fuelMeter += Tuning.fuelMeterGainPerDryGrid;
	}
	/// <summary>
	/// Weighted pick across all tiers among candidates with the category,
	/// used for guaranteed slots and fuel conversions. Anomalous candidates
	/// still respect the gate, cap, and anti-chain window
	/// </summary>
	private int PickByCategory(LootCategory Category, Context Ctx, Random Rng, HashSet<int> PlacedThisGrid)
	{
		float[] EffectiveWeights = GetEffectiveWeights(Ctx);
		List<Candidate> Pool = new();
		List<double> PoolWeights = new();
		double totalWeight = 0;
		foreach (Candidate Candidate in Candidates)
		{
			if (Candidate.Rarity.Tag == Rarity.Tags.Tsurath)
				continue;
			if (!Candidate.Categories.Contains(Category))
				continue;
			if (Candidate.minRegionIndex > Ctx.regionIndex)
				continue;
			if (Candidate.uniquePerRun && State.UniqueItemsSpawned.Contains(Candidate.index))
				continue;
			int tier = TierIndexOf(Candidate.Rarity);
			if (tier == anomalousTier && EffectiveWeights[anomalousTier] <= 0f)
				continue;
			double weight = Candidate.lootWeight;
			weight *= WithinGridMultiplier(tier, Candidate.index, PlacedThisGrid);
			weight *= HistoryMultiplier(tier, Candidate.index);
			if (weight <= 0)
				continue;
			Pool.Add(Candidate);
			PoolWeights.Add(weight);
			totalWeight += weight;
		}
		if (totalWeight <= 0)
			return -1;
		double roll = Rng.NextDouble() * totalWeight;
		double cumulative = 0;
		for (int i = 0; i < Pool.Count; i++)
		{
			cumulative += PoolWeights[i];
			if (roll < cumulative)
				return Pool[i].index;
		}
		return Pool[Pool.Count - 1].index;
	}
	/// <summary>
	/// Rolls a container's contents (Reserve Crate, Weapon Safe, ...) using
	/// the container's profile for counts, rarity shift, category filter,
	/// and guaranteed slots. Container rolls advance the same pity, history,
	/// and Anomalous state as ground scatter, but never grid pacing or the
	/// fuel meter. Call at grid generation so contents are fixed up front
	/// </summary>
	public List<int> RollContainerLoot(Context Ctx, Random Rng)
	{
		int rollCount = Rng.Next(Ctx.Profile.MinItems, Ctx.Profile.MaxItems + 1);
		List<int> Loot = new();
		HashSet<int> PlacedThisContainer = new();
		foreach (LootProfileInfo.GuaranteedSlot Slot in Ctx.Profile.Guaranteed)
		{
			for (int i = 0; i < Slot.count; i++)
				PlanForced(PickByCategory(Slot.Category, Ctx, Rng, PlacedThisContainer), Ctx, Loot, PlacedThisContainer, ref rollCount);
		}
		for (int i = 0; i < rollCount; i++)
		{
			int index = RollItem(Ctx, Rng, PlacedThisContainer);
			if (index >= 0)
			{
				Loot.Add(index);
				PlacedThisContainer.Add(index);
			}
		}
		return Loot;
	}
	/// <summary>
	/// Rolls a Ts'urath-tier drop for Ts'urath kills and caches — the only
	/// two sources of that tier; the weighted paths can never select it.
	/// Returns -1 while no Ts'urath items exist in the database
	/// </summary>
	public int RollTsurathDrop(Random Rng)
	{
		List<Candidate> Pool = new();
		double totalWeight = 0;
		foreach (Candidate Candidate in Candidates)
		{
			if (Candidate.Rarity.Tag != Rarity.Tags.Tsurath)
				continue;
			if (Candidate.uniquePerRun && State.UniqueItemsSpawned.Contains(Candidate.index))
				continue;
			if (Candidate.lootWeight <= 0)
				continue;
			Pool.Add(Candidate);
			totalWeight += Candidate.lootWeight;
		}
		if (totalWeight <= 0)
			return -1;
		double roll = Rng.NextDouble() * totalWeight;
		double cumulative = 0;
		foreach (Candidate Candidate in Pool)
		{
			cumulative += Candidate.lootWeight;
			if (roll < cumulative || Candidate == Pool[Pool.Count - 1])
			{
				if (Candidate.uniquePerRun)
					State.UniqueItemsSpawned.Add(Candidate.index);
				return Candidate.index;
			}
		}
		return -1;
	}
	/// <summary>
	/// The backstop is due once the run reaches the configured grid of the
	/// backstop region (or any later region) without a single Rare+ drop
	/// </summary>
	private bool IsBackstopDue(Context Ctx)
	{
		int backstopRegionIndex = (int)Tuning.GetBackstopRegionTag();
		if (Ctx.regionIndex > backstopRegionIndex)
			return true;
		return Ctx.regionIndex == backstopRegionIndex && Ctx.gridsCompleted >= Tuning.backstopGrid - 1;
	}
	/// <summary>
	/// Rolls one item (rarity tier first, then an item within the tier) and
	/// records it into run state. Returns -1 when no candidate fits
	/// </summary>
	public int RollItem(Context Ctx, Random Rng, HashSet<int> PlacedThisGrid = null)
	{
		int tier = RollRarityTier(Ctx, Rng);
		int index = RollItemWithinTier(tier, Ctx, Rng, PlacedThisGrid);
		if (index >= 0)
			RecordSpawn(index, Ctx);
		return index;
	}
	/// <summary>
	/// Returns the region's interpolated rarity weights with run-state
	/// modifiers applied: the Rare pity offset, the Anomalous anti-chain
	/// window, and the Anomalous region cap
	/// </summary>
	public float[] GetEffectiveWeights(Context Ctx)
	{
		float t = Ctx.gridsRequired <= 0 ? 0f
			: Math.Clamp((float)Ctx.gridsCompleted / Ctx.gridsRequired, 0f, 1f);
		float[] Weights = new float[tierCount];
		for (int i = 0; i < tierCount; i++)
			Weights[i] = Ctx.RarityWeightsStart[i] + (Ctx.RarityWeightsEnd[i] - Ctx.RarityWeightsStart[i]) * t;
		// Pity only nudges regions where Rare is structurally possible; it
		// never unlocks Rare where the table says 0 (e.g. Ruined Outpost)
		if (Weights[rareTier] > 0f)
			Weights[rareTier] = Math.Max(0f, Weights[rareTier] + State.rarePityOffset);
		Weights[anomalousTier] *= AnomalousRecencyMultiplier();
		if (State.anomalousSpawnedThisRegion >= Ctx.anomalousCap)
			Weights[anomalousTier] = 0f;
		return Weights;
	}
	/// <summary>
	/// Weight multiplier from the anti-chain pacing window: x0 for the grids
	/// right after an Anomalous drop, then halved, then back to normal
	/// </summary>
	private float AnomalousRecencyMultiplier()
	{
		if (State.gridsSinceAnomalous <= Tuning.anomalousBlockedGrids)
			return 0f;
		if (State.gridsSinceAnomalous <= Tuning.anomalousBlockedGrids + Tuning.anomalousHalvedGrids)
			return Tuning.anomalousHalvedMultiplier;
		return 1f;
	}
	private int RollRarityTier(Context Ctx, Random Rng)
	{
		float[] Weights = GetEffectiveWeights(Ctx);
		float totalWeight = 0f;
		foreach (float weight in Weights)
			totalWeight += weight;
		if (totalWeight <= 0f)
			return commonTier;
		double roll = Rng.NextDouble() * totalWeight;
		double cumulative = 0;
		int tier = commonTier;
		for (int i = 0; i < tierCount; i++)
		{
			cumulative += Weights[i];
			if (roll < cumulative)
			{
				tier = i;
				break;
			}
		}
		// Special archetypes nudge the roll one tier, but a +1 shift can
		// never force its way past the Anomalous gate, cap, or anti-chain
		if (Ctx.Profile != null && Ctx.Profile.RarityShift != 0)
		{
			int shifted = Math.Clamp(tier + Ctx.Profile.RarityShift, commonTier, anomalousTier);
			if (shifted == anomalousTier && tier != anomalousTier && Weights[anomalousTier] <= 0f)
				shifted = rareTier;
			tier = shifted;
		}
		// Out-of-depth escape valve: a Scarce roll can climb exactly one tier
		// to Rare in late regions; escalation never produces Anomalous
		if (tier == scarceTier
			&& Ctx.regionIndex >= (int)Tuning.GetOodEscalationMinRegionTag()
			&& Rng.NextDouble() < Tuning.oodEscalationChance)
			tier = rareTier;
		return tier;
	}
	/// <summary>
	/// Picks an item from the tier; when a tier has no eligible candidates
	/// the roll downgrades one tier at a time instead of failing outright
	/// </summary>
	private int RollItemWithinTier(int startTier, Context Ctx, Random Rng, HashSet<int> PlacedThisGrid)
	{
		for (int tier = startTier; tier >= commonTier; tier--)
		{
			int index = PickFromTier(tier, Ctx, Rng, PlacedThisGrid);
			if (index >= 0)
				return index;
		}
		return -1;
	}
	private int PickFromTier(int tier, Context Ctx, Random Rng, HashSet<int> PlacedThisGrid, Predicate<Candidate> Filter = null)
	{
		List<Candidate> Pool = new();
		List<double> PoolWeights = new();
		double totalWeight = 0;
		foreach (Candidate Candidate in Candidates)
		{
			if (Candidate.Rarity.Tag == Rarity.Tags.Tsurath)
				continue;
			if (TierIndexOf(Candidate.Rarity) != tier)
				continue;
			// Fuel never enters the rarity roll; the fuel meter and the
			// profile's guaranteed slots are its only sources
			if (Candidate.Categories.Contains(LootCategory.Fuel))
				continue;
			if (Ctx.Profile != null && Ctx.Profile.IncludeCategories.Count > 0
				&& !HasAnyCategory(Candidate, Ctx.Profile.IncludeCategories))
				continue;
			if (Candidate.minRegionIndex > Ctx.regionIndex)
				continue;
			if (Candidate.uniquePerRun && State.UniqueItemsSpawned.Contains(Candidate.index))
				continue;
			if (Filter != null && !Filter(Candidate))
				continue;
			double weight = Candidate.lootWeight;
			weight *= WithinGridMultiplier(tier, Candidate.index, PlacedThisGrid);
			weight *= HistoryMultiplier(tier, Candidate.index);
			if (Ctx.Profile != null)
				weight *= Ctx.Profile.GetMultiplierFor(Candidate.Categories);
			if (FlavorCategory.HasValue && Candidate.Categories.Contains(FlavorCategory.Value))
				weight *= Tuning.flavorCategoryMultiplier;
			if (weight <= 0)
				continue;
			Pool.Add(Candidate);
			PoolWeights.Add(weight);
			totalWeight += weight;
		}
		if (totalWeight <= 0)
			return -1;
		double roll = Rng.NextDouble() * totalWeight;
		double cumulative = 0;
		for (int i = 0; i < Pool.Count; i++)
		{
			cumulative += PoolWeights[i];
			if (roll < cumulative)
				return Pool[i].index;
		}
		return Pool[Pool.Count - 1].index;
	}
	/// <summary>
	/// Suppresses repeats of an item already placed in the grid being
	/// planned: impossible for Scarce+, heavily discouraged for Limited,
	/// lightly discouraged for Common junk like Rock and Branch
	/// </summary>
	private float WithinGridMultiplier(int tier, int index, HashSet<int> PlacedThisGrid)
	{
		if (PlacedThisGrid == null || !PlacedThisGrid.Contains(index))
			return 1f;
		return tier switch
		{
			commonTier => Tuning.withinGridMultiplierCommon,
			limitedTier => Tuning.withinGridMultiplierLimited,
			_ => 0f,
		};
	}
	/// <summary>
	/// Suppresses items that generated recently in earlier grids so notable
	/// drops stay varied; Common and Limited items are exempt
	/// </summary>
	private float HistoryMultiplier(int tier, int index)
	{
		if (tier < scarceTier || !State.RecentItemHistory.Contains(index))
			return 1f;
		return tier == scarceTier ? Tuning.historyMultiplierScarce : Tuning.historyMultiplierRarePlus;
	}
	/// <summary>
	/// Advances run state for a generated item. Keyed off generation, never
	/// player actions, so pickups and drops cannot charge any meter
	/// </summary>
	private void RecordSpawn(int index, Context Ctx)
	{
		Candidate Candidate = Candidates.Find(Candidate => Candidate.index == index);
		if (Candidate == null)
			return;
		int tier = TierIndexOf(Candidate.Rarity);
		if (tier >= rareTier)
		{
			State.hasRarePlusSpawned = true;
			State.rarePityOffset = 0f;
		}
		else if (RareIsPossible(Ctx))
			State.rarePityOffset = Math.Min(Tuning.rarePityCap, State.rarePityOffset + Tuning.rarePityPerItem);
		if (tier == anomalousTier)
		{
			State.anomalousSpawnedThisRegion++;
			State.gridsSinceAnomalous = 0;
		}
		if (tier > commonTier)
			State.PushHistory(index, Tuning.historySize);
		if (Candidate.uniquePerRun)
			State.UniqueItemsSpawned.Add(index);
		if (Candidate.Categories.Contains(LootCategory.Fuel))
			State.fuelSpawnedThisRegion++;
	}
	/// <summary>
	/// Pity only accumulates where Rare drops are structurally possible, so
	/// tutorial-region items cannot pre-charge the first region's excitement
	/// </summary>
	private static bool RareIsPossible(Context Ctx) =>
		Ctx.RarityWeightsStart[rareTier] > 0 || Ctx.RarityWeightsEnd[rareTier] > 0;
	private static bool HasAnyCategory(Candidate Candidate, List<LootCategory> Categories)
	{
		foreach (LootCategory Category in Categories)
		{
			if (Candidate.Categories.Contains(Category))
				return true;
		}
		return false;
	}
	public static int TierIndexOf(Rarity Rarity) => Rarity.Tag switch
	{
		Rarity.Tags.Common => commonTier,
		Rarity.Tags.Limited => limitedTier,
		Rarity.Tags.Scarce => scarceTier,
		Rarity.Tags.Rare => rareTier,
		Rarity.Tags.Anomalous => anomalousTier,
		_ => commonTier,
	};
}
