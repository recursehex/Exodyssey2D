using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Organizes items in the player's inventory
/// </summary>
public class InventoryUI : MonoBehaviour
{
	public Inventory Inventory;
	[SerializeField] private Sprite ItemBackground;
	[SerializeField] private GameObject InventoryPressed0;
	[SerializeField] private GameObject InventoryPressed1;
	[SerializeField] private GameObject ItemName;
	[SerializeField] private GameObject ItemDesc;
	public int SelectedIndex { get; private set; } = -1;
	private string cachedName;
	private Color cachedColor;
	private string cachedDesc;
	private static Color defaultColor = new(115/255f, 119/255f, 160/255f);
	private readonly float sensitivityDistance = 0.5f;
	/// <summary>
	/// Removes item from inventory UI
	/// </summary>
	public void RemoveItem(int index)
	{
		// Resets icons and inventory text
		if (SelectedIndex == index)
		{
			SelectedIndex = -1;
			InventoryPressed0.transform.localScale = Vector3.one;
			InventoryPressed1.transform.localScale = Vector3.one;
			ItemName.GetComponent<Text>().text = "";
			ItemName.GetComponent<Text>().color = defaultColor;
			ItemDesc.GetComponent<Text>().text = "";
		}
		else if (index == 0 && SelectedIndex != -1)
		{
			SelectedIndex = 0;
			InventoryPressed1.transform.localScale = Vector3.one;
			InventoryPressed0.transform.localScale = Vector3.zero;
		}
		Inventory.RemoveItem(index);
		RefreshInventoryIcons();
	}
	/// <summary>
	/// Deselects item in inventory UI if selected item is dropped
	/// </summary>
	public void DeselectItem(int index)
	{
		SelectedIndex = -1;
		GameObject.Find("InventoryPressed" + index).transform.localScale = Vector3.one;
		ItemName.GetComponent<Text>().text = "";
		ItemName.GetComponent<Text>().color = defaultColor;
		ItemDesc.GetComponent<Text>().text = "";
	}
	/// <summary>
	/// Refreshes inventory text, called by TryDropItem
	/// </summary>
	public void RefreshText()
	{
		if (SelectedIndex != -1)
		{
			ItemName.GetComponent<Text>().text = Inventory[SelectedIndex].Info.Name;
			ItemName.GetComponent<Text>().color = Inventory[SelectedIndex].Info.Rarity.Color;
			ItemDesc.GetComponent<Text>().text = Inventory[SelectedIndex].Info.Description
			+ Inventory[SelectedIndex].Info.Stats;
		}
		else
		{
			ItemName.GetComponent<Text>().text = "";
			ItemName.GetComponent<Text>().color = defaultColor;
			ItemDesc.GetComponent<Text>().text = "";
		}
	}
	/// <summary>
	/// Refreshes items in inventory UI to match changed items
	/// </summary>
	public void RefreshInventoryIcons()
	{
		for (int i = 0; i < Inventory.Size; i++)
		{
			Image Icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();
			if (i < Inventory.Count)
			{
				// Add item icon
				Icon.sprite = Inventory[i].GetSprite();
			}
			else
			{
				// Cleanup of icons
				Icon.sprite = ItemBackground;
			}
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
			cachedName = Inventory[SelectedIndex].Info.Name;
			cachedColor = Inventory[SelectedIndex].Info.Rarity.Color;
			cachedDesc = Inventory[SelectedIndex].Info.Description
			+ Inventory[SelectedIndex].Info.Stats;
		}
		else
		{
			cachedName = "";
			cachedColor = defaultColor;
			cachedDesc = "";
		}
		ItemName.GetComponent<Text>().text = cachedName;
		ItemName.GetComponent<Text>().color = cachedColor;
		ItemDesc.GetComponent<Text>().text = cachedDesc;
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
		bool mouseIsOverIcon = false;
		Text NameText = ItemName.GetComponent<Text>();
		Text DescText = ItemDesc.GetComponent<Text>();
		int itemIndex = 0;
		while (itemIndex < Inventory.Count)
		{
			Item Item = Inventory[itemIndex];
			Image Icon = GameObject.Find("InventoryIcon" + itemIndex).GetComponent<Image>();
			Vector3 IconPosition = Icon.transform.position;
			if (Math.Abs(IconPosition.x - MousePosition.x) <= sensitivityDistance
			 && Math.Abs(IconPosition.y - MousePosition.y) <= sensitivityDistance)
			{
				mouseIsOverIcon = true;
				NameText.text = Item.Info.Name;
				NameText.color = Item.Info.Rarity.Color;
				DescText.text = Item.Info.Description + Item.Info.Stats;
				break;
			}
			itemIndex++;
		}
		if (mouseIsOverIcon)
		{
			return;
		}
		if (SelectedIndex == -1)
		{
			NameText.text = "";
			NameText.color = Color.white;
			DescText.text = "";
		}
		else
		{
			NameText.text = cachedName;
			NameText.color = cachedColor;
			DescText.text = cachedDesc;
		}
	}
}
