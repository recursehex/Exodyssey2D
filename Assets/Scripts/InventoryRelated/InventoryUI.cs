using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Organizes items in the player's inventory
/// </summary>
public class InventoryUI : MonoBehaviour
{
    private Inventory inventory;

    private int selectedIdx = -1; // not selected
    private string cachedName;
    private string cachedDesc;

    [SerializeField]
    public Sprite itemBackground;

    public void SetInventory(Inventory inventory)
    {
        this.inventory = inventory;
        RefreshInventoryItems();
    }

    public void RemoveItem(int idx)
    {
        if (selectedIdx != -1)
        {
            if (selectedIdx == idx)
            {
                selectedIdx = -1;
                GameObject.Find("InventoryPressed0").transform.localScale = new Vector3(1, 1, 1);
                GameObject.Find("InventoryPressed1").transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                if (idx == 0)
                {
                    selectedIdx = 0;
                    GameObject.Find("InventoryPressed1").transform.localScale = new Vector3(1, 1, 1);
                    GameObject.Find("InventoryPressed0").transform.localScale = new Vector3(0, 0, 0);
                }
            }
        }

        inventory.RemoveItem(idx);
    }

    public void RefreshInventoryItems()
    {
        int j = 0;
        int MaxInventoryUIItems = 2;

        // cleanup of icons
        for (int i = 0; i < MaxInventoryUIItems; i++)
        {
            Image icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();
            icon.sprite = itemBackground;
        }

        // add new
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
        if (selectedIdx == n)
        {
            return inventory.ProcessDamageAfterWeaponDrop(p, selectedIdx);
        }
        return false;
    }

    public bool ProcessWeaponUse()
    {
        if (selectedIdx != -1)
        {
            return inventory.ProcessWeaponUse(selectedIdx);
        }
        return false;
    }

    public int IsRangedWeaponSelected()
    {
        if (selectedIdx != -1)
        {
            return inventory.IsRangedWeaponSelected(selectedIdx);
        }
        return 0;
    }

    public int GetCurrentSelected()
    {
        return selectedIdx;
    }

    public void SetCurrentSelected(int nPosition)
    {
        selectedIdx = nPosition;
        if (nPosition >= 0)
        {
            cachedName = inventory.itemList[selectedIdx].itemInfo.name;
            cachedDesc = inventory.itemList[selectedIdx].itemInfo.description;
        }
        else
        {
            cachedName = "";
            cachedDesc = "";
        }
        // NOTE: need to get updated desc when UP changes
    }

    // shows name and desc of item when hovering over it
    public void ProcessHoverForInventory(Vector3 mousePosition)
    {
        int i = 0;
        int sensitivityDistance = 50;
        bool found = false;

        GameObject iName = GameObject.Find("ItemName");
        Text nameText = iName.GetComponent<Text>();
        GameObject iDesc = GameObject.Find("ItemDescription");
        Text descText = iDesc.GetComponent<Text>();

        foreach (ItemInventory item in inventory.GetItemList())
        {
            Image icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();

            Vector3 iconPosition = icon.transform.position;

            if (Math.Abs(iconPosition.x - mousePosition.x) < sensitivityDistance && Math.Abs(iconPosition.y - mousePosition.y) < sensitivityDistance)
            {
                found = true;

                nameText.text = item.itemInfo.name;
                descText.text = item.itemInfo.description;

                break;
            }
            i++;
        }
        
        if (!found && selectedIdx == -1)
        {
            nameText.text = "";
            descText.text = "";
        }
        else if (!found && selectedIdx != -1)
        {
            nameText.text = cachedName;
            descText.text = cachedDesc;
        }
    }
}
