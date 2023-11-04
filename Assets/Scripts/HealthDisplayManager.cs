using UnityEngine.UI;
using UnityEngine;

public class HealthDisplayManager : MonoBehaviour
{
    public Sprite healthFull;
    public Sprite healthEmpty;
    
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
            Image health3 = GameObject.Find("Health" + i).GetComponent<Image>();
            health3.sprite = healthEmpty;
        }
    }
}
