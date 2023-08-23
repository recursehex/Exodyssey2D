using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VehicleTag
{
    // CARS
    Rover = 0,
    Trailer,
    Buggy,
    Shuttle,
    Carrier,

    Unknown,
}

public class VehicleInfo
{
    public VehicleTag tag;                      // Name of vehicle
    public Rarity rarity;                       // Rarity of vehicle

    public string name;                         // Ingame name of vehicle
    public string description;                  // Ingame desc of vehicle

    public int efficiency;                      // Higher is better
    public float time;                          // Lower is better
    public bool isLargeVehicle;                 // 0 = car, 1 = large vehicle

    public int maxSeats;                        // Max # of passengers
    public int currentSeats;

    public int maxStorage;                      // Max # of inventory slots
    public int currentStorage;

    public int maxFuel;
    public int currentFuel;

    public int maxHP = 1;
    public int currentHP = 1;

    public int maxAP = 1;
    public int currentAP = 1;

    public static int lastVehicleIdx = (int)VehicleTag.Unknown;

    static public List<Rarity> GenerateAllRarities()
    {
        List<Rarity> ret = new();

        for (int i = 0; i < lastVehicleIdx; i++)
        {
            VehicleInfo item = FactoryFromNumber(i);
            ret.Add(item.rarity);
        }
        return ret;
    }

    /// <summary>
    /// Returns the percentage for a desired rarity
    /// </summary>
    /// <returns></returns>
    public static Dictionary<Rarity, int> RarityPercentMap()
    {
        Dictionary<Rarity, int> RarityToPercentage = new()
        {
            [Rarity.Common] = 35,
            [Rarity.Limited] = 30,
            [Rarity.Scarce] = 20,
            [Rarity.Rare] = 10,
            [Rarity.Anomalous] = 5,
        };
        return RarityToPercentage;
    }

    /// <summary>
    /// Returns the info for a desired vehicle 
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static VehicleInfo FactoryFromNumber(int n)
    {
        VehicleInfo inf = new();

        switch (n)
        {
            case 0:
                inf.tag = VehicleTag.Rover;
                inf.rarity = Rarity.Scarce;
                inf.name = "ROVER";
                inf.maxSeats = 4;
                inf.maxStorage = 2;
                inf.efficiency = 2;
                inf.time = 0.5f;
                inf.isLargeVehicle = false;
                inf.maxFuel = 10;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 2;
                inf.currentHP = inf.maxHP;
                break;

            case 1:
                inf.tag = VehicleTag.Trailer;
                inf.rarity = Rarity.Scarce;
                inf.name = "TRAILER";
                inf.maxSeats = 2;
                inf.maxStorage = 4;
                inf.efficiency = 1;
                inf.time = 1.0f;
                inf.isLargeVehicle = false;
                inf.maxFuel = 15;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 3;
                inf.currentHP = inf.maxHP;
                break;

            case 2:
                inf.tag = VehicleTag.Buggy;
                inf.rarity = Rarity.Rare;
                inf.name = "BUGGY";
                inf.maxSeats = 2;
                inf.maxStorage = 0;
                inf.efficiency = 3;
                inf.time = 0.25f;
                inf.isLargeVehicle = false;
                inf.maxFuel = 5;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 1;
                inf.currentHP = inf.maxHP;
                break;

            case 3:
                inf.tag = VehicleTag.Shuttle;
                inf.rarity = Rarity.Anomalous;
                inf.name = "SHUTTLE";
                inf.maxSeats = 7;
                inf.maxStorage = 0;
                inf.efficiency = 2;
                inf.time = 1.5f;
                inf.isLargeVehicle = false;
                inf.maxFuel = 20;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 4;
                inf.currentHP = inf.maxHP;
                break;

            case 4:
                inf.tag = VehicleTag.Carrier;
                inf.rarity = Rarity.Anomalous;
                inf.name = "CARRIER";
                inf.maxSeats = 5;
                inf.maxStorage = 3;
                inf.efficiency = 1;
                inf.time = 1.0f;
                inf.isLargeVehicle = false;
                inf.maxFuel = 15;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 4;
                inf.currentHP = inf.maxHP;
                break;
        }
        return inf;
    }
}
