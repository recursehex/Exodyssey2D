using UnityEngine;

public class Item : MonoBehaviour
{
	public ItemInfo Info;
	public Item(ItemInfo Info)
	{
		this.Info = Info;
	}
	public Sprite GetSprite()
	{
		return Info.Tag switch
		{
			ItemInfo.Tags.MedKit => Resources.Load<Sprite>("Sprites/medkit"),
			ItemInfo.Tags.ToolKit => Resources.Load<Sprite>("Sprites/toolkit"),
			// ItemInfo.Tags.PowerCell => ItemAssets.Instance.PowerCell,
			ItemInfo.Tags.Branch => Resources.Load<Sprite>("Sprites/branch"),
			ItemInfo.Tags.Knife => Resources.Load<Sprite>("Sprites/knife"),
			ItemInfo.Tags.Wrench => Resources.Load<Sprite>("Sprites/wrench"),
			ItemInfo.Tags.Mallet => Resources.Load<Sprite>("Sprites/mallet"),
			ItemInfo.Tags.FireAxe => Resources.Load<Sprite>("Sprites/axe"),
			ItemInfo.Tags.DiamondChainsaw => Resources.Load<Sprite>("Sprites/diamond_chainsaw"),
			// ItemInfo.Tags.Rock => Resources.Load<Sprite>("Sprites/rock"),
			// ItemInfo.Tags.SmokeGrenade => Resources.Load<Sprite>("Sprites/smoke_grenade"),
			// ItemInfo.Tags.Dynamite => Resources.Load<Sprite>("Sprites/dynamite"),
			// ItemInfo.Tags.StickyGrenade => Resources.Load<Sprite>("Sprites/sticky_grenade"),
			ItemInfo.Tags.Tranquilizer => Resources.Load<Sprite>("Sprites/tranquilizer"),
			// ItemInfo.Tags.Carbine => Resources.Load<Sprite>("Sprites/carbine"),
			// ItemInfo.Tags.Flamethrower => Resources.Load<Sprite>("Sprites/flamethrower"),
			ItemInfo.Tags.HuntingRifle => Resources.Load<Sprite>("Sprites/hunting_rifle"),
			ItemInfo.Tags.PlasmaRailgun => Resources.Load<Sprite>("Sprites/plasma_railgun"),
			// ItemInfo.Tags.Battery => Resources.Load<Sprite>("Sprites/battery"),
			// ItemInfo.Tags.Backpack => Resources.Load<Sprite>("Sprites/backpack"),
			// ItemInfo.Tags.Crate => Resources.Load<Sprite>("Sprites/crate"),
			// ItemInfo.Tags.Lightrod => Resources.Load<Sprite>("Sprites/lightrod"),
			// ItemInfo.Tags.Extinguisher => Resources.Load<Sprite>("Sprites/extinguisher"),
			// ItemInfo.Tags.Spotlight => Resources.Load<Sprite>("Sprites/spotlight"),
			// ItemInfo.Tags.Blowtorch => Resources.Load<Sprite>("Sprites/blowtorch"),
			// ItemInfo.Tags.ThermalImager => Resources.Load<Sprite>("Sprites/thermal_imager"),
			// ItemInfo.Tags.NightVision => Resources.Load<Sprite>("Sprites/night_vision"),
			// ItemInfo.Tags.Helmet => Resources.Load<Sprite>("Sprites/helmet"),
			// ItemInfo.Tags.Vest => Resources.Load<Sprite>("Sprites/vest"),
			// ItemInfo.Tags.GrapheneShield => Resources.Load<Sprite>("Sprites/graphene_shield"),
			ItemInfo.Tags.Unknown => Resources.Load<Sprite>("Sprites/missing"),
			_ => Resources.Load<Sprite>("Sprites/missing"),
		};
	}
}
