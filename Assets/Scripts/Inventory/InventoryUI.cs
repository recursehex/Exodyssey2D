using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Organizes items in the player's inventory
/// </summary>
public class InventoryUI : MonoBehaviour
{
    private Inventory inventory;
    private int selectedIdx = -1;
    private string cachedName;
    private string cachedDesc;
    public Sprite itemBackground;

    public void SetInventory(Inventory inventory)
    {
        this.inventory = inventory;
        RefreshInventoryItems();
    }

    public void RemoveItem(int index)
    {
        if (selectedIdx == -1) return;
        if (selectedIdx == index)
        {
            selectedIdx = -1;
            GameObject.Find("InventoryPressed0").transform.localScale = Vector3.one;
            GameObject.Find("InventoryPressed1").transform.localScale = Vector3.one;
        }
        else if (index == 0)
        {
            selectedIdx = 0;
            GameObject.Find("InventoryPressed1").transform.localScale = Vector3.one;
            GameObject.Find("InventoryPressed0").transform.localScale = Vector3.zero;
        }
        inventory.RemoveItem(index);
        RefreshInventoryItems();
    }

    public void RefreshInventoryItems()
    {
        int j = 0;
        int MaxInventoryUIItems = 2;
        // Cleanup of icons
        for (int i = 0; i < MaxInventoryUIItems; i++)
        {
            Image icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();
            icon.sprite = itemBackground;
        }
        // Add item icon
        foreach (ItemInventory item in inventory.GetItemList())
        {
            if (j < MaxInventoryUIItems)
            {
                Image icon = GameObject.Find("InventoryIcon" + j).GetComponent<Image>();
                icon.sprite = item.GetSprite();
            }
            j++;
        }
    }

    public bool ProcessDamageAfterWeaponDrop(Player p, int n)
    {
        if (selectedIdx != n) return false;
        return inventory.ProcessDamageAfterWeaponDrop(p, selectedIdx);
    }

    public bool UpdateWeaponUP()
    {
        if (selectedIdx == -1) return false;
        bool wasUsed = inventory.UpdateWeaponUP(selectedIdx);
        SetCurrentSelected(selectedIdx);
        return wasUsed;
    }

    public int GetWeaponRange()
    {
        if (selectedIdx == -1) return 0;
        return inventory.GetWeaponRange(selectedIdx);
    }

    public int GetCurrentSelected()
    {
        return selectedIdx;
    }

    public void SetCurrentSelected(int itemPosition)
    {
        selectedIdx = itemPosition;
        if (itemPosition >= 0)
        {
            cachedName = inventory.itemList[selectedIdx].itemInfo.name;
            cachedDesc = inventory.itemList[selectedIdx].itemInfo.description + inventory.itemList[selectedIdx].itemInfo.stats;
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
        int i = 0;
        float sensitivityDistance = 0.5f;
        bool mouseIsOverIcon = false;
        GameObject itemName = GameObject.Find("ItemName");
        Text nameText = itemName.GetComponent<Text>();
        GameObject itemDesc = GameObject.Find("ItemDescription");
        Text descText = itemDesc.GetComponent<Text>();
        foreach (ItemInventory item in inventory.GetItemList())
        {
            Image icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();
            Vector3 iconPosition = icon.transform.position;
            if (Math.Abs(iconPosition.x - mousePosition.x) <= sensitivityDistance && Math.Abs(iconPosition.y - mousePosition.y) <= sensitivityDistance)
            {
                mouseIsOverIcon = true;
                nameText.text = item.itemInfo.name;
                descText.text = item.itemInfo.description + item.itemInfo.stats;
                break;
            }
            i++;
        }
        if (mouseIsOverIcon) return;
        if (selectedIdx == -1)
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
