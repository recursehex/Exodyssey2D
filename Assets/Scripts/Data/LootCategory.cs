/// <summary>
/// Functional categories items declare in ItemDefinitions.json.
/// Grid archetype loot profiles bias generation by multiplying
/// item weights per category, so items never need to be listed
/// per archetype by hand
/// </summary>
public enum LootCategory
{
	Medical = 0,
	Repair,
	Fuel,
	MeleeWeapon,
	RangedWeapon,
	Throwable,
	Armor,
	Storage,
	Light,
	Recon,
	FireTool,
}
