using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] private GameObject[] ItemTemplates;
    [SerializeField] private List<Item> Items = new();
    [SerializeField] private int spawnItemCount;
    public void Initialize(GameObject[] Templates)
    {
        ItemTemplates = Templates;
    }
    public void GenerateItems()
    {
        spawnItemCount = Random.Range(5, 10);
        int cap = spawnItemCount * 2;
        while (cap > 0 && spawnItemCount > 0)
        {
            if (WeightedRarityGeneration.Generate<Item>())
            {
                spawnItemCount--;
            }
            cap--;
        }
    }
    public void SpawnItem(int index, Vector3 Position)
    {
        Item Item = Instantiate(ItemTemplates[index], Position, Quaternion.identity).GetComponent<Item>();
        Item.Info = new ItemInfo(index);
        Items.Add(Item);
    }
    public void SpawnItem(ItemInfo existingInfo, Vector3 Position)
    {
        int index = (int)existingInfo.Tag;
        Item Item = Instantiate(ItemTemplates[index], Position, Quaternion.identity).GetComponent<Item>();
        Item.Info = existingInfo;
        Items.Add(Item);
    }
    public bool HasItemAtPosition(Vector3 Position)
    {
        return GetItemAtPosition(Position) != null;
    }
    public Item GetItemAtPosition(Vector3 Position)
    {
        return Items.Find(Item => Item.transform.position == Position);
    }
    public void RemoveItemAtPosition(Item itemAtPosition)
    {
        Items.Remove(itemAtPosition);
    }
    public void DestroyItemAtPosition(Vector3 Position)
    {
        Item Item = Items.Find(Item => Item.transform.position == Position);
        Items.Remove(Item);
        Destroy(Item.gameObject);
    }
    public void DestroyAllItems()
    {
        Items.ForEach(Item => Destroy(Item.gameObject));
        Items.Clear();
    }
}