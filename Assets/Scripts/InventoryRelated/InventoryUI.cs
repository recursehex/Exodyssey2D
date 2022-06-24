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

        int nMaxInvUiItems = 2;

        // cleanup 
        for (int i = 0; i < nMaxInvUiItems; i++)
        {
            Image icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();
            icon.sprite = itemBackground;
        }

        // add new
        foreach (ItemInventory item in inventory.GetItemList())
        {
            if (j < nMaxInvUiItems)
            {
                Image icon = GameObject.Find("InventoryIcon" + j).GetComponent<Image>();
                icon.sprite = item.GetSprite();
            }
            j++;
        }

        /*
        // if current selected item is in 2nd slot, but will be moved to 1st slot when the item in 1st slot is used up
        if (selectedIdx == 1)
        {
            // visually deselects 2nd slot
            GameObject.Find("InventoryPressed1").transform.localScale = new Vector3(1, 1, 1);
            // visually selects 1st slot
            GameObject.Find("InventoryPressed0").transform.localScale = new Vector3(0, 0, 0);
            selectedIdx = 0;
        }
        // if current selected item is in 1st slot and is used up
        else if (selectedIdx == 0 && inventory.itemList.Count == 0)
        {
            GameObject.Find("InventoryPressed0").transform.localScale = new Vector3(1, 1, 1);
            selectedIdx = -1;
        }
        */
    }

    public bool ProcessWeaponUse()
    {
        if (selectedIdx != -1)
        {
            return inventory.ProcessWeaponUse(selectedIdx);
        }
        return false;
    }

    public bool IsRangedWeaponSelected()
    {
        if (selectedIdx != -1)
        {
            return inventory.IsRangedWeaponSelected(selectedIdx);
        }
        return false;
    }

    public int getCurrentSelected()
    {
        return selectedIdx;
    }
    public void setCurrentSelected(int nPos)
    {
        selectedIdx = nPos;
    }

    // shows name and desc of item when hovering over it
    public void ProcessHoverForInventory(Vector3 mp)
    {
        int i = 0;
        int sens = 50;
        bool found = false;

        GameObject iName = GameObject.Find("ItemName");
        Text nametxt = iName.GetComponent<Text>();
        GameObject iDesc = GameObject.Find("ItemDescription");
        Text desctxt = iDesc.GetComponent<Text>();

        foreach (ItemInventory item in inventory.GetItemList())
        {
            Image icon = GameObject.Find("InventoryIcon" + i).GetComponent<Image>();

            Vector3 p1 = icon.transform.position;

            if (Math.Abs(p1.x - mp.x) < sens && Math.Abs(p1.y - mp.y) < sens)
            {
                found = true;

                nametxt.text = item.itemInfo.itemName;
                desctxt.text = item.itemInfo.description;

                break;
            }
            i++;
        }

        if (!found)
        {
            nametxt.text = "";
            desctxt.text = "";
        }
    }
}
