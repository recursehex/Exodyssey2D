using System.Collections.Generic;

public class Inventory
{
	private readonly List<Item> InventoryList;
	public int Size => InventoryList.Capacity;
	public int Count => InventoryList.Count;
	public Inventory(int size)
	{
		InventoryList = new(size);
	}
	public Item this[int index]
	{
		get => InventoryList[index];
	}
	/// <summary>
	/// Returns true if Item was successfully added, returns false if inventory is full
	/// </summary>
	public bool TryAddItem(Item Item)
	{
		if (InventoryList.Count >= Size)
		{
			return false;
		}
		InventoryList.Add(Item);
		return true;
	}
	/// <summary>
	/// Removes item from inventory at specified index
	/// </summary>
	public void RemoveItem(int index)
	{
		InventoryList.RemoveAt(index);
	}
}
