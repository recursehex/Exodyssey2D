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
    private RegionData Data = new();                                        // Internal data
    public Tags Tag                 { get; private set; } = Tags.Unknown;   // Tag of region
    public string Name              => Data.Name;                           // Display name of region
    public int GridsRequired        => Data.GridsRequired;                  // Number of grids to complete region
    public int GridsCompleted       { get; set; } = 0;                      // Current progress in region
    public string Description       => Data.Description;                    // Lore description
    public Tile[] GroundTiles       { get; set; }                           // Ground tiles for this region
    public Tile[] WallTiles         { get; set; }                           // Wall tiles for this region
    public List<string> EnemyPool   => Data.EnemyPool;                      // Allowed enemy types for this region
    public List<string> ItemPool    => Data.ItemPool;                       // Allowed item tags for this region
    public List<string> VehiclePool => Data.VehiclePool;                    // Allowed vehicle tags for this region
    private static RegionDatabase RegionDatabase;
	private static void LoadDatabase()
	{
		if (RegionDatabase != null)
			return;
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/RegionDefinitions");
		if (JsonFile != null)
			RegionDatabase = JsonUtility.FromJson<RegionDatabase>(JsonFile.text);
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
        // Try to load from JSON first
        if (RegionDatabase != null && RegionDatabase.Regions != null)
        {
            RegionData Data = RegionDatabase.Regions.Find(region => region.Tag == TagName);
            if (Data != null && !Data.disabled)
            {
                LoadFromData(Data);
                return;
            }
            else if (Data != null && Data.disabled)
                Debug.LogWarning($"Region {index} {TagName} is disabled in JSON");
        }
        // Fallback to default values if JSON loading fails
        Debug.LogWarning($"Region {TagName} not found in JSON, using default values");
        Tag = TagData;
        Data.Tag = TagName;
        Data.Name = TagName.ToUpper();
        Data.GridsRequired = 3;
        Data.Description = "Unknown region";
        Data.EnemyPool = new();
        Data.ItemPool = new();
        Data.VehiclePool = new();
    }
    /// <summary>
    /// Loads region data from source RegionData object
    /// </summary>
    private void LoadFromData(RegionData SourceData)
    {
        // Copy the data
        Data = new RegionData
        {
            Tag                 = SourceData.Tag,
            Name                = SourceData.Name,
            GridsRequired       = SourceData.GridsRequired,
            GroundTileSetName   = SourceData.GroundTileSetName,
            WallTileSetName     = SourceData.WallTileSetName,
            EnemyPool           = new(SourceData.EnemyPool),
            ItemPool            = new(SourceData.ItemPool),
            VehiclePool         = new(SourceData.VehiclePool),
            Description         = SourceData.Description
        };

        // Parse enum
        Tag = Enum.TryParse(Data.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
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
