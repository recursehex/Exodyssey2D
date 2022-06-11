using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public string vehicleName;
    public int seats;
    public int storage;
    public int efficiency; // higher is better
    public float time; // lower is better
    public int size; // 0 = small, 1 = large

    public int maxFuel;
    public int currentFuel;

    public int maxHP;
    public int currentHP;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static Vehicle VehicleFactoryFromNumber(int n)
    {
        Vehicle inf = new Vehicle();

        switch (n)
        {
            case 0:
                inf.vehicleName = "ROVER";
                inf.seats = 4;
                inf.storage = 2;
                inf.efficiency = 2;
                inf.time = 0.5f;
                inf.size = 0;
                inf.maxFuel = 10;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 2;
                inf.currentHP = inf.maxHP;
                break;

            case 1:
                inf.vehicleName = "TRAILER";
                inf.seats = 2;
                inf.storage = 4;
                inf.efficiency = 1;
                inf.time = 1.0f;
                inf.size = 0;
                inf.maxFuel = 15;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 3;
                inf.currentHP = inf.maxHP;
                break;

            case 2:
                inf.vehicleName = "BUGGY";
                inf.seats = 2;
                inf.storage = 0;
                inf.efficiency = 3;
                inf.time = 0.25f;
                inf.size = 0;
                inf.maxFuel = 5;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 1;
                inf.currentHP = inf.maxHP;
                break;

            case 3:
                inf.vehicleName = "SHUTTLE";
                inf.seats = 7;
                inf.storage = 0;
                inf.efficiency = 2;
                inf.time = 1.5f;
                inf.size = 0;
                inf.maxFuel = 20;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 4;
                inf.currentHP = inf.maxHP;
                break;

            case 4:
                inf.vehicleName = "CARRIER";
                inf.seats = 5;
                inf.storage = 3;
                inf.efficiency = 1;
                inf.time = 1.0f;
                inf.size = 0;
                inf.maxFuel = 15;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 4;
                inf.currentHP = inf.maxHP;
                break;

            case 5:
                inf.vehicleName = "MECH";
                inf.seats = 3;
                inf.storage = 1;
                inf.efficiency = 1;
                inf.time = 1.0f;
                inf.size = 1;
                inf.maxFuel = 15;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 4;
                inf.currentHP = inf.maxHP;
                break;

            case 6:
                inf.vehicleName = "CRAWLER";
                inf.seats = 6;
                inf.storage = 4;
                inf.efficiency = 1;
                inf.time = 1.5f;
                inf.size = 1;
                inf.maxFuel = 15;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 6;
                inf.currentHP = inf.maxHP;
                break;
        }
        return inf;
    }

    /// <summary>
    /// Changes vehicle's HP, use negative to decrease
    /// </summary>
    public void ChangeHealth(int change)
    {
        if (currentHP + change > maxHP) // if new HP is greater than max
        {
            currentHP = maxHP;
        }
        else
        {
            currentHP += change;
        }
    }

    /// <summary>
    /// Resets vehicle's HP to maxHP
    /// </summary>
    public void RestoreHP()
    {
        currentHP = maxHP;
    }
}
