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
    public void RemoveItem(int index)
    {
        if (selectedIndex == -1) return;
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
        if (selectedIndex != n) return false;
        return inventory.ProcessDamageAfterWeaponDrop(p, selectedIndex);
    }
    public bool UpdateWeaponUP()
    {
        if (selectedIndex == -1) return false;
        bool wasUsed = inventory.UpdateWeaponUP(selectedIndex);
        SetCurrentSelected(selectedIndex);
        return wasUsed;
    }
    public int GetWeaponRange()
    {
        if (selectedIndex == -1) return 0;
        return inventory.GetWeaponRange(selectedIndex);
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
