using UnityEngine;
using Exodyssey.Rarity;

public static class WeightedRarityGeneration
{
	private static Rarity ChosenRarity;
	private static Vector3 ChosenPosition;
	private static bool GenerateRarityAndPosition()
	{
		int roll = Random.Range(1, 101);
		int cumulative = 0;
		foreach (Rarity Rarity in System.Enum.GetValues(typeof(Rarity)))
		{
			cumulative += (int)Rarity;
			if (roll <= cumulative)
			{
				int x = Random.Range(-4, 4);
				int y = Random.Range(-4, 4);
				Vector3Int Position = new(x, y, 0);
				// Fails if wall tile is at selected position
				if (GameManager.Instance.TilemapWalls.HasTile(Position)
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
		int randomItemIndex = ItemInfo.GetRandomIndexOfSpecifiedRarity(ChosenRarity);
		// Fails if no items of chosen rarity
		if (randomItemIndex == -1)
		{
			return false;
		}
		GameObject Element = GameManager.Instance.ItemTemplates[randomItemIndex];
		Item Item = GameObject.Instantiate(Element, ChosenPosition, Quaternion.identity).GetComponent<Item>();
		Item.Info = ItemInfo.ItemFactory(randomItemIndex);
		GameManager.Instance.Items.Add(Item);
		return true;
	}
	public static bool GenerateEnemy()
	{
		if (!GenerateRarityAndPosition()) 
		{
			return false;
		}
		int randomEnemyIndex = EnemyInfo.GetRandomIndexOfSpecifiedRarity(ChosenRarity);
		// Fails if no enemies of chosen rarity
		if (randomEnemyIndex == -1)
		{
			return false;
		}
		GameObject Element = GameManager.Instance.EnemyTemplates[randomEnemyIndex];
		Enemy Enemy = GameObject.Instantiate(Element, ChosenPosition, Quaternion.identity).GetComponent<Enemy>();
		Enemy.Info = EnemyInfo.EnemyFactory(randomEnemyIndex);
		GameManager.Instance.Enemies.Add(Enemy);
		return true;
	}
}