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
			ItemTag.DiamondChainsaw => ItemAssets.Instance.diamondChainsaw,
			ItemTag.Knife => ItemAssets.Instance.knife,
			/*
			ItemTag.ToolKit => ItemAssets.Instance.toolKit,
			ItemTag.Wrench => ItemAssets.Instance.wrench,
			ItemTag.Mallet => ItemAssets.Instance.mallet,
			ItemTag.Axe => ItemAssets.Instance.axe,
			ItemTag.Rock => ItemAssets.Instance.rock,
			ItemTag.SmokeGrenade => ItemAssets.Instance.smokeGrenade,
			ItemTag.Dynamite => ItemAssets.Instance.dynamite,
			ItemTag.StickyGrenade => ItemAssets.Instance.stickyGrenade,
			ItemTag.Tranquilizer => ItemAssets.Instance.tranquilizer,
			ItemTag.Carbine => ItemAssets.Instance.carbine,
			ItemTag.Flamethrower => ItemAssets.Instance.flamethrower,
			*/
			ItemTag.HuntingRifle => ItemAssets.Instance.huntingRifle,
			/*
			ItemTag.FusionCell => ItemAssets.Instance.fusionCell,
			ItemTag.Battery => ItemAssets.Instance.battery,
			ItemTag.Backpack => ItemAssets.Instance.backpack,
			ItemTag.Crate => ItemAssets.Instance.crate,
			ItemTag.Lightrod => ItemAssets.Instance.lightrod,
			ItemTag.Extinguisher => ItemAssets.Instance.extinguisher,
			ItemTag.Spotlight => ItemAssets.Instance.spotlight,
			ItemTag.Blowtorch => ItemAssets.Instance.blowtorch,
			ItemTag.ThermalImager => ItemAssets.Instance.thermalImager,
			ItemTag.NightVision => ItemAssets.Instance.nightVision;
			ItemTag.Helmet => ItemAssets.Instance.helmet;
			ItemTag.Vest => ItemAssets.Instance.vest;
			ItemTag.GrapheneShield => ItemAssets.Instance.grapheneShield;
			*/
			ItemTag.PlasmaRailgun => ItemAssets.Instance.plasmaRailgun,
			ItemTag.Unknown => ItemAssets.Instance.missing,
			_ => ItemAssets.Instance.missing,
		};
	}
}
