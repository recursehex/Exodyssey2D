using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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
	private VehicleData Data = new();									// Internal data storage
	public Tags Tag 			{ get; private set; } = Tags.Unknown;	// Name of vehicle
	public Rarity Rarity 		{ get; private set; } = Rarity.Common;	// Rarity of vehicle
	public Types Type 			{ get; private set; } = Types.Unknown;	// Type of vehicle
	public string Name 			=> Data.Name;							// Ingame name of vehicle
	public string Description 	=> Data.Description;					// Ingame description of vehicle
	public int Efficiency 		=> Data.efficiency;						// Charge used per 100 km, lower is better
	public float Speed 			=> Data.speed;							// Movement speed for pathfinding
	public int MovementRange 	=> Data.movementRange;					// How many tiles vehicle can move per turn
	public int Storage 			=> Data.storage;						// Number of inventory slots
	public int CurrentCharge 	{ get; private set; } = 1;				// Current charge
	public int CurrentHealth 	{ get; private set; } = 1;				// Current health
	public bool CanOffroad 		=> Data.canOffroad;						// If vehicle can drive offroad
	public bool HasBattery 		=> Data.hasBattery;						// If vehicle has battery
	public bool HasSpotlight 	=> Data.hasSpotlight;					// If vehicle has spotlight
	public bool IsOn 			{ get; private set; } = false;			// If vehicle is turned on
	private static readonly int lastVehicleIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> VehicleRarityList = GenerateAllRarities();
	private static VehicleDatabase VehicleDatabase;
	private static bool databaseLoaded = false;
	private static void LoadDatabase()
	{
		if (databaseLoaded)
		{
			return;
		}
		TextAsset jsonFile = Resources.Load<TextAsset>("VehicleDefinitions");
		if (jsonFile != null)
		{
			VehicleDatabase = JsonUtility.FromJson<VehicleDatabase>(jsonFile.text);
			databaseLoaded = true;
		}
		else
		{
			Debug.LogError("VehicleDefinitions.json not found in Resources folder!");
		}
	}
	
	private static List<Rarity> GenerateAllRarities()
	{
		LoadDatabase();
		List<Rarity> rarities = new();
		// First, try to get rarities from JSON
		if (VehicleDatabase != null
		 && VehicleDatabase.Vehicles != null)
		{
			for (int i = 0; i < lastVehicleIndex; i++)
			{
				Tags tag = (Tags)i;
				string tagName = tag.ToString();
				VehicleData data = VehicleDatabase.Vehicles.Find(vehicle => vehicle.Tag == tagName);
				if (data != null && !data.disabled)
				{
					Rarity parsedRarity = Rarity.Parse(data.Rarity);
					rarities.Add(parsedRarity);
				}
				else if (data != null && data.disabled)
				{
					// Skip disabled items
					continue;
				}
				else
				{
					// Fallback to creating VehicleInfo if not found in JSON
					rarities.Add(new VehicleInfo(i).Rarity);
				}
			}
		}
		else
		{
			Debug.LogWarning($"Database failed to load, returning empty list");
		}
		return rarities;
	}
	public static int GetRandomIndexFrom(Rarity Rarity)
	{
		List<int> indices = Enumerable.Range(0, VehicleRarityList.Count)
									  .Where(i => VehicleRarityList[i] == Rarity)
									  .ToList();
		if (indices.Count == 0)
			return -1;
		return indices[UnityEngine.Random.Range(0, indices.Count)];
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
		if (CurrentHealth == Data.maxHealth)
		{
			return false;
		}
		CurrentHealth = Data.maxHealth;
		return true;
	}
	/// <summary>
	/// Recharges vehicle by amount, subtract from input
	/// </summary>
	public bool RechargeBy(ref int amount)
	{
		// Return false if vehicle is already fully charged
		if (CurrentCharge == Data.maxCharge)
		{
			return false;
		}
		// Calculate how much charge to add
		int chargeToAdd = Mathf.Clamp(amount, 0, Data.maxCharge - CurrentCharge);
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
		LoadDatabase();
		
		Tags TagData = (Tags)n;
		string TagName = TagData.ToString();
		
		// Try to load from JSON first
		if (VehicleDatabase != null
		 && VehicleDatabase.Vehicles != null)
		{
			VehicleData Data = VehicleDatabase.Vehicles.Find(Vehicle => Vehicle.Tag == TagName);
			if (Data != null && !Data.disabled)
			{
				LoadFromData(Data);
				CurrentCharge = Data.maxCharge;
				CurrentHealth = Data.maxHealth;
				return;
			}
			else if (Data != null && Data.disabled)
			{
				Debug.LogWarning($"Vehicle {n} {TagName} is disabled in JSON");
			}
		}
		
		// Fallback to hardcoded values if JSON loading fails
		Debug.LogWarning($"Vehicle {TagName} not found in JSON, using default values");
		
		// Set minimal defaults for unknown vehicles
		Tag 				= TagData;
		Rarity 				= Rarity.Common;
		Type 				= Types.Unknown;
		Data.Tag 			= TagName;
		Data.Name 			= TagName.ToUpper();
		Data.Description 	= "Unknown vehicle";
		Data.maxCharge 		= 1;
		Data.maxHealth 		= 1;
		CurrentCharge 		= Data.maxCharge;
		CurrentHealth 		= Data.maxHealth;
	}
	
	private void LoadFromData(VehicleData SourceData)
	{
		// Copy the data
		Data = new VehicleData
		{
			Tag 			= SourceData.Tag,
			Rarity 			= SourceData.Rarity,
			Type 			= SourceData.Type,
			Name 			= SourceData.Name,
			Description 	= SourceData.Description,
			efficiency 		= SourceData.efficiency,
			speed 			= SourceData.speed,
			movementRange 	= SourceData.movementRange,
			storage 		= SourceData.storage,
			maxCharge 		= SourceData.maxCharge,
			maxHealth 		= SourceData.maxHealth,
			canOffroad 		= SourceData.canOffroad,
			hasBattery 		= SourceData.hasBattery,
			hasSpotlight 	= SourceData.hasSpotlight
		};
		
		// Parse enums
		if (Enum.TryParse(Data.Tag, out Tags ParsedTag))
			Tag = ParsedTag;
		else
			Tag = Tags.Unknown;
			
		Rarity = Rarity.Parse(Data.Rarity);
			
		if (Enum.TryParse(Data.Type, out Types ParsedType))
			Type = ParsedType;
		else
			Type = Types.Unknown;
	}
}
