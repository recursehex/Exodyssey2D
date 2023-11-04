using UnityEngine.UI;
using UnityEngine;

public class EnergyDisplayManager : MonoBehaviour
{
    public Sprite energyFull;
    public Sprite energyEmpty;
    
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
            Image energy3 = GameObject.Find("Energy" + i).GetComponent<Image>();
            energy3.sprite = energyEmpty;
        }
    }
}
