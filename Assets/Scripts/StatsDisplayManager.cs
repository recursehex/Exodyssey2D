using UnityEngine;
using UnityEngine.UI;

public class StatsDisplayManager : MonoBehaviour
{
    public Sprite HealthFull;
    public Sprite HealthEmpty;
    public Sprite EnergyFull;
    public Sprite EnergyEmpty;
    
    public void RestoreHealthDisplay()
    {
        Image Health2 = GameObject.Find("Health2").GetComponent<Image>();
        Health2.sprite = HealthFull;
        Image Health3 = GameObject.Find("Health3" ).GetComponent<Image>();
        Health3.sprite = HealthFull;
    }
    public void DecreaseHealthDisplay(int currentHealth, int maxHealth)
    {
        for (int i = currentHealth + 1; i < maxHealth + 1; i++)
        {
            Image Health = GameObject.Find("Health" + i).GetComponent<Image>();
            Health.sprite = HealthEmpty;
        }
    }
    public void RestoreEnergyDisplay(int currentHealth)
    {
        Image Energy1 = GameObject.Find("Energy1").GetComponent<Image>();
        Energy1.sprite = EnergyFull;
        if (currentHealth > 1)
        {
            Image Energy2 = GameObject.Find("Energy2").GetComponent<Image>();
            Energy2.sprite = EnergyFull;
            Image Energy3 = GameObject.Find("Energy3").GetComponent<Image>();
            Energy3.sprite = EnergyFull;
        }
    }
    public void DecreaseEnergyDisplay(int currentEnergy, int maxEnergy)
    {
        for (int i = currentEnergy + 1; i < maxEnergy + 1; i++)
        {
            Image Energy = GameObject.Find("Energy" + i).GetComponent<Image>();
            Energy.sprite = EnergyEmpty;
        }
    }
}
