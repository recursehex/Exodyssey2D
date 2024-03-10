using UnityEngine;

public class ItemInventory
{
	public ItemInfo itemInfo;

	public Sprite GetSprite()
	{
		return itemInfo.tag switch
		{
			ItemInfo.ItemTag.MedKit => ItemAssets.Instance.medKit,
			ItemInfo.ItemTag.Branch => ItemAssets.Instance.branch,
			ItemInfo.ItemTag.DiamondChainsaw => ItemAssets.Instance.diamondChainsaw,
			ItemInfo.ItemTag.Knife => ItemAssets.Instance.knife,
			/*
			ItemInfo.ItemTag.ToolKit => ItemAssets.Instance.toolKit,
			ItemInfo.ItemTag.Wrench => ItemAssets.Instance.wrench,
			ItemInfo.ItemTag.Mallet => ItemAssets.Instance.mallet,
			ItemInfo.ItemTag.Axe => ItemAssets.Instance.axe,
			ItemInfo.ItemTag.Rock => ItemAssets.Instance.rock,
			ItemInfo.ItemTag.SmokeGrenade => ItemAssets.Instance.smokeGrenade,
			ItemInfo.ItemTag.Dynamite => ItemAssets.Instance.dynamite,
			ItemInfo.ItemTag.StickyGrenade => ItemAssets.Instance.stickyGrenade,
			ItemInfo.ItemTag.Tranquilizer => ItemAssets.Instance.tranquilizer,
			ItemInfo.ItemTag.Carbine => ItemAssets.Instance.carbine,
			ItemInfo.ItemTag.Flamethrower => ItemAssets.Instance.flamethrower,
			*/
			ItemInfo.ItemTag.HuntingRifle => ItemAssets.Instance.huntingRifle,
			ItemInfo.ItemTag.PlasmaRailgun => ItemAssets.Instance.plasmaRailgun,
			/*
			ItemInfo.ItemTag.FusionCell => ItemAssets.Instance.fusionCell,
			ItemInfo.ItemTag.Battery => ItemAssets.Instance.battery,
			ItemInfo.ItemTag.Backpack => ItemAssets.Instance.backpack,
			ItemInfo.ItemTag.Crate => ItemAssets.Instance.crate,
			ItemInfo.ItemTag.Lightrod => ItemAssets.Instance.lightrod,
			ItemInfo.ItemTag.Extinguisher => ItemAssets.Instance.extinguisher,
			ItemInfo.ItemTag.Spotlight => ItemAssets.Instance.spotlight,
			ItemInfo.ItemTag.Blowtorch => ItemAssets.Instance.blowtorch,
			ItemInfo.ItemTag.ThermalImager => ItemAssets.Instance.thermalImager,
			ItemInfo.ItemTag.NightVision => ItemAssets.Instance.nightVision;
			ItemInfo.ItemTag.Helmet => ItemAssets.Instance.helmet;
			ItemInfo.ItemTag.Vest => ItemAssets.Instance.vest;
			ItemInfo.ItemTag.GrapheneShield => ItemAssets.Instance.grapheneShield;
			*/
			ItemInfo.ItemTag.Unknown => ItemAssets.Instance.missing,
			_ => ItemAssets.Instance.missing,
		};
	}
}
