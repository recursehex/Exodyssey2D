#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// Cheat-menu-only setters for Player's private state. Kept out of the production
/// source file; compiled into the Exodyssey assembly only in editor/dev builds.
/// </summary>
public partial class Player
{
	public int Debug_MaxHealth => maxHealth;
	public int Debug_CurrentHealth => currentHealth;
	public int Debug_MaxEnergy => maxEnergy;
	public bool Debug_HasHelmet => hasHelmet;
	public bool Debug_HasVest => hasVest;

	/// <summary>
	/// Sets current health, refreshing the display and lifting the low-health energy penalty.
	/// </summary>
	public void Debug_SetHealth(int value)
	{
		currentHealth = Mathf.Clamp(value, 0, maxHealth);
		if (currentHealth > 1)
			maxEnergy = fixedMaxEnergy;
		RefreshHealthDisplay();
	}

	public void Debug_SetMaxHealth(int value)
	{
		// Clamp to the icon count — the stats display can't render more than its icons (3)
		int iconCount = StatsDisplayManager.HealthIcons != null ? StatsDisplayManager.HealthIcons.Length : maxHealth;
		maxHealth = Mathf.Clamp(value, 1, iconCount);
		currentHealth = Mathf.Min(currentHealth, maxHealth);
		RefreshHealthDisplay();
	}

	public void Debug_RestoreFullHealth()
	{
		currentHealth = maxHealth;
		maxEnergy = fixedMaxEnergy;
		RefreshHealthDisplay();
	}

	public void Debug_SetMaxEnergy(int value)
	{
		// Clamp to the icon count — the stats display can't render more than its icons (3)
		int iconCount = StatsDisplayManager.EnergyIcons != null ? StatsDisplayManager.EnergyIcons.Length : maxEnergy;
		maxEnergy = Mathf.Clamp(value, 1, iconCount);
		CurrentEnergy = Mathf.Min(CurrentEnergy, maxEnergy);
		RefreshEnergyDisplay();
	}

	public void Debug_SetEnergy(int value)
	{
		CurrentEnergy = Mathf.Clamp(value, 0, maxEnergy);
		RefreshEnergyDisplay();
	}

	public void Debug_RestoreFullEnergy()
	{
		CurrentEnergy = maxEnergy;
		RefreshEnergyDisplay();
	}

	private void RefreshHealthDisplay()
	{
		StatsDisplayManager.RestoreHealthDisplay();
		// Empty every icon above the current value so reducing max visibly clears icons
		int iconCount = StatsDisplayManager.HealthIcons != null ? StatsDisplayManager.HealthIcons.Length : maxHealth;
		StatsDisplayManager.DecreaseHealthDisplay(currentHealth, iconCount);
	}

	private void RefreshEnergyDisplay() => StatsDisplayManager.SetEnergyDisplay(CurrentEnergy);

	/// <summary>
	/// Adds an item by tag straight into the inventory. Returns false if full.
	/// </summary>
	public bool Debug_AddItem(ItemInfo.Tags Tag)
	{
		if (Inventory == null)
			return false;
		if (!Inventory.TryAddItem(new ItemInfo((int)Tag)))
			return false;
		InventoryUI.RefreshInventoryIcons();
		return true;
	}

	/// <summary>
	/// Empties the inventory by reconstructing it (Inventory has no Clear()).
	/// </summary>
	public void Debug_ClearInventory()
	{
		Inventory = new(inventorySize);
		InventoryUI.Inventory = Inventory;
		SelectedItemInfo = null;
		InventoryUI.SetNoneSelected();
		InventoryUI.RefreshInventoryIcons();
		InventoryUI.RefreshText();
	}

	public void Debug_SetHelmet(bool equipped)
	{
		hasHelmet = equipped;
		helmetHealth = equipped ? new ItemInfo((int)ItemInfo.Tags.Helmet).MaxUses : 0;
	}

	public void Debug_SetVest(bool equipped)
	{
		hasVest = equipped;
		vestHealth = equipped ? new ItemInfo((int)ItemInfo.Tags.Vest).MaxUses : 0;
	}

	public void Debug_SetNightVision(bool equipped)
	{
		hasNightVision = equipped;
		if (GameManager.Instance != null)
			GameManager.Instance.RefreshVisibility();
	}

	public void Debug_RemoveAllEquipment()
	{
		hasHelmet = false;
		helmetHealth = 0;
		hasVest = false;
		vestHealth = 0;
		hasNightVision = false;
		if (GameManager.Instance != null)
			GameManager.Instance.RefreshVisibility();
	}
}
#endif
