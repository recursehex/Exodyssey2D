using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Organizes items in the player's inventory
/// </summary>
public class InventoryUI : MonoBehaviour
{
	private Inventory inventory;
	private int selectedIndex = -1;
	private string cachedName;
	private string cachedDesc;
	public Sprite itemBackground;
	public void SetInventory(Inventory inventory)
	{
		this.inventory = inventory;
		RefreshInventoryItems();
	}
	/// <summary>
	/// Removes item from inventory UI
	/// </summary>
	/// <param name="index"></param>
	public void RemoveItem(int index)
	{
		if (selectedIndex != -1) 
		{
			if (selectedIndex == index)
			{
				selectedIndex = -1;
				GameObject.Find("InventoryPressed0").transform.localScale = Vector3.one;
				GameObject.Find("InventoryPressed1").transform.localScale = Vector3.one;
			}
			else if (index == 0)
			{
				selectedIndex = 0;
				GameObject.Find("InventoryPressed1").transform.localScale = Vector3.one;
				GameObject.Find("InventoryPressed0").transform.localScale = Vector3.zero;
			}
		}
		inventory.RemoveItem(index);
		RefreshInventoryItems();
	}
	/// <summary>
	/// Refreshes items in inventory UI to match changed items
	/// </summary>
	public void RefreshInventoryItems()
	{
		// Cleanup of icons
		for (int i = 0; i < inventory.InventorySize; i++)
		{
			Image icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();
			icon.sprite = itemBackground;
		}
		int iconNumber = 0;
		// Add item icon
		foreach (ItemInventory item in inventory.itemList)//GetItemList())
		{
			if (iconNumber < inventory.InventorySize)
			{
				Image icon = GameObject.Find("InventoryIcon" + iconNumber).GetComponent<Image>();
				icon.sprite = item.GetSprite();
			}
			iconNumber++;
		}
	}
	public bool ProcessDamageAfterWeaponDrop(Player p, int itemIndex)
	{
		if (selectedIndex != itemIndex) return false;
		return inventory.ProcessDamageAfterWeaponDrop(p, selectedIndex);
	}
	/// <summary>
	/// Decreases weapon durability after use if one is selected
	/// </summary>
	public void ChangeWeaponDurability(int change)
	{
		if (selectedIndex == -1) return;
		inventory.DecreaseWeaponDurability(selectedIndex, change);
		SetCurrentSelected(selectedIndex);
	}
	public int GetCurrentSelected()
	{
		return selectedIndex;
	}
	public void SetCurrentSelected(int itemPosition)
	{
		selectedIndex = itemPosition;
		if (itemPosition >= 0)
		{
			cachedName = inventory.itemList[selectedIndex].itemInfo.name;
			cachedDesc = inventory.itemList[selectedIndex].itemInfo.description + inventory.itemList[selectedIndex].itemInfo.stats;
		}
		else
		{
			cachedName = "";
			cachedDesc = "";
		}
		GameObject itemName = GameObject.Find("ItemName");
		Text nameText = itemName.GetComponent<Text>();
		GameObject itemDesc = GameObject.Find("ItemDescription");
		Text descText = itemDesc.GetComponent<Text>();
		nameText.text = cachedName;
		descText.text = cachedDesc;

	}
	/// <summary>
	/// Shows name and desc of an item when hovering over it
	/// </summary>
	/// <param name="mousePosition"></param>
	public void ProcessHoverForInventory(Vector3 mousePosition)
	{
		int iconNumber = 0;
		float sensitivityDistance = 0.5f;
		bool mouseIsOverIcon = false;
		GameObject itemName = GameObject.Find("ItemName");
		Text nameText = itemName.GetComponent<Text>();
		GameObject itemDesc = GameObject.Find("ItemDescription");
		Text descText = itemDesc.GetComponent<Text>();
		foreach (ItemInventory item in inventory.itemList)
		{
			Image icon = GameObject.Find("InventoryIcon" + iconNumber).GetComponent<Image>();
			Vector3 iconPosition = icon.transform.position;
			if (Math.Abs(iconPosition.x - mousePosition.x) <= sensitivityDistance && Math.Abs(iconPosition.y - mousePosition.y) <= sensitivityDistance)
			{
				mouseIsOverIcon = true;
				nameText.text = item.itemInfo.name;
				descText.text = item.itemInfo.description + item.itemInfo.stats;
				break;
			}
			iconNumber++;
		}
		if (mouseIsOverIcon) return;
		if (selectedIndex == -1)
		{
			nameText.text = "";
			descText.text = "";
		}
		else
		{
			nameText.text = cachedName;
			descText.text = cachedDesc;
		}
	}
}
