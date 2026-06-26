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
    public string Description       { get; private set; }                   // Lore description
    public Tile[] GroundTiles       { get; set; }                           // Ground tiles for this region
    public Tile[] WallTiles         { get; set; }                           // Wall tiles for this region
    public List<string> EnemyPool   { get; private set; } = new();          // Allowed enemy types for this region
    public List<string> ItemPool    { get; private set; } = new();          // Allowed item tags for this region
    public List<string> VehiclePool { get; private set; } = new();          // Allowed vehicle tags for this region
    public Dictionary<string, int> WallWeights { get; private set; } = new(); // Per-region wall spawn weights by sprite name
    [Serializable] private class Entry
    {
        public string Tag, Name, Description;
        public int GridsRequired = 3;
        public string GroundTileSetName, WallTileSetName;
        public List<string> EnemyPool = new(), ItemPool = new(), VehiclePool = new();
        public List<WallWeight> WallWeights = new();
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
        Description     = Source.Description;
        EnemyPool       = new(Source.EnemyPool);
        ItemPool        = new(Source.ItemPool);
        VehiclePool     = new(Source.VehiclePool);
        WallWeights     = new();
        foreach (WallWeight Weight in Source.WallWeights)
        {
            if (!string.IsNullOrEmpty(Weight.Name))
                WallWeights[Weight.Name] = Weight.weight;
        }
        Tag = Enum.TryParse(Source.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
    }
    /// <summary>
    /// Checks if an enemy type is allowed in this region's spawn pool
    /// </summary>
    public bool IsEnemyAllowed(string enemyType) => EnemyPool.Contains(enemyType);
    /// <summary>
    /// Checks if an item tag is allowed in this region's spawn pool
    /// </summary>
    public bool IsItemAllowed(string itemTag) => ItemPool.Contains(itemTag);
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
