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
				int x = Random.Range(-4, 4);
				int y = Random.Range(-4, 4);
				Vector3Int Position = new(x, y, 0);
				// Fails if wall tile is at selected position
				if (GameManager.Instance.HasWallAtPosition(Position)
					|| (x <= -2 && y <= 1 && y >= -1))
				{
					return false;
				}
				Vector3 ShiftedPosition = new(x + 0.5f, y + 0.5f, 0);
				// Fails if item or enemy is at selected position
				if (GameManager.Instance.HasItemAtPosition(ShiftedPosition)
				 || GameManager.Instance.HasEnemyAtPosition(ShiftedPosition))
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
	public static bool GenerateItem()
	{
		if (!GenerateRarityAndPosition()) 
		{
			return false;
		}
		int randomItemIndex = ItemInfo.GetRandomIndexFrom(ChosenRarity);
		// Fails if no items of chosen rarity
		if (randomItemIndex == -1)
		{
			return false;
		}
		GameManager.Instance.SpawnItem(randomItemIndex, ChosenPosition);
		return true;
	}
	public static bool GenerateEnemy()
	{
		if (!GenerateRarityAndPosition()) 
		{
			return false;
		}
		int randomEnemyIndex = EnemyInfo.GetRandomIndexFrom(ChosenRarity);
		// Fails if no enemies of chosen rarity
		if (randomEnemyIndex == -1)
		{
			return false;
		}
		GameManager.Instance.SpawnEnemy(randomEnemyIndex, ChosenPosition);
		return true;
	}
	public static bool GenerateVehicle()
	{
		if (!GenerateRarityAndPosition()) 
		{
			return false;
		}
		int randomVehicleIndex = VehicleInfo.GetRandomIndexFrom(ChosenRarity);
		// Fails if no vehicles of chosen rarity
		if (randomVehicleIndex == -1)
		{
			return false;
		}
		GameManager.Instance.SpawnVehicle(randomVehicleIndex, ChosenPosition);
		return true;
	}
}