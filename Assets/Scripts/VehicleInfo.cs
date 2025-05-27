using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class VehicleInfo
{
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
	public enum Types
	{
		Car,
		LargeVehicle,
		Unknown,
	}
	public Tags Tag 			= Tags.Unknown;					// Name of vehicle
	public Rarity Rarity 		= Rarity.Common;				// Rarity of vehicle
	public Types Type 			= Types.Unknown;				// Type of vehicle
	public string Name 			{ get; private set; }			// Ingame name of vehicle
	public string Description 	{ get; private set; }			// Ingame description of vehicle
	public int Efficiency 		{ get; private set; } = 1;		// Fuel used per 100 km, lower is better
	public float Speed 			{ get; private set; } = 2;		// Movement speed for pathfinding
	public int MovementRange 	{ get; private set; } = 3;		// How many tiles vehicle can move per turn
	public int Storage 			{ get; private set; } = 1;		// Number of inventory slots
	private readonly int maxFuel = 1;							// Fuel capacity
	public int CurrentFuel 		{ get; private set; } = 1;		// Current fuel
	private readonly int maxHealth = 1;							// Max health
	public int CurrentHealth 	{ get; private set; } = 1;		// Current health
	public bool CanOffroad 		{ get; private set; } = false;	// If vehicle can drive offroad
	public bool HasBattery 		{ get; private set; } = false; 	// If vehicle has battery
	public bool HasSpotlight 	{ get; private set; } = false;	// If vehicle has spotlight
	public bool IsOn 			{ get; private set; } = false;	// If vehicle is turned on
	private static readonly int lastVehicleIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> VehicleRarityList = GenerateAllRarities();
	private static List<Rarity> GenerateAllRarities()
	{
		return Enumerable.Range(0, lastVehicleIndex)
						 .Select(i => new VehicleInfo(i).Rarity)
						 .ToList();
	}
	public static int GetRandomIndexFrom(Rarity Rarity)
	{
		var indices = Enumerable.Range(0, VehicleRarityList.Count)
								.Where(i => VehicleRarityList[i] == Rarity)
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
	public void SwitchIgnition()
	{
		IsOn = !IsOn;
	}
	/// <summary>
	/// Returns info for a desired vehicle,
	/// n must match Tag order and GameManager VehicleTemplates order
	/// </summary>
	public VehicleInfo(int n)
	{
		switch (n)
		{
			case 0:
				Tag 			= Tags.Rover;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Car;
				Name 			= "ROVER";
				Description 	= "Standard ISA vehicle";
				Storage 		= 2;
				Efficiency 		= 2;
				Speed 			= 2f;
				MovementRange 	= 4;
				maxFuel 		= 10;
				CurrentFuel 	= maxFuel;
				maxHealth 		= 2;
				CurrentHealth 	= maxHealth;
				break;
			case 1:
				Tag 			= Tags.Trailer;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Car;
				Name 			= "TRAILER";
				Description 	= "Has a storage bay";
				Storage 		= 4;
				Efficiency 		= 3;
				Speed 			= 1f;
				MovementRange 	= 3;
				maxFuel 		= 15;
				CurrentFuel 	= maxFuel;
				maxHealth 		= 3;
				CurrentHealth 	= maxHealth;
				break;
			case 2:
				Tag 			= Tags.Buggy;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Car;
				Name 			= "BUGGY";
				Description 	= "Lightweight and efficient";
				Storage 		= 0;
				Efficiency 		= 1;
				Speed 			= 4f;
				MovementRange 	= 5;
				maxFuel 		= 5;
				CurrentFuel 	= maxFuel;
				maxHealth 		= 1;
				CurrentHealth 	= maxHealth;
				CanOffroad 		= true;
				break;
			case 3:
				Tag 			= Tags.Carrier;
				Rarity 			= Rarity.Anomalous;
				Type 			= Types.LargeVehicle;
				Name 			= "CARRIER";
				Description 	= "Armored transport vehicle";
				Storage 		= 6;
				Efficiency 		= 5;
				Speed 			= 0.5f;
				MovementRange 	= 2;
				maxFuel 		= 20;
				CurrentFuel 	= maxFuel;
				maxHealth 		= 10;
				CurrentHealth 	= maxHealth;
				CanOffroad		= true;
				break;
		}
	}
}
