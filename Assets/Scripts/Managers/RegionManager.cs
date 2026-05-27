using UnityEngine;
using UnityEngine.Tilemaps;

public class RegionManager : MonoBehaviour
{
    public static RegionInfo CurrentRegion { get; private set; }
    public int CurrentRegionIndex { get; private set; } = 0;
    // Tile arrays for each region
    [Header("Fragmented Coast Tiles")]
    [SerializeField] private Tile[] FC_GroundTiles;
    [SerializeField] private Tile[] FC_WallTiles;
    [Header("Glacial Desert Tiles")]
    [SerializeField] private Tile[] GD_GroundTiles;
    [SerializeField] private Tile[] GD_WallTiles;
    [Header("Scorched Plateau Tiles")]
    [SerializeField] private Tile[] SP_GroundTiles;
    [SerializeField] private Tile[] SP_WallTiles;
    [Header("Rainforest Ravines Tiles")]
    [SerializeField] private Tile[] RR_GroundTiles;
    [SerializeField] private Tile[] RR_WallTiles;
    [Header("Volatile Volcanoes Tiles")]
    [SerializeField] private Tile[] VV_GroundTiles;
    [SerializeField] private Tile[] VV_WallTiles;
    [Header("Luminous Swamp Tiles")]
    [SerializeField] private Tile[] LS_GroundTiles;
    [SerializeField] private Tile[] LS_WallTiles;
    [Header("Radiant Cascades Tiles")]
    [SerializeField] private Tile[] RC_GroundTiles;
    [SerializeField] private Tile[] RC_WallTiles;
    public delegate void RegionChangedDelegate(RegionInfo newRegion);
    public event RegionChangedDelegate OnRegionChanged;
    /// <summary>
    /// Initializes the region system with the starting region
    /// </summary>
    public void Initialize() => LoadRegion(CurrentRegionIndex);
    /// <summary>
    /// Resets region progress back to the first region
    /// </summary>
    public void ResetRegionProgress()
    {
        CurrentRegionIndex = 0;
        LoadRegion(CurrentRegionIndex);
        CurrentRegion.ResetProgress();
        OnRegionChanged?.Invoke(CurrentRegion);
    }
    /// <summary>
    /// Loads a region by index and assigns appropriate tiles
    /// </summary>
    private void LoadRegion(int regionIndex)
    {
        CurrentRegion = new(regionIndex);
        CurrentRegionIndex = regionIndex;
        // Assign tiles based on region
        AssignTilesToRegion();
    }
    /// <summary>
    /// Assigns the correct tile arrays to the current region
    /// </summary>
    private void AssignTilesToRegion()
    {
        switch (CurrentRegion.Tag)
        {
            // TEMP while only one region is implemented
            // case <= RegionInfo.Tags.FragmentedCoast:
            //     CurrentRegion.GroundTiles = FC_GroundTiles;
            //     CurrentRegion.WallTiles = FC_WallTiles;
            //     break;
            // case RegionInfo.Tags.GlacialDesert:
            //     CurrentRegion.GroundTiles = GD_GroundTiles;
            //     CurrentRegion.WallTiles = GD_WallTiles;
            //     break;
            // case RegionInfo.Tags.ScorchedPlateau:
            //     CurrentRegion.GroundTiles = SP_GroundTiles;
            //     CurrentRegion.WallTiles = SP_WallTiles;
            //     break;
            // case RegionInfo.Tags.RainforestRavines:
            //     CurrentRegion.GroundTiles = RR_GroundTiles;
            //     CurrentRegion.WallTiles = RR_WallTiles;
            //     break;
            // case RegionInfo.Tags.VolatileVolcanoes:
            //     CurrentRegion.GroundTiles = VV_GroundTiles;
            //     CurrentRegion.WallTiles = VV_WallTiles;
            //     break;
            // case RegionInfo.Tags.LuminousSwamp:
            //     CurrentRegion.GroundTiles = LS_GroundTiles;
            //     CurrentRegion.WallTiles = LS_WallTiles;
            //     break;
            // case RegionInfo.Tags.RadiantCascades:
            //     CurrentRegion.GroundTiles = RC_GroundTiles;
            //     CurrentRegion.WallTiles = RC_WallTiles;
            //     break;
            default:
                // Fallback to first available tiles
                CurrentRegion.GroundTiles = FC_GroundTiles;
                CurrentRegion.WallTiles = FC_WallTiles;
                break;
        }
    }
    /// <summary>
    /// Increments grid completion for current region
    /// </summary>
    public void CompleteGrid()
    {
        CurrentRegion.IncrementProgress();
        Debug.Log($"Grid completed in {CurrentRegion.Name}: {CurrentRegion.GridsCompleted}/{CurrentRegion.GridsRequired}");
    }
    /// <summary>
    /// Checks if current region is complete and advances to next region if so, returning true
    /// </summary>
    public bool TryAdvanceRegion()
    {
        if (!CurrentRegion.IsComplete())
            return false;
        // Advance to next region
        CurrentRegionIndex++;
        // Check if all regions completed
        if (CurrentRegionIndex >= (int)RegionInfo.Tags.Unknown)
        {
            Debug.Log("All regions completed!");
            // Trigger game end
            GameManager.Instance.GameOver();
            return false;
        }
        LoadRegion(CurrentRegionIndex);
        OnRegionChanged?.Invoke(CurrentRegion);
        return true;
    }
    /// <summary>
    /// Returns current region's ground tiles
    /// </summary>
    public Tile[] GetCurrentGroundTiles() => CurrentRegion?.GroundTiles;
    /// <summary>
    /// Returns current region's wall tiles
    /// </summary>
    public Tile[] GetCurrentWallTiles() => CurrentRegion?.WallTiles;
    /// <summary>
    /// Checks if an enemy type is allowed in the current region
    /// </summary>
    public bool IsEnemyAllowedInRegion(string EnemyType) => CurrentRegion?.IsEnemyAllowed(EnemyType) ?? false;
    /// <summary>
    /// Checks if an item is allowed in the current region
    /// </summary>
    public bool IsItemAllowedInRegion(string itemTag) => CurrentRegion?.IsItemAllowed(itemTag) ?? false;
    /// <summary>
    /// Checks if a vehicle is allowed in the current region
    /// </summary>
    public bool IsVehicleAllowedInRegion(string vehicleTag) => CurrentRegion?.IsVehicleAllowed(vehicleTag) ?? false;
}
