using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WeightedRarityGeneration: MonoBehaviour
{
    static public void Generation(Dictionary<Rarity, int> percentMap, List<Rarity> allRarities, int nStartElements, GameObject[] elementTemplates, List<GameObject> elements, Tilemap tilemapWalls,GameManager gm, bool isFromItemClass)
    {
        int sumPercent = 0;
        List<int> rarityPercentages = new List<int>();
        List<List<int>> ItemIndexDoubleList = new List<List<int>>();
        for (Rarity r = Rarity.Common; r < Rarity.Unknown; r++)
        {
            int nItemsOfRarity = 0;
            List<int> itemIndices = new List<int>();
            for (int i = 0; i < allRarities.Count; i++)
            {
                if (allRarities[i] == r)
                {
                    nItemsOfRarity++;
                    itemIndices.Add(i);
                }
            }
            if (nItemsOfRarity > 0)
            {
                int p = percentMap[r];
                rarityPercentages.Add(p);
                ItemIndexDoubleList.Add(itemIndices);
                sumPercent += p;
            }
        }

        for (int i = 0; i < nStartElements; i++)
        {
            int rSum = 0;
            int randomPercent = Random.Range(0, 100);
            int j;
            for (j = 0; j < rarityPercentages.Count; j++)
            {
                if (randomPercent <= ((rSum + rarityPercentages[j]) * 100) / sumPercent)
                {
                    break;
                }
                rSum += rarityPercentages[j];
            }

            int nItemsInGroup = ItemIndexDoubleList[j].Count;
            int randomItemInGroupIndex = Random.Range(0, nItemsInGroup);
            int randomItemIndex = ItemIndexDoubleList[j][randomItemInGroupIndex];

            GameObject element = elementTemplates[randomItemIndex];

            while (true)
            {
                int x = (Random.Range(-4, 4));
                int y = (Random.Range(-4, 4));
                Vector3Int p = new Vector3Int(x, y, 0);

                if (!tilemapWalls.HasTile(p) && !(x == -4 && y == 0))
                {
                    Vector3 shiftedDistance = new Vector3(x + 0.5f, y + 0.5f, 0);
                    if (!gm.HasElementAtPosition(shiftedDistance))
                    {
                        if (isFromItemClass)
                        {
                            GameObject instance = Instantiate(element, shiftedDistance, Quaternion.identity) as GameObject;
                            Item e = instance.GetComponent<Item>();
                            e.info = ItemInfo.FactoryFromNumber(randomItemIndex);
                            elements.Add(instance);
                        }
                        else
                        {
                            GameObject instance = Instantiate(element, shiftedDistance, Quaternion.identity) as GameObject;
                            Enemy e = instance.GetComponent<Enemy>();
                            e.info = EnemyInfo.FactoryFromNumber(randomItemIndex);
                            e.SetGameManager(gm);
                            e.ExposedStart();
                            elements.Add(instance);
                        }
                        break;
                    }
                }
            }
        }
    }
}
