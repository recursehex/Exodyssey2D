using System.Collections.Generic;

public class Inventory
{
	private readonly List<ItemInfo> InventoryList;
	public int Size => InventoryList.Count;
	public int Count
	{
		get
		{
			int count = 0;
			for (int i = 0; i < InventoryList.Count; i++)
			{
				if (InventoryList[i] != null)
					count++;
			}
			return count;
		}
	}
	public Inventory(int size)
	{
		InventoryList = new(size);
		for (int i = 0; i < size; i++)
			InventoryList.Add(null);
	}
	public ItemInfo this[int index]
	{
		get => InventoryList[index];
		set => InventoryList[index] = value;
	}
	/// <summary>
	/// Returns true if Item was successfully added, returns false if inventory is full
	/// </summary>
	public bool TryAddItem(ItemInfo ItemInfo)
	{
		return TryAddItem(ItemInfo, out _);
	}
	public bool TryAddItem(ItemInfo ItemInfo, out int addedIndex)
	{
		addedIndex = -1;
		for (int i = 0; i < InventoryList.Count; i++)
		{
			if (InventoryList[i] != null)
				continue;
			InventoryList[i] = ItemInfo;
			addedIndex = i;
			return true;
		}
		return false;
	}
	/// <summary>
	/// Removes item from inventory at specified index
	/// </summary>
	public void RemoveItem(int index)
	{
		if (index < 0 || index >= InventoryList.Count)
			return;
		InventoryList[index] = null;
	}
	public bool HasItemAt(int index)
	{
		if (index < 0 || index >= InventoryList.Count)
			return false;
		return InventoryList[index] != null;
	}
	/// <summary>
	/// Returns true if inventory is empty
	/// </summary>
	public bool IsEmpty => Count == 0;
}
