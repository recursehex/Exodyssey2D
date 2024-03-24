using System.Collections.Generic;
using UnityEngine;

public enum VehicleTag
{
    // CARS
    Rover = 0,
    Trailer,
    Buggy,

    Unknown,
}

public class VehicleInfo
{
    public VehicleTag tag;                      // Name of vehicle
    public Rarity.RarityTag rarity;             // Rarity of vehicle

    public string name;                         // Ingame name of vehicle
    public string description;                  // Ingame desc of vehicle

    public int efficiency;                      // Higher is better
    public float time;                          // Lower is better

    public int maxStorage;                      // Max # of inventory slots
    public int currentStorage;

    public int maxFuel;
    public int currentFuel;

    public int maxHP = 1;
    public int currentHP = 1;

    public int maxAP = 1;
    public int currentAP = 1;

    private static readonly int lastVehicleIndex = (int)VehicleTag.Unknown;

    private static List<Rarity.RarityTag> GenerateAllRarities()
    {
        List<Rarity.RarityTag> vehicleRarityList = new();

        for (int i = 0; i < lastVehicleIndex; i++)
        {
            VehicleInfo vehicle = FactoryFromNumber(i);
            vehicleRarityList.Add(vehicle.rarity);
        }
        return vehicleRarityList;
    }
    public static int GetRandomIndexOfSpecifiedRarity(Rarity.RarityTag specifiedRarity)
	{
		List<Rarity.RarityTag> vehicleRarityList = GenerateAllRarities();
		List<int> indicesOfSpecifiedRarity = new();
		for (int i = 0; i < vehicleRarityList.Count; i++)
		{
			if (vehicleRarityList[i] == specifiedRarity) indicesOfSpecifiedRarity.Add(i);
		}
		if (indicesOfSpecifiedRarity.Count == 0) return -1;
		int randomIndex = Random.Range(0, indicesOfSpecifiedRarity.Count);
		return indicesOfSpecifiedRarity[randomIndex];
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
                inf.rarity = Rarity.RarityTag.Scarce;
                inf.name = "ROVER";
                inf.maxStorage = 2;
                inf.efficiency = 2;
                inf.time = 0.5f;
                inf.maxFuel = 10;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 2;
                inf.currentHP = inf.maxHP;
                break;

            case 1:
                inf.tag = VehicleTag.Trailer;
                inf.rarity = Rarity.RarityTag.Scarce;
                inf.name = "TRAILER";
                inf.maxStorage = 4;
                inf.efficiency = 1;
                inf.time = 1.0f;
                inf.maxFuel = 15;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 3;
                inf.currentHP = inf.maxHP;
                break;

            case 2:
                inf.tag = VehicleTag.Buggy;
                inf.rarity = Rarity.RarityTag.Rare;
                inf.name = "BUGGY";
                inf.maxStorage = 0;
                inf.efficiency = 3;
                inf.time = 0.25f;
                inf.maxFuel = 5;
                inf.currentFuel = inf.maxFuel;
                inf.maxHP = 1;
                inf.currentHP = inf.maxHP;
                break;
        }
        return inf;
    }
}
