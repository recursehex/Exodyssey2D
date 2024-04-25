using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Organizes items in the player's inventory
/// </summary>
public class InventoryUI : MonoBehaviour
{
	private Inventory inventory;
	public int SelectedIndex { get; private set; } = -1;
	private string cachedName;
	private string cachedDesc;
	public Sprite itemBackground;
	public void SetInventory(Inventory inventory)
	{
		this.inventory = inventory;
		RefreshInventoryIcons();
	}
	/// <summary>
	/// Removes item from inventory UI
	/// </summary>
	/// <param name="index"></param>
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
				GameObject itemName = GameObject.Find("ItemName");
				Text nameText = itemName.GetComponent<Text>();
				GameObject itemDesc = GameObject.Find("ItemDescription");
				Text descText = itemDesc.GetComponent<Text>();
				nameText.text = "";
				descText.text = "";
			}
			else if (index == 0)
			{
				SelectedIndex = 0;
				GameObject.Find("InventoryPressed1").transform.localScale = Vector3.one;
				GameObject.Find("InventoryPressed0").transform.localScale = Vector3.zero;
			}
		}
		inventory.RemoveItem(index);
		RefreshInventoryIcons();
	}
	/// <summary>
	/// Refreshes items in inventory UI to match changed items
	/// </summary>
	public void RefreshInventoryIcons()
	{
		// Cleanup of icons
		for (int i = 0; i < inventory.InventorySize; i++)
		{
			Image icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();
			icon.sprite = itemBackground;
		}
		int iconNumber = 0;
		// Add item icon
		foreach (ItemInventory item in inventory.itemList)
		{
			if (iconNumber < inventory.InventorySize)
			{
				Image icon = GameObject.Find("InventoryIcon" + iconNumber).GetComponent<Image>();
				icon.sprite = item.GetSprite();
			}
			iconNumber++;
		}
	}
	/// <summary>
	/// Sets selected index to item index and updates inventory text display
	/// </summary>
	/// <param name="itemIndex"></param>
	public void SetCurrentSelected(int itemIndex)
	{
		SelectedIndex = itemIndex;
		if (SelectedIndex >= 0)
		{
			cachedName = inventory.itemList[SelectedIndex].itemInfo.name;
			cachedDesc = inventory.itemList[SelectedIndex].itemInfo.description + inventory.itemList[SelectedIndex].itemInfo.stats;
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
	/// Called by ClickItem when item is selected or unselected
	/// </summary>
	/// <param name="oldSelectedIndex"></param>
	/// <param name="newSelectedIndex"></param>
	public static bool ProcessSelection(int oldSelectedIndex, int newSelectedIndex)
	{
		// Unselect old item
		if (oldSelectedIndex == newSelectedIndex && oldSelectedIndex != -1)
		{
			GameObject.Find("InventoryPressed" + oldSelectedIndex).transform.localScale = Vector3.one;
			return false;
		}
		// Unselect old item & select new item
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
		if (mouseIsOverIcon)
		{
			return;
		}
		if (SelectedIndex == -1)
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
