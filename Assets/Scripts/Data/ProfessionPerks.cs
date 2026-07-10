using UnityEngine;

/// <summary>
/// Pure profession perk rules shared by Player, Vehicle, and GameManager,
/// kept free of MonoBehaviour state so they stay testable outside play mode
/// </summary>
public static class ProfessionPerks
{
	// Doubling applies to hiker walking, navigator driving, and hunter attack animations
	public const float SpeedMultiplier = 2f;
	// Surviving this many regions masters the player's profession
	public const int RegionsToMaster = 3;
	// Range that covers the entire grid from any tile, for ranger throws
	public const int FullGridRange = (GameConfig.Grid.MaxX - GameConfig.Grid.MinX) + (GameConfig.Grid.MaxY - GameConfig.Grid.MinY);
	/// <summary>
	/// Hiker walks twice as fast
	/// </summary>
	public static float GetWalkSpeed(Profession Profession, float baseSpeed) =>
		Profession.Tag is Profession.Tags.Hiker ? baseSpeed * SpeedMultiplier : baseSpeed;
	/// <summary>
	/// Hunter attack animations play twice as fast
	/// </summary>
	public static float GetAttackAnimationSpeed(Profession Profession) =>
		Profession.Tag is Profession.Tags.Hunter ? SpeedMultiplier : 1f;
	/// <summary>
	/// Navigator drives vehicles twice as fast on the grid
	/// </summary>
	public static float GetDriveSpeedMultiplier(Profession Profession) =>
		Profession.Tag is Profession.Tags.Navigator ? SpeedMultiplier : 1f;
	/// <summary>
	/// Medic heals without spending energy
	/// </summary>
	public static bool HealCostsEnergy(Profession Profession) =>
		Profession.Tag is not Profession.Tags.Medic;
	/// <summary>
	/// Master medic's MedKit doesn't lose durability
	/// </summary>
	public static bool HealConsumesDurability(Profession Profession) =>
		!(Profession.Tag is Profession.Tags.Medic && Profession.IsMaster);
	/// <summary>
	/// Mechanic repairs vehicles without spending energy
	/// </summary>
	public static bool RepairCostsEnergy(Profession Profession) =>
		Profession.Tag is not Profession.Tags.Mechanic;
	/// <summary>
	/// Master mechanic's repair tools don't lose durability
	/// </summary>
	public static bool RepairConsumesDurability(Profession Profession) =>
		!(Profession.Tag is Profession.Tags.Mechanic && Profession.IsMaster);
	/// <summary>
	/// Master hiker's first step each turn costs no energy
	/// </summary>
	public static bool HasFreeFirstStep(Profession Profession) =>
		Profession.Tag is Profession.Tags.Hiker && Profession.IsMaster;
	public static int GetWalkDistance(Profession Profession, int currentEnergy, bool hasUsedFreeStep) =>
		Mathf.Max(0, currentEnergy) + (HasFreeFirstStep(Profession) && !hasUsedFreeStep ? 1 : 0);
	public static int GetWalkEnergyCost(Profession Profession, int moveCount, bool hasUsedFreeStep) =>
		Mathf.Max(0, moveCount - (HasFreeFirstStep(Profession) && !hasUsedFreeStep ? 1 : 0));
	/// <summary>
	/// Master hunter attacks gain 1 DP; stun-only weapons stay damageless
	/// </summary>
	public static int GetAttackDamage(Profession Profession, int baseDamage) =>
		baseDamage > 0 && Profession.Tag is Profession.Tags.Hunter && Profession.IsMaster
			? baseDamage + 1
			: baseDamage;
	/// <summary>
	/// Master navigator uses 1 less fuel when traveling to the next grid
	/// </summary>
	public static int GetGridTravelCharge(Profession Profession, int efficiency) =>
		Profession.Tag is Profession.Tags.Navigator && Profession.IsMaster
			? Mathf.Max(0, efficiency - 1)
			: efficiency;
	/// <summary>
	/// Ranger throws to anywhere on the grid; master ranger's ranged weapons gain 1 RP
	/// </summary>
	public static int GetWeaponRange(Profession Profession, ItemInfo Item)
	{
		if (Item == null || !Item.HasRange)
			return 0;
		if (Profession.Tag is not Profession.Tags.Ranger)
			return Item.Range;
		if (Item.IsThrowable)
			return FullGridRange;
		return Profession.IsMaster ? Item.Range + 1 : Item.Range;
	}
	/// <summary>
	/// Returns true once enough regions are survived to master a profession
	/// </summary>
	public static bool ShouldMaster(int regionsSurvived) => regionsSurvived >= RegionsToMaster;
}
