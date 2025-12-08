using UnityEngine;
using UnityEngine.UI;

public class StatsDisplayManager : MonoBehaviour
{
	public Sprite HealthFull;
	public Sprite HealthEmpty;
	public Sprite EnergyFull;
	public Sprite EnergyEmpty;
	public Image[] HealthIcons;
	public Image[] EnergyIcons;
	
	/// <summary>
	/// Restores Player Health icons to match MaxHealth
	/// </summary>
	public void RestoreHealthDisplay()
	{
		for (int i = 0; i < HealthIcons.Length; i++)
			HealthIcons[i].sprite = HealthFull;
	}
	/// <summary>
	/// Decreases Player Health icons, requires current and max health
	/// </summary>
	public void DecreaseHealthDisplay(int currentHealth, int maxHealth)
	{
		int clampedMax = Mathf.Min(maxHealth, HealthIcons.Length);
		for (int i = currentHealth; i < clampedMax; i++)
			HealthIcons[i].sprite = HealthEmpty;
	}
	/// <summary>
	/// Restores Player Energy icons to match MaxEnergy, requires current health
	/// </summary>
	public void RestoreEnergyDisplay(int currentHealth)
	{
		for (int i = 0; i < EnergyIcons.Length; i++)
			EnergyIcons[i].sprite = (currentHealth > 1 || i == 0) ? EnergyFull : EnergyEmpty;
	}
	/// <summary>
	/// Decreases Player Energy icons, requires current and max energy
	/// </summary>
	public void DecreaseEnergyDisplay(int currentEnergy, int maxEnergy)
	{
		int clampedMax = Mathf.Min(maxEnergy, EnergyIcons.Length);
		for (int i = currentEnergy; i < clampedMax; i++)
			EnergyIcons[i].sprite = EnergyEmpty;
	}
}
