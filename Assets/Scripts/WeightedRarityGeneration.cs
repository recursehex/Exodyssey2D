using UnityEngine;
using Exodyssey.Rarity;

public static class WeightedRarityGeneration
{
	private static Rarity ChosenRarity;
	private static Vector3 ChosenShiftedPosition;
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
				if (GameManager.Instance.TilemapWallsHasTile(Position)
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
				ChosenShiftedPosition = ShiftedPosition;
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
		GameObject ItemInstance = GameObject.Instantiate(Element, ChosenShiftedPosition, Quaternion.identity);
		Item Item = ItemInstance.GetComponent<Item>();
		Item.Info = ItemInfo.ItemFactory(randomItemIndex);
		GameManager.Instance.Items.Add(ItemInstance);
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
		GameObject EnemyInstance = GameObject.Instantiate(Element, ChosenShiftedPosition, Quaternion.identity);
		Enemy Enemy = EnemyInstance.GetComponent<Enemy>();
		Enemy.TilemapGround = GameManager.Instance.GetTilemapGround();
		Enemy.TilemapWalls = GameManager.Instance.GetTilemapWalls();
		Enemy.Info = EnemyInfo.EnemyFactory(randomEnemyIndex);
		Enemy.SetGameManager(GameManager.Instance);
		Enemy.SetSoundManager(SoundManager.Instance);
		GameManager.Instance.Enemies.Add(EnemyInstance);
		return true;
	}
}
