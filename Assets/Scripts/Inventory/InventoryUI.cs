using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Organizes items in the player's inventory
/// </summary>
public class InventoryUI : MonoBehaviour
{
	private Inventory Inventory;
	public int SelectedIndex { get; private set; } = -1;
	private string cachedName;
	private string cachedDesc;
	public Sprite ItemBackground;
	public void SetInventory(Inventory Inventory)
	{
		this.Inventory = Inventory;
		RefreshInventoryIcons();
	}
	/// <summary>
	/// Removes item from inventory UI
	/// </summary>
	public void RemoveItem(int index)
	{
		if (SelectedIndex != -1) 
		{
			// Resets icons and inventory text
			if (SelectedIndex == index)
			{
				SelectedIndex = -1;
				GameObject.Find("InventoryPressed0").transform.localScale = Vector3.one;
				GameObject.Find("InventoryPressed1").transform.localScale = Vector3.one;
				GameObject ItemName = GameObject.Find("ItemName");
				Text NameText = ItemName.GetComponent<Text>();
				GameObject ItemDesc = GameObject.Find("ItemDescription");
				Text DescText = ItemDesc.GetComponent<Text>();
				NameText.text = "";
				DescText.text = "";
			}
			else if (index == 0)
			{
				SelectedIndex = 0;
				GameObject.Find("InventoryPressed1").transform.localScale = Vector3.one;
				GameObject.Find("InventoryPressed0").transform.localScale = Vector3.zero;
			}
		}
		Inventory.RemoveItem(index);
		RefreshInventoryIcons();
	}
	/// <summary>
	/// Refreshes items in inventory UI to match changed items
	/// </summary>
	public void RefreshInventoryIcons()
	{
		// Cleanup of icons
		for (int i = 0; i < Inventory.InventorySize; i++)
		{
			Image Icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();
			Icon.sprite = ItemBackground;
		}
		int iconNumber = 0;
		// Add item icon
		foreach (ItemInventory Item in Inventory.InventoryList)
		{
			if (iconNumber < Inventory.InventorySize)
			{
				Image icon = GameObject.Find("InventoryIcon" + iconNumber).GetComponent<Image>();
				icon.sprite = Item.GetSprite();
			}
			iconNumber++;
		}
	}
	/// <summary>
	/// Sets selected index to item index and updates inventory text display
	/// </summary>
	public void SetCurrentSelected(int itemIndex)
	{
		SelectedIndex = itemIndex;
		if (SelectedIndex >= 0)
		{
			cachedName = Inventory.InventoryList[SelectedIndex].ItemInfo.name;
			cachedDesc = Inventory.InventoryList[SelectedIndex].ItemInfo.description + Inventory.InventoryList[SelectedIndex].ItemInfo.stats;
		}
		else
		{
			cachedName = "";
			cachedDesc = "";
		}
		GameObject ItemName = GameObject.Find("ItemName");
		Text NameText = ItemName.GetComponent<Text>();
		GameObject ItemDesc = GameObject.Find("ItemDescription");
		Text DescText = ItemDesc.GetComponent<Text>();
		NameText.text = cachedName;
		DescText.text = cachedDesc;

	}
	/// <summary>
	/// Called by ClickItem when item is selected or deselected
	/// </summary>
	public static bool ProcessSelection(int oldSelectedIndex, int newSelectedIndex)
	{
		// Deselect old item
		if (oldSelectedIndex == newSelectedIndex
			&& oldSelectedIndex != -1)
		{
			GameObject.Find("InventoryPressed" + oldSelectedIndex).transform.localScale = Vector3.one;
			return false;
		}
		// Deselect old item & select new item
		if (oldSelectedIndex != -1)
		{
			GameObject.Find("InventoryPressed" + oldSelectedIndex).transform.localScale = Vector3.one;
		}
		if (newSelectedIndex != -1)
		{
			GameObject.Find("InventoryPressed" + newSelectedIndex).transform.localScale = Vector3.zero;
		}
		return true;
	}
	/// <summary>
	/// Shows name and desc of an item when hovering over it
	/// </summary>
	public void ProcessHoverForInventory(Vector3 MousePosition)
	{
		int iconNumber = 0;
		float sensitivityDistance = 0.5f;
		bool mouseIsOverIcon = false;
		GameObject ItemName = GameObject.Find("ItemName");
		Text NameText = ItemName.GetComponent<Text>();
		GameObject ItemDesc = GameObject.Find("ItemDescription");
		Text DescText = ItemDesc.GetComponent<Text>();
		foreach (ItemInventory Item in Inventory.InventoryList)
		{
			Image Icon = GameObject.Find("InventoryIcon" + iconNumber).GetComponent<Image>();
			Vector3 IconPosition = Icon.transform.position;
			if (Math.Abs(IconPosition.x - MousePosition.x) <= sensitivityDistance
				&& Math.Abs(IconPosition.y - MousePosition.y) <= sensitivityDistance)
			{
				mouseIsOverIcon = true;
				NameText.text = Item.ItemInfo.name;
				DescText.text = Item.ItemInfo.description + Item.ItemInfo.stats;
				break;
			}
			iconNumber++;
		}
		if (mouseIsOverIcon)
		{
			return;
		}
		if (SelectedIndex == -1)
		{
			NameText.text = "";
			DescText.text = "";
		}
		else
		{
			NameText.text = cachedName;
			DescText.text = cachedDesc;
		}
	}
}
