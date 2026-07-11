using System.Collections.Generic;
using UnityEngine;

public static class WeightedRarityGeneration
{
	/// <summary>
	/// Rolls a weighted-random rarity for the given entity type based on the
	/// current region's allowed rarities. Returns false if none are available.
	/// Items are planned by LootManager instead and never roll here.
	/// </summary>
	private static bool TryRollRarity<T>(out Rarity Chosen)
	{
		Chosen = default;
		// Get allowed rarities based on entity type; type-object comparison avoids
		// the reflection metadata lookup typeof(T).Name performs per roll
		List<Rarity> AllowedRarities;
		if (typeof(T) == typeof(Enemy))
			AllowedRarities = EnemyInfo.GetAllowedRarities();
		else if (typeof(T) == typeof(Vehicle))
			AllowedRarities = VehicleInfo.GetAllowedRarities();
		else
			AllowedRarities = Rarity.RarityList;
		// If no allowed rarities, fail
		if (AllowedRarities.Count == 0)
			return false;
		int totalWeight = 0;
		foreach (Rarity Rarity in AllowedRarities)
			totalWeight += Rarity.GetDropRate();
		if (totalWeight <= 0)
			return false;
		int roll = Random.Range(1, totalWeight + 1);
		int cumulative = 0;
		// Select rarity based on weighted drop rates
		foreach (Rarity Rarity in AllowedRarities)
		{
			cumulative += Rarity.GetDropRate();
			if (roll <= cumulative)
			{
				Chosen = Rarity;
				return true;
			}
		}
		return false;
	}
	/// <summary>
	/// Spawns a single enemy or vehicle of a random rarity/type at the
	/// given (already-empty) shifted world position. Returns true if spawned.
	/// </summary>
	private static bool GenerateAt<T>(Vector3 Position)
	{
		if (!TryRollRarity<T>(out Rarity ChosenRarity))
			return false;
		int index = -1;
		if (typeof(T) == typeof(Enemy))
		{
			index = EnemyInfo.GetRandomIndexFrom(ChosenRarity);
			if (index != -1)
				GameManager.Instance.SpawnEnemy(index, Position);
		}
		else if (typeof(T) == typeof(Vehicle))
		{
			index = VehicleInfo.GetRandomIndexFrom(ChosenRarity);
			if (index != -1)
			{
				VehicleInfo VehicleInfo = new(index);
				int maxStartingFuel = VehicleInfo.CurrentCharge;
				int startingFuel = Random.Range(0, maxStartingFuel + 1);
				GameManager.Instance.SpawnVehicle(index, Position, startingFuel);
			}
		}
		else
			Debug.LogError($"WeightedRarityGeneration.GenerateAt<T>() " +
							$"does not support type {typeof(T)}");
		return index != -1;
	}
	/// <summary>
	/// Shuffles empty cells in place so distinct tiles are drawn without
	/// positional bias
	/// </summary>
	private static void ShuffleCells(List<Vector3Int> Cells)
	{
		for (int i = 0; i < Cells.Count; i++)
		{
			int swapIndex = Random.Range(i, Cells.Count);
			(Cells[i], Cells[swapIndex]) = (Cells[swapIndex], Cells[i]);
		}
	}
	/// <summary>
	/// Spawns up to <paramref name="targetCount"/> entities of type T into the
	/// grid's currently-empty tiles. Because placement draws from the known list
	/// of empty tiles, at least <paramref name="targetCount"/> (which is always
	/// >= the minimum) is guaranteed whenever enough empty tiles exist. When
	/// there are too few empty tiles to meet <paramref name="guaranteedMin"/>,
	/// the minimum simply fails: a warning is logged and only what fits spawns.
	/// Returns the number actually spawned.
	/// </summary>
	public static int GenerateBatch<T>(int guaranteedMin, int targetCount)
	{
		List<Vector3Int> EmptyCells = GameManager.Instance.GetEmptyCells();
		if (EmptyCells.Count < guaranteedMin)
			Debug.LogWarning($"Only {EmptyCells.Count} empty tiles available, cannot guarantee " +
							$"minimum {guaranteedMin} {typeof(T).Name} spawns this level");
		int toSpawn = Mathf.Min(targetCount, EmptyCells.Count);
		ShuffleCells(EmptyCells);
		int spawned = 0;
		int cellIndex = 0;
		// Each empty cell is used at most once; advance past cells where the
		// rarity/type roll fails so a rare miss does not consume the target
		while (spawned < toSpawn && cellIndex < EmptyCells.Count)
		{
			Vector3 Position = GridCoordinates.GetCellCenter(EmptyCells[cellIndex]);
			if (GenerateAt<T>(Position))
				spawned++;
			cellIndex++;
		}
		return spawned;
	}
	/// <summary>
	/// Spawns items already planned by the loot director into the grid's
	/// currently-empty tiles, one item per tile. Only placement is random
	/// here; what spawns was decided at grid generation. Returns the number
	/// actually spawned.
	/// </summary>
	public static int SpawnPlannedItems(IReadOnlyList<int> PlannedIndices)
	{
		List<Vector3Int> EmptyCells = GameManager.Instance.GetEmptyCells();
		if (EmptyCells.Count < PlannedIndices.Count)
			Debug.LogWarning($"Only {EmptyCells.Count} empty tiles available for " +
							$"{PlannedIndices.Count} planned items this level");
		ShuffleCells(EmptyCells);
		int toSpawn = Mathf.Min(PlannedIndices.Count, EmptyCells.Count);
		for (int i = 0; i < toSpawn; i++)
		{
			Vector3 Position = GridCoordinates.GetCellCenter(EmptyCells[i]);
			GameManager.Instance.SpawnItem(PlannedIndices[i], Position);
		}
		return toSpawn;
	}
}
