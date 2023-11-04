using UnityEngine;

public class ItemInventory
{
    public ItemInfo itemInfo;

    public Sprite GetSprite()
    {
        return itemInfo.tag switch
        {
            ItemTag.MedKit => ItemAssets.Instance.medKit,
            ItemTag.Branch => ItemAssets.Instance.branch,
            ItemTag.DiamondChainsaw => ItemAssets.Instance.honedGavel,
            /*
            ItemTag.ToolKit => ItemAssets.Instance.toolKit,
            ItemTag.Knife => ItemAssets.Instance.knife,
            ItemTag.Wrench => ItemAssets.Instance.wrench,
            ItemTag.Mallet => ItemAssets.Instance.mallet,
            ItemTag.Axe => ItemAssets.Instance.axe,
            ItemTag.Rock => ItemAssets.Instance.rock,
            ItemTag.SmokeGrenade => ItemAssets.Instance.smokeGrenade,
            ItemTag.Dynamite => ItemAssets.Instance.dynamite,
            ItemTag.BottledLightning => ItemAssets.Instance.bottledLightning,
            ItemTag.Tranquilizer => ItemAssets.Instance.tranquilizer,
            ItemTag.Carbine => ItemAssets.Instance.carbine,
            ItemTag.Flamethrower => ItemAssets.Instance.flamethrower,
            */
            ItemTag.HuntingRifle => ItemAssets.Instance.huntingRifle,
            /*
            ItemTag.HydrogenCanister => ItemAssets.Instance.hydrogenCanister,
            ItemTag.FuelTank => ItemAssets.Instance.fuelTank,
            ItemTag.Backpack => ItemAssets.Instance.backpack,
            ItemTag.Crate => ItemAssets.Instance.crate,
            ItemTag.Lightrod => ItemAssets.Instance.lightrod,
            ItemTag.Extinguisher => ItemAssets.Instance.extinguisher,
            ItemTag.Spotlight => ItemAssets.Instance.spotlight,
            ItemTag.Blowtorch => ItemAssets.Instance.blowtorch,
            ItemTag.ThermalImager => ItemAssets.Instance.thermalImager,
            ItemTag.NightVision => ItemAssets.Instance.nightVision;
            */
            ItemTag.PlasmaRailgun => ItemAssets.Instance.plasmaRailgun,
            ItemTag.Unknown => ItemAssets.Instance.missing,
            _ => ItemAssets.Instance.missing,
        };
    }
}
