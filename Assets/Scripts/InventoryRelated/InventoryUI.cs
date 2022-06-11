using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Organizes items in the player's inventory
/// </summary>
public class InventoryUI : MonoBehaviour
{
    private Inventory inventory;

    [SerializeField]
    public Sprite itemBackground;

    public void SetInventory(Inventory inventory)
    {
        this.inventory = inventory;
        RefreshInventoryItems();
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
