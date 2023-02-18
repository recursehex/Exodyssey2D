using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public VehicleInfo info;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Changes vehicle's HP, use negative to decrease
    /// </summary>
    public void ChangeHealth(int change)
    {
        if (info.currentHP + change > info.maxHP) // if new HP is greater than max
        {
            info.currentHP = info.maxHP;
        }
        else
        {
            info.currentHP += change;
        }
    }

    /// <summary>
    /// Resets vehicle's HP to maxHP
    /// </summary>
    public void RestoreHP()
    {
        info.currentHP = info.maxHP;
    }
}
