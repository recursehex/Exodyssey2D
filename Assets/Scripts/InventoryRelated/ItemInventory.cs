using UnityEngine;

public class ItemInventory
{
    public ItemInfo itemInfo;
    public int amount;
   

    public Sprite GetSprite()
    {
        switch (itemInfo.type)
        {
            default:
            case ItemType.MedKit: return ItemAssets.Instance.medKit;
            case ItemType.MedKitPlus: return ItemAssets.Instance.medKitPlus;
            case ItemType.Branch: return ItemAssets.Instance.branch;
            //case ItemType.RovKit: return ItemAssets.Instance.missing;
            /*
            case ItemType.Knife: return ItemAssets.Instance.missing;
            case ItemType.SteelBeam: return ItemAssets.Instance.missing;
            case ItemType.Mallet: return ItemAssets.Instance.missing;
            case ItemType.Axe: return ItemAssets.Instance.missing;
            case ItemType.HonedGavel: return ItemAssets.Instance.missing;
            case ItemType.TribladeRotator: return ItemAssets.Instance.missing;
            case ItemType.BladeOfEternity: return ItemAssets.Instance.missing;
            case ItemType.HydrogenCanister: return ItemAssets.Instance.missing;
            case ItemType.ExternalTank: return ItemAssets.Instance.missing;
            case ItemType.Backpack: return ItemAssets.Instance.missing;
            case ItemType.StorageCrate: return ItemAssets.Instance.missing;
            case ItemType.Flashlight: return ItemAssets.Instance.missing;
            case ItemType.Lightrod: return ItemAssets.Instance.missing;
            case ItemType.Spotlight: return ItemAssets.Instance.missing;
            case ItemType.Matchbox: return ItemAssets.Instance.missing;
            case ItemType.Blowtorch: return ItemAssets.Instance.missing;
            case ItemType.Extinguisher: return ItemAssets.Instance.missing;
            case ItemType.RangeScanner: return ItemAssets.Instance.missing;
            case ItemType.AudioLocalizer: return ItemAssets.Instance.missing;
            case ItemType.ThermalImager: return ItemAssets.Instance.missing;
            case ItemType.QuantumRelocator: return ItemAssets.Instance.missing;
            case ItemType.TemporalSedative: return ItemAssets.Instance.missing;
            case ItemType.Flamethrower: return ItemAssets.Instance.missing;
            */
            case ItemType.LightningRailgun: return ItemAssets.Instance.lightningRailgun;
            //case ItemType.PaintBlaster: return ItemAssets.Instance.missing;
            case ItemType.Unknown: return ItemAssets.Instance.missing;
                break;
        }
    }
}
