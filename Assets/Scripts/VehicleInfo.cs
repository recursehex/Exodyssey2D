using System.Collections.Generic;
using UnityEngine;
using Exodyssey.Rarity;

public enum Tags
{
	// CARS
	Rover = 0,
	Trailer,
	Buggy,
	// LARGE VEHICLES
	Carrier,

	Unknown,
}
public class VehicleInfo
{
	// Name of vehicle
	public Tags Tag = Tags.Unknown;
	// Rarity of vehicle
	public Rarity Rarity = Rarity.Common;
	// Ingame name of vehicle
	public string name = "UNKNOWN";
	// Ingame description of vehicle
	public string description = "UNKNOWN";
	// Fuel efficiency on the map, higher is better
	public int efficiency;
	// Time to travel one tile on the grid, lower is better
	public float time;
	// Number of inventory slots
	public int storage;
	// Fuel capacity
	public int maxFuel = 1;
	// Current fuel
	public int currentFuel;
	// Max health
	public int maxHealth = 1;
	// Current health
	public int currentHealth = 1;
	// If vehicle can drive offroad
	public bool canOffroad = false;
	private static readonly int lastVehicleIndex = (int)Tags.Unknown;
	private static List<Rarity> GenerateAllRarities()
	{
		List<Rarity> VehicleRarityList = new();
		for (int i = 0; i < lastVehicleIndex; i++)
		{
			VehicleInfo Vehicle = FactoryFromNumber(i);
			VehicleRarityList.Add(Vehicle.Rarity);
		}
		return VehicleRarityList;
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity SpecifiedRarity)
	{
		List<Rarity> VehicleRarityList = GenerateAllRarities();
		List<int> IndicesOfSpecifiedRarity = new();
		for (int i = 0; i < VehicleRarityList.Count; i++)
		{
			if (VehicleRarityList[i] == SpecifiedRarity)
			{
				IndicesOfSpecifiedRarity.Add(i);
			}
		}
		if (IndicesOfSpecifiedRarity.Count == 0)
		{
			return -1;
		}
		int randomIndex = Random.Range(0, IndicesOfSpecifiedRarity.Count);
		return IndicesOfSpecifiedRarity[randomIndex];
	}
	/// <summary>
	/// Returns the info for a desired vehicle 
	/// </summary>
	public static VehicleInfo FactoryFromNumber(int n)
	{
		VehicleInfo info = new();
		switch (n)
		{
			case 0:
				info.Tag = Tags.Rover;
				info.Rarity = Rarity.Scarce;
				info.name = "ROVER";
				info.description = "Standard ISA vehicle for long range exploration";
				info.storage = 2;
				info.efficiency = 2;
				info.time = 0.5f;
				info.maxFuel = 10;
				info.currentFuel = info.maxFuel;
				info.maxHealth = 2;
				info.currentHealth = info.maxHealth;
				break;
			case 1:
				info.Tag = Tags.Trailer;
				info.Rarity = Rarity.Scarce;
				info.name = "TRAILER";
				info.description = "Has a storage bay to transport supplies";
				info.storage = 4;
				info.efficiency = 1;
				info.time = 1.0f;
				info.maxFuel = 15;
				info.currentFuel = info.maxFuel;
				info.maxHealth = 3;
				info.currentHealth = info.maxHealth;
				break;
			case 2:
				info.Tag = Tags.Buggy;
				info.Rarity = Rarity.Rare;
				info.name = "BUGGY";
				info.description = "Lightweight and efficient, built for scouting missions";
				info.storage = 0;
				info.efficiency = 3;
				info.time = 0.25f;
				info.maxFuel = 5;
				info.currentFuel = info.maxFuel;
				info.maxHealth = 1;
				info.currentHealth = info.maxHealth;
				info.canOffroad = true;
				break;
			case 3:
				info.Tag = Tags.Carrier;
				info.Rarity = Rarity.Anomalous;
				info.name = "CARRIER";
				info.description = "An armored transport vehicle, protecting its crew";
				info.storage = 4;
				info.efficiency = 1;
				info.time = 2.0f;
				info.maxFuel = 20;
				info.currentFuel = info.maxFuel;
				info.maxHealth = 10;
				info.currentHealth = info.maxHealth;
				info.canOffroad = true;
				break;
		}
		return info;
	}
}
