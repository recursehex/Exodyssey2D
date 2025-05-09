using UnityEngine;

public static class WeightedRarityGeneration
{
	private static Rarity ChosenRarity;
	private static Vector3 ChosenPosition;
	private static bool GenerateRarityAndPosition()
	{
		int roll = Random.Range(1, 101);
		int cumulative = 0;
		foreach (Rarity Rarity in Rarity.RarityList)
		{
			cumulative += Rarity.GetDropRate();
			if (roll <= cumulative)
			{
				int x = Random.Range(-4, 5);
				int y = Random.Range(-4, 5);
				Vector3Int Position = new(x, y, 0);
				// Fails if wall tile or Player is at selected position
				if (GameManager.Instance.HasWallAtPosition(Position)
				 || GameManager.Instance.HasExitTileAtPosition(Position)
				 || (x <= -2 && y <= 1 && y >= -1))
				{
					return false;
				}
				Vector3 ShiftedPosition = new(x + 0.5f, y + 0.5f, 0);
				// Fails if item, enemy, or vehicle is at selected position
				if (GameManager.Instance.HasItemAtPosition(ShiftedPosition)
				 || GameManager.Instance.HasEnemyAtPosition(ShiftedPosition)
				 || GameManager.Instance.HasVehicleAtPosition(ShiftedPosition))
				{
					return false;
				}
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
        if (!GenerateRarityAndPosition())
		{
            return false;
		}
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