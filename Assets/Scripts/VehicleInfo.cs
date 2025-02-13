using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using System.Linq;

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
	public string Name { get; private set; } = "UNKNOWN";
	// Ingame description of vehicle
	public string Description { get; private set; } = "UNKNOWN";
	// Fuel used per 100 km, lower is better
	public int Efficiency { get; private set; } = 1;
	// Time to travel one tile on the grid, lower is better
	public float Time { get; private set; } = 1;
	// Number of inventory slots
	public int Storage { get; private set; } = 1;
	// Fuel capacity
	private int maxFuel = 1;
	// Current fuel
	public int CurrentFuel { get; private set; } = 1;
	// Max health
	private int maxHealth = 1;
	// Current health
	public int CurrentHealth { get; private set; } = 1;
	// If vehicle can drive offroad
	public bool CanOffroad { get; private set; } = false;
	private static readonly int lastVehicleIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> VehicleRarityList = GenerateAllRarities();
	private static List<Rarity> GenerateAllRarities()
	{
		return Enumerable.Range(0, lastVehicleIndex)
						.Select(i => VehicleFactory(i).Rarity)
						.ToList();
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity SpecifiedRarity)
	{
		var indices = Enumerable.Range(0, VehicleRarityList.Count)
								.Where(i => VehicleRarityList[i] == SpecifiedRarity)
								.ToList();
		if (indices.Count == 0)
			return -1;
		return indices[Random.Range(0, indices.Count)];
	}
	/// <summary>
	/// Decreases vehicle's CurrentHealth by amount
	/// </summary>
	public void DecreaseHealthBy(int amount)
	{
		CurrentHealth -= amount;
	}
	/// <summary>
	/// Resets vehicle's CurrentHealth to maxHealth
	/// </summary>
	public void RestoreHealth()
	{
		CurrentHealth = maxHealth;
	}
	/// <summary>
	/// Refuels vehicle by amount, subtract from input
	/// </summary>
	public void RefuelBy(ref int amount)
	{
		if (CurrentFuel + amount > maxFuel)
		{
			amount -= maxFuel - CurrentFuel;
			CurrentFuel = maxFuel;
			return;
		}
		CurrentFuel += amount;
	}
	public bool DecreaseFuelBy(int amount)
	{
		if (CurrentFuel - amount < 0)
		{
			return false;
		}
		CurrentFuel -= amount;
		return true;
	}
	/// <summary>
	/// Returns the info for a desired vehicle 
	/// </summary>
	public static VehicleInfo VehicleFactory(int n)
	{
		VehicleInfo info = new();
		switch (n)
		{
			case 0:
				info.Tag 			= Tags.Rover;
				info.Rarity 		= Rarity.Scarce;
				info.Name 			= "ROVER";
				info.Description 	= "Standard ISA vehicle";
				info.Storage 		= 2;
				info.Efficiency 	= 2;
				info.Time 			= 0.5f;
				info.maxFuel 		= 10;
				info.CurrentFuel 	= info.maxFuel;
				info.maxHealth 		= 2;
				info.CurrentHealth 	= info.maxHealth;
				break;
			case 1:
				info.Tag 			= Tags.Trailer;
				info.Rarity 		= Rarity.Scarce;
				info.Name 			= "TRAILER";
				info.Description 	= "Has a storage bay";
				info.Storage 		= 4;
				info.Efficiency 	= 3;
				info.Time 			= 1.0f;
				info.maxFuel 		= 15;
				info.CurrentFuel 	= info.maxFuel;
				info.maxHealth 		= 3;
				info.CurrentHealth 	= info.maxHealth;
				break;
			case 2:
				info.Tag 			= Tags.Buggy;
				info.Rarity 		= Rarity.Rare;
				info.Name 			= "BUGGY";
				info.Description 	= "Lightweight and efficient";
				info.Storage 		= 0;
				info.Efficiency 	= 1;
				info.Time		 	= 0.25f;
				info.maxFuel 		= 5;
				info.CurrentFuel 	= info.maxFuel;
				info.maxHealth 		= 1;
				info.CurrentHealth 	= info.maxHealth;
				info.CanOffroad 	= true;
				break;
			case 3:
				info.Tag 			= Tags.Carrier;
				info.Rarity 		= Rarity.Anomalous;
				info.Name 			= "CARRIER";
				info.Description 	= "Armored transport vehicle";
				info.Storage 		= 6;
				info.Efficiency 	= 5;
				info.Time 			= 2.0f;
				info.maxFuel 		= 20;
				info.CurrentFuel 	= info.maxFuel;
				info.maxHealth 		= 10;
				info.CurrentHealth 	= info.maxHealth;
				info.CanOffroad		= true;
				break;
		}
		return info;
	}
}
