using UnityEngine;

public class ItemInventory
{
    public ItemInfo itemInfo;
    public int amount;
   

    public Sprite GetSprite()
    {
        switch (itemInfo.tag)
        {
            default:
            case ItemTag.MedKit: return ItemAssets.Instance.medKit;
            case ItemTag.MedKitPlus: return ItemAssets.Instance.medKitPlus;
            case ItemTag.Branch: return ItemAssets.Instance.branch;
            //case ItemTag.RovKit: return ItemAssets.Instance.missing;
            /*
            case ItemTag.Knife: return ItemAssets.Instance.missing;
            case ItemTag.SteelBeam: return ItemAssets.Instance.missing;
            case ItemTag.Mallet: return ItemAssets.Instance.missing;
            case ItemTag.Axe: return ItemAssets.Instance.missing;
            case ItemTag.HonedGavel: return ItemAssets.Instance.missing;
            case ItemTag.TribladeRotator: return ItemAssets.Instance.missing;
            case ItemTag.BladeOfEternity: return ItemAssets.Instance.missing;
            case ItemTag.HydrogenCanister: return ItemAssets.Instance.missing;
            case ItemTag.ExternalTank: return ItemAssets.Instance.missing;
            case ItemTag.Backpack: return ItemAssets.Instance.missing;
            case ItemTag.StorageCrate: return ItemAssets.Instance.missing;
            case ItemTag.Flashlight: return ItemAssets.Instance.missing;
            case ItemTag.Lightrod: return ItemAssets.Instance.missing;
            case ItemTag.Spotlight: return ItemAssets.Instance.missing;
            case ItemTag.Matchbox: return ItemAssets.Instance.missing;
            case ItemTag.Blowtorch: return ItemAssets.Instance.missing;
            case ItemTag.Extinguisher: return ItemAssets.Instance.missing;
            case ItemTag.RangeScanner: return ItemAssets.Instance.missing;
            case ItemTag.AudioLocalizer: return ItemAssets.Instance.missing;
            case ItemTag.ThermalImager: return ItemAssets.Instance.missing;
            case ItemTag.QuantumRelocator: return ItemAssets.Instance.missing;
            case ItemTag.TemporalSedative: return ItemAssets.Instance.missing;
            case ItemTag.Flamethrower: return ItemAssets.Instance.missing;
            */
            case ItemTag.LightningRailgun: return ItemAssets.Instance.lightningRailgun;
            //case ItemTag.PaintBlaster: return ItemAssets.Instance.missing;
            case ItemTag.Unknown: return ItemAssets.Instance.missing;
                break;
        }
    }
}
