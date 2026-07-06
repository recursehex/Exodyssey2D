using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RegionInfo
{
    public enum Tags
    {
        RuinedOutpost = 0,
        FragmentedCoast,
        GlacialDesert,
        ScorchedPlateau,
        RainforestRavines,
        VolatileVolcanoes,
        LuminousSwamp,
        RadiantCascades,
        Unknown,
    }
    public Tags Tag                 { get; private set; } = Tags.Unknown;   // Tag of region
    public string Name              { get; private set; }                   // Display name of region
    public int GridsRequired        { get; private set; } = 3;             // Number of grids to complete region
    public int GridsCompleted       { get; set; } = 0;                      // Current progress in region
    public int MinEnemySpawn        { get; private set; } = 0;              // Guaranteed minimum enemies per level
    public string Description       { get; private set; }                   // Lore description
    public Tile[] GroundTiles       { get; set; }                           // Ground tiles for this region
    public Tile[] WallTiles         { get; set; }                           // Wall tiles for this region
    public List<string> EnemyPool   { get; private set; } = new();          // Allowed enemy types for this region
    public List<string> VehiclePool { get; private set; } = new();          // Allowed vehicle tags for this region
    public Dictionary<string, int> WallWeights { get; private set; } = new(); // Per-region wall spawn weights by sprite name
    public int ForcedWildfires      { get; private set; } = 0;              // Guaranteed wildfires spawned per grid
    public bool AllowNaturalWildfire { get; private set; } = true;          // Whether the natural wildfire chance roll can occur
    public IReadOnlyList<int> ItemRarityWeightsStart { get; private set; } = DefaultItemRarityWeights; // C/L/S/R/A weights at region start
    public IReadOnlyList<int> ItemRarityWeightsEnd { get; private set; } = DefaultItemRarityWeights;   // C/L/S/R/A weights at region end
    public int AnomalousCap         { get; private set; } = 0;              // Max Anomalous items generated in this region
    private static readonly int[] DefaultItemRarityWeights = { 45, 35, 15, 4, 1 };
    [Serializable] private class Entry
    {
        public string Tag, Name, Description;
        public int GridsRequired = 3;
        public int MinEnemySpawn = 0;
        public string GroundTileSetName, WallTileSetName;
        public List<string> EnemyPool = new(), VehiclePool = new();
        public List<int> ItemRarityWeightsStart = new(), ItemRarityWeightsEnd = new();
        public int AnomalousCap = 0;
        public List<WallWeight> WallWeights = new();
        public int ForcedWildfires = 0;
        public bool AllowNaturalWildfire = true;
        public bool disabled = false;
    }
    [Serializable] private class WallWeight
    {
        public string Name;
        public int weight = 1;
    }
    [Serializable] private class EntryList { public List<Entry> Regions; }
    private static List<Entry> Database;
	private static void LoadDatabase()
	{
		if (Database != null)
			return;
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/RegionDefinitions");
		if (JsonFile != null)
			Database = JsonUtility.FromJson<EntryList>(JsonFile.text).Regions;
		else
			Debug.LogError("RegionDefinitions.json not found in Resources folder!");
	}
    /// <summary>
    /// Returns info for a desired region,
    /// index must match Tag order
    /// </summary>
    public RegionInfo(int index)
    {
        LoadDatabase();
        Tags TagData = (Tags)index;
        string TagName = TagData.ToString();
        if (Database != null)
        {
            Entry Entry = Database.Find(Entry => Entry.Tag == TagName);
            if (Entry != null && !Entry.disabled)
            {
                LoadFrom(Entry);
                return;
            }
            else if (Entry != null && Entry.disabled)
                Debug.LogWarning($"Region {index} {TagName} is disabled in JSON");
        }
        // Fallback to default values if JSON loading fails
        Debug.LogWarning($"Region {TagName} not found in JSON, using default values");
        Tag             = TagData;
        Name            = TagName.ToUpper();
        Description     = "Unknown region";
    }
    private void LoadFrom(Entry Source)
    {
        Name            = Source.Name;
        GridsRequired   = Source.GridsRequired;
        MinEnemySpawn   = Source.MinEnemySpawn;
        Description     = Source.Description;
        EnemyPool       = new(Source.EnemyPool);
        VehiclePool     = new(Source.VehiclePool);
        ForcedWildfires = Source.ForcedWildfires;
        AllowNaturalWildfire = Source.AllowNaturalWildfire;
        ItemRarityWeightsStart = ValidateWeights(Source.ItemRarityWeightsStart, Source.Tag, "ItemRarityWeightsStart");
        ItemRarityWeightsEnd = Source.ItemRarityWeightsEnd.Count == 0
            ? ItemRarityWeightsStart
            : ValidateWeights(Source.ItemRarityWeightsEnd, Source.Tag, "ItemRarityWeightsEnd");
        AnomalousCap    = Mathf.Max(0, Source.AnomalousCap);
        WallWeights     = new();
        foreach (WallWeight Weight in Source.WallWeights)
        {
            if (!string.IsNullOrEmpty(Weight.Name))
                WallWeights[Weight.Name] = Weight.weight;
        }
        Tag = Enum.TryParse(Source.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
    }
    /// <summary>
    /// Falls back to the canonical global weights when a region's table is
    /// missing, has the wrong number of tiers, or does not sum to 100
    /// </summary>
    private static IReadOnlyList<int> ValidateWeights(List<int> Weights, string regionTag, string fieldName)
    {
        if (Weights.Count != DefaultItemRarityWeights.Length)
        {
            Debug.LogWarning($"{fieldName} for region {regionTag} must have {DefaultItemRarityWeights.Length} entries, using defaults");
            return DefaultItemRarityWeights;
        }
        int sum = 0;
        foreach (int weight in Weights)
        {
            if (weight < 0)
            {
                Debug.LogWarning($"{fieldName} for region {regionTag} contains a negative weight, using defaults");
                return DefaultItemRarityWeights;
            }
            sum += weight;
        }
        if (sum != 100)
        {
            Debug.LogWarning($"{fieldName} for region {regionTag} sums to {sum}, expected 100, using defaults");
            return DefaultItemRarityWeights;
        }
        return Weights;
    }
    /// <summary>
    /// Returns item rarity weights (C/L/S/R/A order, matching Rarity.RarityList)
    /// interpolated between the region's start and end tables by progress t,
    /// clamped to [0, 1]
    /// </summary>
    public float[] GetItemRarityWeightsAt(float t)
    {
        t = Mathf.Clamp01(t);
        float[] Weights = new float[DefaultItemRarityWeights.Length];
        for (int i = 0; i < Weights.Length; i++)
            Weights[i] = Mathf.Lerp(ItemRarityWeightsStart[i], ItemRarityWeightsEnd[i], t);
        return Weights;
    }
    /// <summary>
    /// Checks if an enemy type is allowed in this region's spawn pool
    /// </summary>
    public bool IsEnemyAllowed(string enemyType) => EnemyPool.Contains(enemyType);
    /// <summary>
    /// Checks if a vehicle tag is allowed in this region's spawn pool
    /// </summary>
    public bool IsVehicleAllowed(string vehicleTag) => VehiclePool.Contains(vehicleTag);
    /// <summary>
    /// Resets grids completed counter
    /// </summary>
    public void ResetProgress() => GridsCompleted = 0;
    /// <summary>
    /// Increments grids completed counter
    /// </summary>
    public void IncrementProgress() => GridsCompleted++;
    /// <summary>
    /// Checks if region is complete
    /// </summary>
    public bool IsComplete() => GridsCompleted >= GridsRequired;
}
