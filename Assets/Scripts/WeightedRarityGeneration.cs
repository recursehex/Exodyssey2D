using UnityEngine;
using Exodyssey.Rarity;

public static class WeightedRarityGeneration
{
	private static Rarity chosenRarity;
	private static Vector3 chosenShiftedPosition;
	private static bool GenerateRarityAndPosition()
	{
		int roll = Random.Range(1, 101);
		int cumulative = 0;
		
		foreach (Rarity rarity in System.Enum.GetValues(typeof(Rarity)))
		{
			cumulative += (int)rarity;
			
			if (roll <= cumulative)
			{
				int x = Random.Range(-4, 4);
				int y = Random.Range(-4, 4);
				Vector3Int position = new(x, y, 0);
				
				// Fails if wall tile is at selected position
				if (GameManager.instance.TilemapWallsHasTile(position)
					|| (x <= -2 && y <= 1 && y >= -1))
				{
					return false;
				}
				
				Vector3 shiftedPosition = new(x + 0.5f, y + 0.5f, 0);
				
				// Fails if item or enemy is at selected position
				if (GameManager.instance.HasItemAtPosition(shiftedPosition)
					|| GameManager.instance.HasEnemyAtPosition(shiftedPosition))
				{
					return false;
				}
				
				chosenRarity = rarity;
				chosenShiftedPosition = shiftedPosition;
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
		
		int randomItemIndex = ItemInfo.GetRandomIndexOfSpecifiedRarity(chosenRarity);
		
		// Fails if no items of chosen rarity
		if (randomItemIndex == -1)
		{
			return false;
		}
		
		GameObject element = GameManager.instance.itemTemplates[randomItemIndex];
		GameObject instance = GameObject.Instantiate(element, chosenShiftedPosition, Quaternion.identity);
		Item item = instance.GetComponent<Item>();
		item.info = ItemInfo.ItemFactory(randomItemIndex);
		GameManager.instance.items.Add(instance);
		return true;
	}
	public static bool GenerateEnemy()
	{
		if (!GenerateRarityAndPosition()) 
		{
			return false;
		}
		
		int randomEnemyIndex = EnemyInfo.GetRandomIndexOfSpecifiedRarity(chosenRarity);
		
		// Fails if no enemies of chosen rarity
		if (randomEnemyIndex == -1)
		{
			return false;
		}
		
		GameObject element = GameManager.instance.enemyTemplates[randomEnemyIndex];
		GameObject instance = GameObject.Instantiate(element, chosenShiftedPosition, Quaternion.identity);
		Enemy enemy = instance.GetComponent<Enemy>();
		enemy.tilemapGround = GameManager.instance.GetTilemapGround();
		enemy.tilemapWalls = GameManager.instance.GetTilemapWalls();
		enemy.info = EnemyInfo.EnemyFactory(randomEnemyIndex);
		enemy.SetGameManager(GameManager.instance);
		enemy.SetSoundManager(SoundManager.instance);
		GameManager.instance.enemies.Add(instance);
		return true;
	}
}
