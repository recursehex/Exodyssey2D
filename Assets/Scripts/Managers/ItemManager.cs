using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] private GameObject[] ItemTemplates;
    public List<Item> Items { get; private set; } = new();
    [SerializeField] private int spawnItemCount;
    [Header("Spawning")]
    [SerializeField] private int minSpawnCount = 3;
    [SerializeField] private int maxSpawnCountExclusive = 7;
    [SerializeField] private int spawnRetryMultiplier = 2;
    public void Initialize(GameObject[] Templates) => ItemTemplates = Templates;
    /// <summary>
    /// Generates random number of items for the level
    /// </summary>
    public void GenerateItems()
    {
        spawnItemCount = Random.Range(minSpawnCount, maxSpawnCountExclusive);
        int cap = spawnItemCount * spawnRetryMultiplier;
        while (cap > 0 && spawnItemCount > 0)
        {
            if (WeightedRarityGeneration.Generate<Item>())
                spawnItemCount--;
            cap--;
        }
    }
    /// <summary>
    /// Spawns an item at a given position from an index in ItemTemplates (e.g. item generation)
    /// </summary>
    public Item SpawnItem(int index, Vector3 Position)
    {
        Item Item = Instantiate(ItemTemplates[index], Position, Quaternion.identity).GetComponent<Item>();
        Item.Info = new(index);
        Items.Add(Item);
        return Item;
    }
    /// <summary>
    /// Spawns an item at a given position from an existing ItemInfo (e.g. Player drops an item)
    /// summary>
    public Item SpawnItem(ItemInfo Info, Vector3 Position)
    {
        int index = (int)Info.Tag;
        Item Item = Instantiate(ItemTemplates[index], Position, Quaternion.identity).GetComponent<Item>();
        Item.Info = Info;
        Items.Add(Item);
        return Item;
    }
    /// <summary>
    /// Returns true if an item is at the given position
    /// </summary>
    public bool HasItemAtPosition(Vector3 Position) => GetItemAtPosition(Position) != null;
    /// <summary>
    /// Returns the item at the given position, or null if no item exists
    /// summary>
    public Item GetItemAtPosition(Vector3 Position) => Items.Find(Item => Item.transform.position == Position);
    /// <summary>
    /// Removes an item at the given position from Items list
    /// </summary>
    public void RemoveItemAtPosition(Item ItemAtPosition) => Items.Remove(ItemAtPosition);
    /// <summary>
    /// Destroys an item at the given position
    /// </summary>
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
