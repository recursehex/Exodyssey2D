using UnityEngine;

public class WeightedRarityGeneration : MonoBehaviour
{
	public static bool Generate(string elementType)
	{
		int roll = Random.Range(1, 101);
		int cumulative = 0;
		foreach (Rarity rarity in Rarity.rarities)
		{
			cumulative += rarity.Chance;
			if (roll <= cumulative)
			{
				int x = Random.Range(-4, 4);
				int y = Random.Range(-4, 4);
				Vector3Int position = new(x, y, 0);
				if (GameManager.instance.TilemapWallsHasTile(position) || (x <= -2 && y <= 1 && y >= -1)) return false;
				Vector3 shiftedPosition = new(x + 0.5f, y + 0.5f, 0);
				if (GameManager.instance.HasItemAtPosition(shiftedPosition) || GameManager.instance.HasEnemyAtPosition(shiftedPosition)) return false;
				switch (elementType) 
				{
					case "Item":
						return GenerateItem(rarity, shiftedPosition);
					case "Enemy":
						return GenerateEnemy(rarity, shiftedPosition);
					case "Vehicle":
						break;
					default:
						break;
				}
			}
		}
		return false;
	}
	private static bool GenerateItem(Rarity rarity, Vector3 shiftedPosition) 
	{
		int randomItemIndex = ItemInfo.GetRandomIndexOfSpecifiedRarity(rarity.Tag);
		if (randomItemIndex == -1) return false;
		GameObject element = GameManager.instance.itemTemplates[randomItemIndex];
		GameObject instance = Instantiate(element, shiftedPosition, Quaternion.identity);
		Item item = instance.GetComponent<Item>();
		item.info = ItemInfo.ItemFactory(randomItemIndex);
		GameManager.instance.items.Add(instance);
		return true;
	}
	private static bool GenerateEnemy(Rarity rarity, Vector3 shiftedPosition) 
	{
		int randomEnemyIndex = EnemyInfo.GetRandomIndexOfSpecifiedRarity(rarity.Tag);
		if (randomEnemyIndex == -1) return false;
		GameObject element = GameManager.instance.enemyTemplates[randomEnemyIndex];
		GameObject instance = Instantiate(element, shiftedPosition, Quaternion.identity);
		Enemy enemy = instance.GetComponent<Enemy>();
		enemy.tilemapGround = GameManager.instance.GetTilemapGround();
		enemy.tilemapWalls = GameManager.instance.GetTilemapWalls();
		enemy.info = EnemyInfo.EnemyFactory(randomEnemyIndex);
		enemy.SetGameManager(GameManager.instance);
		enemy.ExposedStart();
		GameManager.instance.enemies.Add(instance);
		return true;
	}
}
