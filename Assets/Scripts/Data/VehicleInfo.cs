using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
	private int maxCharge = 1;
	private int maxHealth = 1;
	public Tags Tag 			{ get; private set; } = Tags.Unknown;	// Name of vehicle
	public Rarity Rarity 		{ get; private set; } = Rarity.Common;	// Rarity of vehicle
	public Types Type 			{ get; private set; } = Types.Unknown;	// Type of vehicle
	public string Name 			{ get; private set; }					// Ingame name of vehicle
	public string Description 	{ get; private set; }					// Ingame description of vehicle
	public int Efficiency 		{ get; private set; } = 1;				// Charge used to travel to a grid, lower is better
	public float Speed 			{ get; private set; } = 2f;				// Movement speed for pathfinding
	public int MovementRange 	{ get; private set; } = 3;				// How many tiles vehicle can move per turn
	public int Storage 			{ get; private set; } = 1;				// Number of inventory slots
	public int CurrentCharge 	{ get; private set; } = 1;				// Current charge
	public int CurrentHealth 	{ get; private set; } = 1;				// Current health
	public bool CanOffroad 		{ get; private set; } = false;			// If vehicle can drive offroad
	public bool HasBattery 		{ get; private set; } = false;			// If vehicle has battery
	public bool HasSpotlight 	{ get; private set; } = false;			// If vehicle has spotlight
	public bool IsOn 			{ get; private set; } = false;			// If vehicle is turned on
	public bool CanRunOver 		{ get; private set; } = false;			// If vehicle can run over enemies
	public EnemyInfo.Types RunOverType { get; private set; } = EnemyInfo.Types.Weak; // Highest enemy type it can run over
	/// <summary>
	/// Returns true if this vehicle can run over (crush and kill) an enemy of the given type
	/// </summary>
	public bool CanRunOverType(EnemyInfo.Types Type) => CanRunOver && Type <= RunOverType;
	[Serializable] private class Entry
	{
		public string Tag, Rarity, Type, Name, Description, runOverType;
		public int efficiency = 1, movementRange = 3, storage = 1, maxCharge = 1, maxHealth = 1;
		public float speed = 2f;
		public bool canOffroad = false, hasBattery = false, hasSpotlight = false, disabled = false;
	}
	[Serializable] private class EntryList { public List<Entry> Vehicles; }
	private static readonly int lastVehicleIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> VehicleRarityList = GenerateAllRarities();
	private static List<Entry> Database;
	private static void LoadDatabase()
	{
		if (Database != null)
			return;
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/VehicleDefinitions");
		if (JsonFile != null)
			Database = JsonUtility.FromJson<EntryList>(JsonFile.text).Vehicles;
		else
			Debug.LogError("VehicleDefinitions.json not found in Resources folder!");
	}
	private static List<Rarity> GenerateAllRarities()
	{
		LoadDatabase();
		if (Database == null)
		{
			Debug.LogWarning("Database failed to load, returning empty list");
			return new();
		}
		List<Rarity> Rarities = new();
		for (int i = 0; i < lastVehicleIndex; i++)
		{
			string TagName = ((Tags)i).ToString();
			Entry Entry = Database.Find(Entry => Entry.Tag == TagName);
			if (Entry != null && !Entry.disabled)
				Rarities.Add(Rarity.Parse(Entry.Rarity));
			else if (Entry == null)
				Rarities.Add(new VehicleInfo(i).Rarity);
		}
		return Rarities;
	}
	/// <summary>
	/// Gets list of allowed rarities based on current region's vehicle pool
	/// </summary>
	public static List<Rarity> GetAllowedRarities()
	{
		RegionManager RegionManager = GameManager.Instance.GetRegionManager();
		List<string> AllowedRarityNames = RegionManager.CurrentRegion?.VehiclePool;
		// No region filtering, return all rarities
		if (AllowedRarityNames == null || AllowedRarityNames.Count == 0)
		{
			return new List<Rarity>(Rarity.RarityList);
		}
		// Convert rarity names to Rarity objects
		HashSet<Rarity> AllowedRarities = new();
		foreach (string RarityName in AllowedRarityNames)
		{
			Rarity Rarity = Rarity.Parse(RarityName);
			AllowedRarities.Add(Rarity);
		}
		return new List<Rarity>(AllowedRarities);
	}
	public static int GetRandomIndexFrom(Rarity Rarity)
	{
		List<int> Indices = Enumerable.Range(0, VehicleRarityList.Count)
									  .Where(i => VehicleRarityList[i] == Rarity)
									  .ToList();
		if (Indices.Count == 0)
			return -1;
		return Indices[UnityEngine.Random.Range(0, Indices.Count)];
	}
	/// <summary>
	/// Decreases vehicle's CurrentHealth by amount
	/// </summary>
	public void DecreaseHealthBy(int amount) => CurrentHealth -= amount;
	/// <summary>
	/// Resets vehicle's CurrentHealth to maxHealth, returns false if already at max
	/// </summary>
	public bool TryRestoreHealth()
	{
		if (CurrentHealth == maxHealth)
		{
			return false;
		}
		CurrentHealth = maxHealth;
		return true;
	}
	public bool TryRestoreHealthBy(int amount)
	{
		if (CurrentHealth == maxHealth)
		{
			return false;
		}
		CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
		return true;
	}
	/// <summary>
	/// Recharges vehicle by amount, subtract from input, returns false if already fully charged
	/// </summary>
	public bool TryRechargeBy(ref int amount)
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
	public bool TryDecreaseChargeBy(int amount)
	{
		if (CurrentCharge - amount < 0)
		{
			return false;
		}
		CurrentCharge -= amount;
		return true;
	}
	public void SetCharge(int amount) => CurrentCharge = Mathf.Clamp(amount, 0, maxCharge);
	public void SwitchIgnition() => IsOn = !IsOn;
	/// <summary>
	/// Returns info for a desired vehicle,
	/// n must match Tag order and GameManager VehicleTemplates order
	/// </summary>
	public VehicleInfo(int n, int startingFuel = -1)
	{
		LoadDatabase();
		Tags TagData = (Tags)n;
		string TagName = TagData.ToString();
		if (Database != null)
		{
			Entry Entry = Database.Find(e => e.Tag == TagName);
			if (Entry != null && !Entry.disabled)
			{
				LoadFrom(Entry);
				CurrentCharge = maxCharge;
				CurrentHealth = maxHealth;
				if (startingFuel >= 0)
					SetCharge(startingFuel);
				return;
			}
			else if (Entry != null && Entry.disabled)
			{
				Debug.LogWarning($"Vehicle {n} {TagName} is disabled in JSON");
			}
		}
		// Fallback to hardcoded values if JSON loading fails
		Debug.LogWarning($"Vehicle {TagName} not found in JSON, using default values");
		Tag 			= TagData;
		Name 			= TagName.ToUpper();
		Description 	= "Unknown vehicle";
		CurrentCharge 	= maxCharge;
		CurrentHealth 	= maxHealth;
		if (startingFuel >= 0)
			SetCharge(startingFuel);
	}
	private void LoadFrom(Entry Source)
	{
		Name 			= Source.Name;
		Description 	= Source.Description;
		Efficiency 		= Source.efficiency;
		Speed 			= Source.speed;
		MovementRange 	= Source.movementRange;
		Storage 		= Source.storage;
		maxCharge 		= Source.maxCharge;
		maxHealth 		= Source.maxHealth;
		CanOffroad 		= Source.canOffroad;
		HasBattery 		= Source.hasBattery;
		HasSpotlight 	= Source.hasSpotlight;
		// A vehicle can run over enemies up to and including the specified type (Weak < Mediocre < Strong < Exotic)
		if (!string.IsNullOrEmpty(Source.runOverType)
			&& Enum.TryParse(Source.runOverType, out EnemyInfo.Types ParsedRunOverType)
			&& ParsedRunOverType != EnemyInfo.Types.Unknown)
		{
			CanRunOver 	= true;
			RunOverType = ParsedRunOverType;
		}
		Tag 	= Enum.TryParse(Source.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
		Rarity 	= Rarity.Parse(Source.Rarity);
		Type 	= Enum.TryParse(Source.Type, out Types ParsedType) ? ParsedType : Types.Unknown;
	}
}
