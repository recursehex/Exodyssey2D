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
    /// <summary>
    /// Generates random number of items for the level
    /// </summary>
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
    /// <summary>
    /// Spawns an item at a given position from an index in ItemTemplates (e.g. item generation)
    /// </summary>
    /// <param name="index"></param>
    /// <param name="Position"></param>
    public void SpawnItem(int index, Vector3 Position)
    {
        Item Item = Instantiate(ItemTemplates[index], Position, Quaternion.identity).GetComponent<Item>();
        Item.Info = new ItemInfo(index);
        Items.Add(Item);
    }
    /// <summary>
    /// Spawns an item at a given position from an existing ItemInfo (e.g. Player drops an item)
    /// summary>
    public void SpawnItem(ItemInfo Info, Vector3 Position)
    {
        int index = (int)Info.Tag;
        Item Item = Instantiate(ItemTemplates[index], Position, Quaternion.identity).GetComponent<Item>();
        Item.Info = Info;
        Items.Add(Item);
    }
    /// <summary>
    /// Returns true if an item is at the given position
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
    public bool HasItemAtPosition(Vector3 Position) => GetItemAtPosition(Position) != null;
    /// <summary>
    /// Returns the item at the given position, or null if no item exists
    /// summary>
    /// param name="Position"></param>
    /// <returns></returns>
    public Item GetItemAtPosition(Vector3 Position) => Items.Find(Item => Item.transform.position == Position);
    /// <summary>
    /// Removes an item at the given position from Items list
    /// </summary>
    /// <param name="ItemAtPosition"></param>
    public void RemoveItemAtPosition(Item ItemAtPosition) => Items.Remove(ItemAtPosition);
    /// <summary>
    /// Destroys an item at the given position
    /// </summary>
    /// <param name="Position"></param>
    public void DestroyItemAtPosition(Vector3 Position)
    {
        Item Item = Items.Find(Item => Item.transform.position == Position);
        Items.Remove(Item);
        Destroy(Item.gameObject);
    }
    /// <summary>
    /// Destroys all items in the scene
    /// </summary>
    public void DestroyAllItems()
    {
        Items.ForEach(Item => Destroy(Item.gameObject));
        Items.Clear();
    }
}