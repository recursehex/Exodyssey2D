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
			return;
		InventoryPressedLookup[index] = PressedObject.transform;
	}
	private Transform GetInventoryPressed(int index)
	{
		if (index < 0)
			return null;
		if (InventoryPressedLookup.TryGetValue(index, out Transform Transform))
			return Transform;
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
			return null;
		if (InventoryIconLookup.TryGetValue(index, out Image Image))
			return Image;
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
			ItemDescText.text = "";
	}
	/// <summary>
	/// Refreshes inventory text, called by TryDropItem
	/// </summary>
	public void RefreshText()
	{
		if (SelectedIndex != -1 && Inventory.HasItemAt(SelectedIndex))
		{
			if (ItemNameText != null)
			{
				ItemNameText.text = Inventory[SelectedIndex].Name;
				ItemNameText.color = Inventory[SelectedIndex].Rarity.Color;
			}
			if (ItemDescText != null)
			{
				ItemDescText.text = Inventory[SelectedIndex].Description
									+ Inventory[SelectedIndex].Stats;
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
				ItemDescText.text = "";
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
				continue;
			// Add item icon
			if (Inventory.HasItemAt(i))
				Icon.sprite = Item.GetSpriteForInfo(Inventory[i]);
			// Cleanup icons
			else
				Icon.sprite = ItemBackground;
		}
	}
	/// <summary>
	/// Sets selected index to item index and updates inventory text display
	/// </summary>
	public void SetCurrentSelected(int itemIndex)
	{
		if (!Inventory.HasItemAt(itemIndex))
		{
			Debug.LogError("Invalid item index: " + itemIndex);
			return;
		}
		SelectedIndex = itemIndex;
		CachedName 	= Inventory[SelectedIndex].Name;
		CachedColor = Inventory[SelectedIndex].Rarity.Color;
		CachedDesc 	= Inventory[SelectedIndex].Description
					+ Inventory[SelectedIndex].Stats;
		if (ItemNameText != null)
		{
			ItemNameText.text = CachedName;
			ItemNameText.color = CachedColor;
		}
		if (ItemDescText != null)
			ItemDescText.text = CachedDesc;
	}
	/// <summary>
	/// Syncs pressed indicators with current SelectedIndex
	/// </summary>
	public void SyncSelectionVisuals()
	{
		ResetPressedStates();
		if (SelectedIndex < 0)
			return;
		Transform PressedTransform = GetInventoryPressed(SelectedIndex);
		if (PressedTransform != null)
			PressedTransform.localScale = Vector3.zero;
	}
	/// <summary>
	/// Sets selected index to -1
	/// </summary>
	public void SetNoneSelected()
	{
		ResetPressedStates();
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
			ItemDescText.text = CachedDesc;
	}
	/// <summary>
	/// Restores all pressed indicators to their default scale
	/// </summary>
	private void ResetPressedStates()
	{
		foreach (Transform PressedTransform in InventoryPressedLookup.Values)
		{
			if (PressedTransform != null)
				PressedTransform.localScale = Vector3.one;
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
				PressedTransform.localScale = Vector3.one;
			return false;
		}
		// Deselect old item & select new item
		if (oldSelectedIndex != -1)
		{
			Transform OldPressed = GetInventoryPressed(oldSelectedIndex);
			if (OldPressed != null)
				OldPressed.localScale = Vector3.one;
		}
		if (newSelectedIndex != -1)
		{
			Transform NewPressed = GetInventoryPressed(newSelectedIndex);
			if (NewPressed != null)
				NewPressed.localScale = Vector3.zero;
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
		for (int itemIndex = 0; itemIndex < Inventory.Size; itemIndex++)
		{
			if (!Inventory.HasItemAt(itemIndex))
				continue;
			ItemInfo Item = Inventory[itemIndex];
			Image Icon = GetInventoryIcon(itemIndex);
			if (Icon == null)
			{
				continue;
			}
			Vector3 IconPosition = Icon.transform.position;
			if (Math.Abs(IconPosition.x - MousePosition.x) <= sensitivityDistance
			 && Math.Abs(IconPosition.y - MousePosition.y) <= sensitivityDistance)
			{
				mouseIsOverIcon = true;
				if (NameText != null)
				{
					NameText.text 	= Item.Name;
					NameText.color 	= Item.Rarity.Color;
				}
				if (DescText != null)
					DescText.text = Item.Description + Item.Stats;
				break;
			}
		}
		if (mouseIsOverIcon)
			return;
		if (SelectedIndex == -1)
		{
			if (NameText != null)
			{
				NameText.text 	= "";
				NameText.color 	= DefaultColor;
			}
			if (DescText != null)
				DescText.text 	= "";
		}
		else
		{
			if (NameText != null)
			{
				NameText.text 	= CachedName;
				NameText.color 	= CachedColor;
			}
			if (DescText != null)
				DescText.text 	= CachedDesc;
		}
	}
}
