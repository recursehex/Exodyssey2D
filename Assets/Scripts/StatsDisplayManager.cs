using UnityEngine;
using UnityEngine.UI;

public class StatsDisplayManager : MonoBehaviour
{
    public Sprite healthFull;
    public Sprite healthEmpty;
    public Sprite energyFull;
    public Sprite energyEmpty;
    
    public void RestoreHealthDisplay()
    {
        Image health2 = GameObject.Find("Health2").GetComponent<Image>();
        health2.sprite = healthFull;
        Image health3 = GameObject.Find("Health3" ).GetComponent<Image>();
        health3.sprite = healthFull;
    }
    public void DecreaseHealthDisplay(int currentHealth, int maxHealth)
    {
        for (int i = currentHealth + 1; i < maxHealth + 1; i++)
        {
            Image health = GameObject.Find("Health" + i).GetComponent<Image>();
            health.sprite = healthEmpty;
        }
    }
    public void RestoreEnergyDisplay(int currentHealth)
    {
        Image energy1 = GameObject.Find("Energy1").GetComponent<Image>();
        energy1.sprite = energyFull;
        if (currentHealth > 1)
        {
            Image energy2 = GameObject.Find("Energy2").GetComponent<Image>();
            energy2.sprite = energyFull;
            Image energy3 = GameObject.Find("Energy3").GetComponent<Image>();
            energy3.sprite = energyFull;
        }
    }
    public void DecreaseEnergyDisplay(int currentEnergy, int maxEnergy)
    {
        for (int i = currentEnergy + 1; i < maxEnergy + 1; i++)
        {
            Image energy = GameObject.Find("Energy" + i).GetComponent<Image>();
            energy.sprite = energyEmpty;
        }
    }
}
