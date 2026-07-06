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
	public LootState State { get; }
	public LootDirector(List<Candidate> Candidates, LootTuning Tuning, LootState State = null)
	{
		this.Candidates = Candidates;
		this.Tuning = Tuning;
		this.State = State ?? new LootState();
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
		int itemCount = Rng.Next(Ctx.Profile.MinItems, Ctx.Profile.MaxItems + 1);
		List<int> Plan = new();
		HashSet<int> PlacedThisGrid = new();
		if (!State.hasRarePlusSpawned && IsBackstopDue(Ctx))
		{
			int forcedIndex = PickFromTier(rareTier, Ctx, Rng, PlacedThisGrid, Candidate => Candidate.isWeapon);
			if (forcedIndex < 0)
				forcedIndex = PickFromTier(rareTier, Ctx, Rng, PlacedThisGrid);
			if (forcedIndex >= 0)
			{
				Plan.Add(forcedIndex);
				PlacedThisGrid.Add(forcedIndex);
				RecordSpawn(forcedIndex, Ctx);
				itemCount--;
			}
		}
		for (int i = 0; i < itemCount; i++)
		{
			int index = RollItem(Ctx, Rng, PlacedThisGrid);
			if (index >= 0)
			{
				Plan.Add(index);
				PlacedThisGrid.Add(index);
			}
		}
		return Plan;
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
			if (TierIndexOf(Candidate.Rarity) != tier)
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
	}
	/// <summary>
	/// Pity only accumulates where Rare drops are structurally possible, so
	/// tutorial-region items cannot pre-charge the first region's excitement
	/// </summary>
	private static bool RareIsPossible(Context Ctx) =>
		Ctx.RarityWeightsStart[rareTier] > 0 || Ctx.RarityWeightsEnd[rareTier] > 0;
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
