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
	public int Efficiency 		{ get; private set; } = 1;		// Charge used per 100 km, lower is better
	public float Speed 			{ get; private set; } = 2;		// Movement speed for pathfinding
	public int MovementRange 	{ get; private set; } = 3;		// How many tiles vehicle can move per turn
	public int Storage 			{ get; private set; } = 1;		// Number of inventory slots
	private readonly int maxCharge = 1;							// Charge capacity
	public int CurrentCharge 		{ get; private set; } = 1;		// Current charge
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
		List<int> indices = Enumerable.Range(0, VehicleRarityList.Count)
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
	public bool RestoreHealth()
	{
		if (CurrentHealth == maxHealth)
		{
			return false;
		}
		CurrentHealth = maxHealth;
		return true;
	}
	/// <summary>
	/// Recharges vehicle by amount, subtract from input
	/// </summary>
	public bool RechargeBy(ref int amount)
	{
		// Return false if vehicle is already fully charged
		if (CurrentCharge == maxCharge)
		{
			return false;
		}
		// Calculate how much charge to add
		int chargeToAdd = Mathf.Clamp(amount, 0, maxCharge - CurrentCharge);
		// Add charge to vehicle
		CurrentCharge += chargeToAdd;
		// Amount will be subtracted from item durability
		amount = chargeToAdd;
		return true;
	}
	public bool DecreaseChargeBy(int amount)
	{
		if (CurrentCharge - amount < 0)
		{
			return false;
		}
		CurrentCharge -= amount;
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
		Tags tag = (Tags)n;
		switch (tag)
		{
			case Tags.Rover:
				Tag = Tags.Rover;
				Rarity = Rarity.Scarce;
				Type = Types.Car;
				Name = "ROVER";
				Description = "Standard ISA vehicle";
				Storage = 2;
				Efficiency = 2;
				Speed = 2f;
				MovementRange = 4;
				maxCharge = 10;
				CurrentCharge = maxCharge;
				maxHealth = 2;
				CurrentHealth = maxHealth;
				break;
			case Tags.Trailer:
				Tag = Tags.Trailer;
				Rarity = Rarity.Scarce;
				Type = Types.Car;
				Name = "TRAILER";
				Description = "Has a storage bay";
				Storage = 4;
				Efficiency = 3;
				Speed = 1f;
				MovementRange = 3;
				maxCharge = 15;
				CurrentCharge = maxCharge;
				maxHealth = 3;
				CurrentHealth = maxHealth;
				break;
			case Tags.Buggy:
				Tag = Tags.Buggy;
				Rarity = Rarity.Rare;
				Type = Types.Car;
				Name = "BUGGY";
				Description = "Lightweight and efficient";
				Storage = 0;
				Efficiency = 1;
				Speed = 4f;
				MovementRange = 5;
				maxCharge = 5;
				CurrentCharge = maxCharge;
				maxHealth = 1;
				CurrentHealth = maxHealth;
				CanOffroad = true;
				break;
			case Tags.Carrier:
				Tag = Tags.Carrier;
				Rarity = Rarity.Anomalous;
				Type = Types.LargeVehicle;
				Name = "CARRIER";
				Description = "Armored transport vehicle";
				Storage = 6;
				Efficiency = 5;
				Speed = 0.5f;
				MovementRange = 2;
				maxCharge = 20;
				CurrentCharge = maxCharge;
				maxHealth = 10;
				CurrentHealth = maxHealth;
				CanOffroad = true;
				break;
		}
	}
}
