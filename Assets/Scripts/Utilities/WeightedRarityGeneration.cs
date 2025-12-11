using System.Collections.Generic;
using UnityEngine;

public static class WeightedRarityGeneration
{
	private static Rarity ChosenRarity;
	private static Vector3 ChosenPosition;
	private static bool GenerateRarityAndPosition<T>()
	{
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
				int x = Random.Range(GameConfig.Grid.MinX, GameConfig.Grid.MaxX + 1);
				int y = Random.Range(GameConfig.Grid.MinY, GameConfig.Grid.MaxY + 1);
				Vector3Int Position = new(x, y);
				// Fails if wall tile or Player is at selected position
				if (GameManager.Instance.HasWallAtPosition(Position)
					|| GameManager.Instance.HasFireAtPosition(Position)
					|| GameManager.Instance.HasExitTileAtPosition(Position)
					|| (x <= GameConfig.Grid.SafeZoneMaxX
						&& y <= GameConfig.Grid.SafeZoneMaxY
						&& y >= GameConfig.Grid.SafeZoneMinY))
					return false;
				Vector3 ShiftedPosition = Position + new Vector3(0.5f, 0.5f);
				// Fails if item, enemy, or vehicle is at selected position
				if (GameManager.Instance.HasItemAtPosition(ShiftedPosition)
				 || GameManager.Instance.HasEnemyAtPosition(ShiftedPosition)
				 || GameManager.Instance.HasVehicleAtPosition(ShiftedPosition))
					return false;
				ChosenRarity = Rarity;
				ChosenPosition = ShiftedPosition;
				return true;
			}
		}
		return false;
	}
	/// <summary>
	/// Generates an item, enemy, or vehicle of a random rarity and position
	/// </summary>
	public static bool Generate<T>()
    {
        if (!GenerateRarityAndPosition<T>())
            return false;
        int index = -1;
        switch (typeof(T).Name)
        {
            case nameof(Item):
                index = ItemInfo.GetRandomIndexFrom(ChosenRarity);
                if (index != -1)
                    GameManager.Instance.SpawnItem(index, ChosenPosition);
                break;
            case nameof(Enemy):
                index = EnemyInfo.GetRandomIndexFrom(ChosenRarity);
                if (index != -1)
                    GameManager.Instance.SpawnEnemy(index, ChosenPosition);
                break;
            case nameof(Vehicle):
                index = VehicleInfo.GetRandomIndexFrom(ChosenRarity);
                if (index != -1)
                    GameManager.Instance.SpawnVehicle(index, ChosenPosition);
                break;
            default:
				Debug.LogError($"WeightedRarityGeneration.Generate<T>() " +
								$"does not support type {typeof(T)}");
                break;
        }
        return index != -1;
    }
}
