using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
	private string CachedName;
	private string CachedDesc;
	private Color CachedColor;
	private static Color DefaultColor = new(115/255f, 119/255f, 160/255f);
	private readonly float sensitivityDistance = 0.5f;
	private readonly Dictionary<int, Image> InventoryIconLookup = new();
	private readonly Dictionary<int, Transform> InventoryPressedLookup = new();
	private Text ItemNameText;
	private Text ItemDescText;
	private void Awake()
	{
		ItemNameText = ItemName != null ? ItemName.GetComponent<Text>() : null;
		ItemDescText = ItemDesc != null ? ItemDesc.GetComponent<Text>() : null;
		RegisterPressedObject(0, InventoryPressed0);
		RegisterPressedObject(1, InventoryPressed1);
	}
	private void RegisterPressedObject(int index, GameObject PressedObject)
	{
		if (index < 0 || PressedObject == null)
		{
			return;
		}
		InventoryPressedLookup[index] = PressedObject.transform;
	}
	private Transform GetInventoryPressed(int index)
	{
		if (index < 0)
		{
			return null;
		}
		if (InventoryPressedLookup.TryGetValue(index, out Transform Transform))
		{
			return Transform;
		}
		GameObject PressedObject = GameObject.Find("InventoryPressed" + index);
		if (PressedObject == null)
		{
			Debug.LogWarning($"InventoryPressed{index} not found in scene.");
			return null;
		}
		Transform = PressedObject.transform;
		InventoryPressedLookup[index] = Transform;
		return Transform;
	}
	private Image GetInventoryIcon(int index)
	{
		if (index < 0)
		{
			return null;
		}
		if (InventoryIconLookup.TryGetValue(index, out Image Image))
		{
			return Image;
		}
		GameObject IconObject = GameObject.Find("InventoryIcon" + index);
		if (IconObject == null)
		{
			Debug.LogWarning($"InventoryIcon{index} not found in scene.");
			return null;
		}
		Image = IconObject.GetComponent<Image>();
		if (Image == null)
		{
			Debug.LogWarning($"InventoryIcon{index} does not have an Image component.");
			return null;
		}
		InventoryIconLookup[index] = Image;
		return Image;
	}
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
			if (ItemNameText != null)
			{
				ItemNameText.text = "";
				ItemNameText.color = DefaultColor;
			}
			if (ItemDescText != null)
			{
				ItemDescText.text = "";
			}
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
		Transform PressedTransform = GetInventoryPressed(index);
		if (PressedTransform != null)
		{
			PressedTransform.localScale = Vector3.one;
		}
		if (ItemNameText != null)
		{
			ItemNameText.text = "";
			ItemNameText.color = DefaultColor;
		}
		if (ItemDescText != null)
		{
			ItemDescText.text = "";
		}
	}
	/// <summary>
	/// Refreshes inventory text, called by TryDropItem
	/// </summary>
	public void RefreshText()
	{
		if (SelectedIndex != -1 && SelectedIndex < Inventory.Count)
		{
			if (ItemNameText != null)
			{
				ItemNameText.text = Inventory[SelectedIndex].Info.Name;
				ItemNameText.color = Inventory[SelectedIndex].Info.Rarity.Color;
			}
			if (ItemDescText != null)
			{
				ItemDescText.text = Inventory[SelectedIndex].Info.Description
									+ Inventory[SelectedIndex].Info.Stats;
			}
		}
		else
		{
			if (ItemNameText != null)
			{
				ItemNameText.text = "";
				ItemNameText.color = DefaultColor;
			}
			if (ItemDescText != null)
			{
				ItemDescText.text = "";
			}
		}
	}
	/// <summary>
	/// Refreshes items in inventory UI to match changed items
	/// </summary>
	public void RefreshInventoryIcons()
	{
		for (int i = 0; i < Inventory.Size; i++)
		{
			Image Icon = GetInventoryIcon(i);
			if (Icon == null)
			{
				continue;
			}
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
		if (itemIndex < 0 || itemIndex >= Inventory.Count)
		{
			Debug.LogError("Invalid item index: " + itemIndex);
			return;
		}
		SelectedIndex = itemIndex;
		CachedName 	= Inventory[SelectedIndex].Info.Name;
		CachedColor = Inventory[SelectedIndex].Info.Rarity.Color;
		CachedDesc 	= Inventory[SelectedIndex].Info.Description
					+ Inventory[SelectedIndex].Info.Stats;
		if (ItemNameText != null)
		{
			ItemNameText.text = CachedName;
			ItemNameText.color = CachedColor;
		}
		if (ItemDescText != null)
		{
			ItemDescText.text = CachedDesc;
		}
	}
	/// <summary>
	/// Sets selected index to -1
	/// </summary>
	public void SetNoneSelected()
	{
		SelectedIndex 	= -1;
		CachedName 		= "";
		CachedColor 	= DefaultColor;
		CachedDesc 		= "";
		if (ItemNameText != null)
		{
			ItemNameText.text = CachedName;
			ItemNameText.color = CachedColor;
		}
		if (ItemDescText != null)
		{
			ItemDescText.text = CachedDesc;
		}
	}
	/// <summary>
	/// Called by ClickItem when item is selected or deselected
	/// </summary>
	public bool ProcessSelection(int oldSelectedIndex, int newSelectedIndex)
	{
		// Deselect old item
		if (oldSelectedIndex == newSelectedIndex
			&& oldSelectedIndex != -1)
		{
			Transform PressedTransform = GetInventoryPressed(oldSelectedIndex);
			if (PressedTransform != null)
			{
				PressedTransform.localScale = Vector3.one;
			}
			return false;
		}
		// Deselect old item & select new item
		if (oldSelectedIndex != -1)
		{
			Transform OldPressed = GetInventoryPressed(oldSelectedIndex);
			if (OldPressed != null)
			{
				OldPressed.localScale = Vector3.one;
			}
		}
		if (newSelectedIndex != -1)
		{
			Transform NewPressed = GetInventoryPressed(newSelectedIndex);
			if (NewPressed != null)
			{
				NewPressed.localScale = Vector3.zero;
			}
		}
		return true;
	}
	/// <summary>
	/// Shows name and desc of an item when hovering over it
	/// </summary>
	public void ProcessHoverForInventory(Vector3 MousePosition)
	{
		bool mouseIsOverIcon = false;
		Text NameText = ItemNameText;
		Text DescText = ItemDescText;
		int itemIndex = 0;
		while (itemIndex < Inventory.Count)
		{
			Item Item = Inventory[itemIndex];
			Image Icon = GetInventoryIcon(itemIndex);
			if (Icon == null)
			{
				itemIndex++;
				continue;
			}
			Vector3 IconPosition = Icon.transform.position;
			if (Math.Abs(IconPosition.x - MousePosition.x) <= sensitivityDistance
			 && Math.Abs(IconPosition.y - MousePosition.y) <= sensitivityDistance)
			{
				mouseIsOverIcon = true;
				if (NameText != null)
				{
					NameText.text 	= Item.Info.Name;
					NameText.color 	= Item.Info.Rarity.Color;
				}
				if (DescText != null)
				{
					DescText.text = Item.Info.Description + Item.Info.Stats;
				}
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
			if (NameText != null)
			{
				NameText.text 	= "";
				NameText.color 	= DefaultColor;
			}
			if (DescText != null)
			{
				DescText.text 	= "";
			}
		}
		else
		{
			if (NameText != null)
			{
				NameText.text 	= CachedName;
				NameText.color 	= CachedColor;
			}
			if (DescText != null)
			{
				DescText.text 	= CachedDesc;
			}
		}
	}
}