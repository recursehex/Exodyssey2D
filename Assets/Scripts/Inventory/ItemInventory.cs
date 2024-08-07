using UnityEngine;

public class ItemInventory
{
	public ItemInfo ItemInfo;
	
	public ItemInventory(ItemInfo ItemInfo) 
	{
		this.ItemInfo = ItemInfo;
	}

	public Sprite GetSprite()
	{
		return ItemInfo.Tag switch
		{
			ItemInfo.Tags.MedKit => ItemAssets.Instance.MedKit,
			ItemInfo.Tags.Branch => ItemAssets.Instance.Branch,
			ItemInfo.Tags.Knife => ItemAssets.Instance.Knife,
			ItemInfo.Tags.Wrench => ItemAssets.Instance.Wrench,
			ItemInfo.Tags.DiamondChainsaw => ItemAssets.Instance.DiamondChainsaw,
			// ItemInfo.Tags.ToolKit => ItemAssets.Instance.ToolKit,
			// ItemInfo.Tags.Mallet => ItemAssets.Instance.Mallet,
			// ItemInfo.Tags.Axe => ItemAssets.Instance.Axe,
			// ItemInfo.Tags.Rock => ItemAssets.Instance.Rock,
			// ItemInfo.Tags.SmokeGrenade => ItemAssets.Instance.SmokeGrenade,
			// ItemInfo.Tags.Dynamite => ItemAssets.Instance.Dynamite,
			// ItemInfo.Tags.StickyGrenade => ItemAssets.Instance.StickyGrenade,
			ItemInfo.Tags.Tranquilizer => ItemAssets.Instance.Tranquilizer,
			// ItemInfo.Tags.Carbine => ItemAssets.Instance.Carbine,
			// ItemInfo.Tags.Flamethrower => ItemAssets.Instance.Flamethrower,
			ItemInfo.Tags.HuntingRifle => ItemAssets.Instance.HuntingRifle,
			ItemInfo.Tags.PlasmaRailgun => ItemAssets.Instance.PlasmaRailgun,
			// ItemInfo.Tags.FusionCell => ItemAssets.Instance.FusionCell,
			// ItemInfo.Tags.Battery => ItemAssets.Instance.Battery,
			// ItemInfo.Tags.Backpack => ItemAssets.Instance.Backpack,
			// ItemInfo.Tags.Crate => ItemAssets.Instance.Crate,
			// ItemInfo.Tags.Lightrod => ItemAssets.Instance.Lightrod,
			// ItemInfo.Tags.Extinguisher => ItemAssets.Instance.Extinguisher,
			// ItemInfo.Tags.Spotlight => ItemAssets.Instance.Spotlight,
			// ItemInfo.Tags.Blowtorch => ItemAssets.Instance.Blowtorch,
			// ItemInfo.Tags.ThermalImager => ItemAssets.Instance.ThermalImager,
			// ItemInfo.Tags.NightVision => ItemAssets.Instance.NightVision;
			// ItemInfo.Tags.Helmet => ItemAssets.Instance.Helmet;
			// ItemInfo.Tags.Vest => ItemAssets.Instance.Vest;
			// ItemInfo.Tags.GrapheneShield => ItemAssets.Instance.GrapheneShield;
			ItemInfo.Tags.Unknown => ItemAssets.Instance.Missing,
			_ => ItemAssets.Instance.Missing,
		};
	}
}
