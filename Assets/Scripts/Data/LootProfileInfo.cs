using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A grid archetype's loot profile: item counts, guaranteed category slots,
/// category weight multipliers, rarity shift, and map hint icons. Loaded
/// from LootTables.json alongside the director's tuning constants
/// </summary>
public class LootProfileInfo
{
	public const string DefaultProfileName = "Default";
	public string Name 			{ get; private set; }				// Archetype name, e.g. "TriageCorridor"
	public string GridType 		{ get; private set; }				// Nature, Supply, Outpost, or Boss
	public int MinItems 		{ get; private set; } = 3;			// Minimum ground-scatter items per grid
	public int MaxItems 		{ get; private set; } = 6;			// Maximum ground-scatter items per grid
	public int RarityShift 		{ get; private set; } = 0;			// -1/0/+1 nudge on the rarity roll, never bypasses gates
	public bool RollFlavorCategory { get; private set; } = false;	// If a hidden per-grid bias category is rolled
	public List<GuaranteedSlot> Guaranteed { get; private set; } = new();	// Category slots planned before random rolls
	public Dictionary<LootCategory, float> CategoryMultipliers { get; private set; } = new();
	public List<LootCategory> MapHints { get; private set; } = new();		// Honest icons for the future map view
	public class GuaranteedSlot
	{
		public LootCategory Category;
		public int count = 1;
	}
	[Serializable] private class Entry
	{
		public string Name, GridType;
		public int MinItems = 3, MaxItems = 6, RarityShift = 0;
		public bool rollFlavorCategory = false;
		public List<GuaranteedEntry> Guaranteed = new();
		public List<MultiplierEntry> CategoryMultipliers = new();
		public List<string> MapHints = new();
	}
	[Serializable] private class GuaranteedEntry
	{
		public string Category;
		public int Count = 1;
	}
	[Serializable] private class MultiplierEntry
	{
		public string Category;
		public float Multiplier = 1f;
	}
	[Serializable] private class TableFile
	{
		public LootTuning Tuning = new();
		public List<Entry> Profiles = new();
	}
	private static LootTuning TuningData;
	private static List<LootProfileInfo> Profiles;
	/// <summary>
	/// Tuning constants for the loot director
	/// </summary>
	public static LootTuning Tuning
	{
		get
		{
			LoadDatabase();
			return TuningData;
		}
	}
	/// <summary>
	/// All loot profiles defined in LootTables.json
	/// </summary>
	public static IReadOnlyList<LootProfileInfo> AllProfiles
	{
		get
		{
			LoadDatabase();
			return Profiles;
		}
	}
	/// <summary>
	/// Loads tuning and profiles from JSON file in Resources folder
	/// </summary>
	private static void LoadDatabase()
	{
		if (TuningData != null)
			return;
		TuningData = new LootTuning();
		Profiles = new List<LootProfileInfo>();
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/LootTables");
		if (JsonFile == null)
		{
			Debug.LogError("LootTables.json not found in Resources folder!");
			return;
		}
		TableFile Parsed = JsonUtility.FromJson<TableFile>(JsonFile.text);
		if (Parsed == null)
			return;
		TuningData = Parsed.Tuning ?? new LootTuning();
		foreach (Entry Entry in Parsed.Profiles)
			Profiles.Add(FromEntry(Entry));
	}
	private static LootProfileInfo FromEntry(Entry Source)
	{
		LootProfileInfo Profile = new()
		{
			Name = Source.Name,
			GridType = Source.GridType,
			MinItems = Mathf.Max(0, Source.MinItems),
			MaxItems = Mathf.Max(Source.MinItems, Source.MaxItems),
			RarityShift = Mathf.Clamp(Source.RarityShift, -1, 1),
			RollFlavorCategory = Source.rollFlavorCategory,
		};
		foreach (GuaranteedEntry Slot in Source.Guaranteed)
		{
			if (TryParseCategory(Slot.Category, Source.Name, out LootCategory Category))
				Profile.Guaranteed.Add(new GuaranteedSlot { Category = Category, count = Mathf.Max(1, Slot.Count) });
		}
		foreach (MultiplierEntry Multiplier in Source.CategoryMultipliers)
		{
			if (TryParseCategory(Multiplier.Category, Source.Name, out LootCategory Category))
				Profile.CategoryMultipliers[Category] = Mathf.Max(0f, Multiplier.Multiplier);
		}
		foreach (string HintName in Source.MapHints)
		{
			if (TryParseCategory(HintName, Source.Name, out LootCategory Category))
				Profile.MapHints.Add(Category);
		}
		return Profile;
	}
	private static bool TryParseCategory(string categoryName, string profileName, out LootCategory Category)
	{
		if (Enum.TryParse(categoryName, out Category))
			return true;
		Debug.LogWarning($"Unknown loot category '{categoryName}' in profile {profileName}");
		return false;
	}
	/// <summary>
	/// Returns the profile with the given name, falling back to Default
	/// </summary>
	public static LootProfileInfo GetProfile(string name)
	{
		LoadDatabase();
		LootProfileInfo Profile = Profiles.Find(Profile => Profile.Name == name);
		if (Profile != null)
			return Profile;
		if (name != DefaultProfileName)
			Debug.LogWarning($"Loot profile '{name}' not found, using {DefaultProfileName}");
		return Profiles.Find(Profile => Profile.Name == DefaultProfileName);
	}
	/// <summary>
	/// Returns the weight multiplier this profile applies to an item with the
	/// given categories: the product of all matching category multipliers
	/// </summary>
	public float GetMultiplierFor(IReadOnlyList<LootCategory> ItemCategories)
	{
		float multiplier = 1f;
		foreach (LootCategory Category in ItemCategories)
		{
			if (CategoryMultipliers.TryGetValue(Category, out float categoryMultiplier))
				multiplier *= categoryMultiplier;
		}
		return multiplier;
	}
}
