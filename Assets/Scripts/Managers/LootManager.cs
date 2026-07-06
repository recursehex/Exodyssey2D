using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene adapter for the loot director: owns the run's LootState and seeded
/// RNG stream, assembles roll contexts from the current region, and plans a
/// grid's items once at generation time so nothing can reroll them later
/// </summary>
public partial class LootManager : MonoBehaviour
{
	private LootDirector Director;
	private RegionManager RegionManager;
	public void Initialize(RegionManager RegionManager)
	{
		this.RegionManager = RegionManager;
		Director = new LootDirector(BuildCandidates(), LootProfileInfo.Tuning);
		Director.State.ResetForNewRun(NewRunSeed(), LootProfileInfo.Tuning.rarePityStart);
		RegionManager.OnRegionChanged += HandleRegionChanged;
	}
	private void OnDestroy()
	{
		if (RegionManager != null)
			RegionManager.OnRegionChanged -= HandleRegionChanged;
	}
	private void HandleRegionChanged(RegionInfo NewRegion) => Director.State.ResetForNewRegion();
	/// <summary>
	/// Resets all run-scoped loot state and reseeds the RNG stream
	/// </summary>
	public void ResetForNewRun() =>
		Director.State.ResetForNewRun(NewRunSeed(), LootProfileInfo.Tuning.rarePityStart);
	private static int NewRunSeed() => Random.Range(int.MinValue, int.MaxValue);
	/// <summary>
	/// Plans this grid's ground-scatter items. Rolls come from a dedicated
	/// System.Random seeded per grid (runSeed XOR grid number), never from
	/// UnityEngine.Random, so map generation and combat cannot steer loot
	/// </summary>
	public List<int> PlanGridLoot()
	{
		System.Random Rng = new(Director.State.runSeed ^ Director.State.globalGridNumber);
		return Director.PlanGridLoot(BuildContext(), Rng);
	}
	private LootDirector.Context BuildContext()
	{
		RegionInfo Region = RegionManager.CurrentRegion;
		return new LootDirector.Context
		{
			regionIndex = (int)Region.Tag,
			gridsCompleted = Region.GridsCompleted,
			gridsRequired = Region.GridsRequired,
			RarityWeightsStart = Region.ItemRarityWeightsStart,
			RarityWeightsEnd = Region.ItemRarityWeightsEnd,
			anomalousCap = Region.AnomalousCap,
			Profile = LootProfileInfo.GetProfile(LootProfileInfo.DefaultProfileName),
		};
	}
	/// <summary>
	/// Builds the director's candidate list from every enabled item
	/// </summary>
	private static List<LootDirector.Candidate> BuildCandidates()
	{
		List<LootDirector.Candidate> Candidates = new();
		foreach (int index in ItemInfo.GetEnabledItemIndices())
		{
			ItemInfo Info = new(index);
			Candidates.Add(new LootDirector.Candidate
			{
				index = index,
				Rarity = Info.Rarity,
				Categories = new List<LootCategory>(Info.Categories),
				lootWeight = Info.LootWeight,
				uniquePerRun = Info.UniquePerRun,
				minRegionIndex = (int)Info.MinRegion,
				isWeapon = Info.Type == ItemInfo.Types.Weapon,
			});
		}
		return Candidates;
	}
}
