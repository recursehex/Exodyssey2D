using System.Collections.Generic;

public class Inventory
{
	public List<ItemInventory> itemList;
	public int InventorySize { get; private set;} = 2;
	public Inventory()
	{
		itemList = new List<ItemInventory>();
	}
	/// <summary>
	/// Returns true if Item was successfully added, returns false if inventory is full
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool AddItem(ItemInventory item)
	{
		if (itemList.Count >= InventorySize) return false;
		itemList.Add(item);
		return true;
	}
	/// <summary>
	/// Removes item from inventory at specified index
	/// </summary>
	/// <param name="index"></param>
	public void RemoveItem(int index)
	{
		itemList.RemoveAt(index);
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="player"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	public bool ProcessDamageAfterWeaponDrop(Player player, int index)
	{
		return itemList[index].itemInfo.ProcessDamageAfterWeaponDrop(player);
	}
	/// <summary>
	/// Decreases weapon durability after use
	/// </summary>
	/// <param name="index"></param>
	public void DecreaseWeaponDurability(int index, int change)
	{
		itemList[index].itemInfo.ChangeWeaponDurability(change);
	}
	/// <summary>
	/// Returns currently select weapon's range
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public int GetWeaponRange(int index)
	{
		return itemList[index].itemInfo.range;
	}
}
