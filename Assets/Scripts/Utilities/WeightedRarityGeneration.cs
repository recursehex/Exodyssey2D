using System.Collections.Generic;
using UnityEngine;

public static class WeightedRarityGeneration
{
	/// <summary>
	/// Rolls a weighted-random rarity for the given entity type based on the
	/// current region's allowed rarities. Returns false if none are available.
	/// </summary>
	private static bool TryRollRarity<T>(out Rarity Chosen)
	{
		Chosen = default;
		// Get allowed rarities based on entity type
		List<Rarity> AllowedRarities = typeof(T).Name switch
		{
			nameof(Item) => ItemInfo.GetAllowedRarities(),
			nameof(Enemy) => EnemyInfo.GetAllowedRarities(),
			nameof(Vehicle) => VehicleInfo.GetAllowedRarities(),
			_ => Rarity.RarityList
		};
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
	/// Spawns a single item, enemy, or vehicle of a random rarity/type at the
	/// given (already-empty) shifted world position. Returns true if spawned.
	/// </summary>
	private static bool GenerateAt<T>(Vector3 Position)
	{
		if (!TryRollRarity<T>(out Rarity ChosenRarity))
			return false;
		int index = -1;
		switch (typeof(T).Name)
		{
			case nameof(Item):
				index = ItemInfo.GetRandomIndexFrom(ChosenRarity);
				if (index != -1)
					GameManager.Instance.SpawnItem(index, Position);
				break;
			case nameof(Enemy):
				index = EnemyInfo.GetRandomIndexFrom(ChosenRarity);
				if (index != -1)
					GameManager.Instance.SpawnEnemy(index, Position);
				break;
			case nameof(Vehicle):
				index = VehicleInfo.GetRandomIndexFrom(ChosenRarity);
				if (index != -1)
				{
					VehicleInfo VehicleInfo = new(index);
					int maxStartingFuel = VehicleInfo.CurrentCharge;
					int startingFuel = Random.Range(0, maxStartingFuel + 1);
					GameManager.Instance.SpawnVehicle(index, Position, startingFuel);
				}
				break;
			default:
				Debug.LogError($"WeightedRarityGeneration.GenerateAt<T>() " +
								$"does not support type {typeof(T)}");
				break;
		}
		return index != -1;
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
		// Shuffle empty cells so distinct tiles are drawn without positional bias
		for (int i = 0; i < EmptyCells.Count; i++)
		{
			int swapIndex = Random.Range(i, EmptyCells.Count);
			(EmptyCells[i], EmptyCells[swapIndex]) = (EmptyCells[swapIndex], EmptyCells[i]);
		}
		int spawned = 0;
		int cellIndex = 0;
		// Each empty cell is used at most once; advance past cells where the
		// rarity/type roll fails so a rare miss does not consume the target
		while (spawned < toSpawn && cellIndex < EmptyCells.Count)
		{
			Vector3 Position = EmptyCells[cellIndex] + new Vector3(0.5f, 0.5f);
			if (GenerateAt<T>(Position))
				spawned++;
			cellIndex++;
		}
		return spawned;
	}
}
