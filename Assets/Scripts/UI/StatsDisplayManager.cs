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
		HealthIcons[1].sprite = HealthFull;
		HealthIcons[2].sprite = HealthFull;
	}
	/// <summary>
	/// Decreases Player Health icons, requires current and max health
	/// </summary>
	public void DecreaseHealthDisplay(int currentHealth, int maxHealth)
	{
		for (int i = currentHealth + 1; i < maxHealth + 1; i++)
		{
			HealthIcons[i-1].sprite = HealthEmpty;
		}
	}
	/// <summary>
	/// Restores Player Energy icons to match MaxEnergy, requires current health
	/// </summary>
	public void RestoreEnergyDisplay(int currentHealth)
	{
		EnergyIcons[0].sprite = EnergyFull;
		if (currentHealth > 1)
		{
			EnergyIcons[1].sprite = EnergyFull;
			EnergyIcons[2].sprite = EnergyFull;
		}
	}
	/// <summary>
	/// Decreases Player Energy icons, requires current and max energy
	/// </summary>
	public void DecreaseEnergyDisplay(int currentEnergy, int maxEnergy)
	{
		for (int i = currentEnergy + 1; i < maxEnergy + 1; i++)
		{
			EnergyIcons[i-1].sprite = EnergyEmpty;
		}
	}
}
