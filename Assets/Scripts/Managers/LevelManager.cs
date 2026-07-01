using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public enum TimeOfDay
    {
        Dawn = 0,
        Noon,
        Afternoon,
        Dusk,
        Night
    }

    [SerializeField] private Text RegionText;
    [SerializeField] private Text DayText;
    [SerializeField] private Text LevelText;
    [SerializeField] private GameObject LevelImage;
    public int Level { get; private set; } = 0;
    public int Day { get; private set; } = 1;
    public TimeOfDay CurrentTimeOfDay { get; private set; } = TimeOfDay.Dawn;
    private readonly float levelStartDelay = 1.5f;
    private readonly string[] timeOfDayNames = { "DAWN", "NOON", "AFTERNOON", "DUSK", "NIGHT" };
    private Tilemap TilemapGround;
    private Tilemap TilemapWalls;
    private Tilemap TilemapExits;
    private TilemapRevealAnimator TilemapRevealAnimator;
    private Tile[] GroundTiles;
    private Tile[] WallTiles;
    private Vector3Int SpawnTile;
    private readonly List<Vector3Int> GroundTilePositions = new();
    private readonly List<Vector3Int> WallTilePositions = new();
    private RegionManager RegionManager;
    public delegate void LevelInitializedDelegate();
    public event LevelInitializedDelegate OnLevelInitialized;
    public event System.Action<bool> OnLoadingScreenVisibilityChanged;
    public event System.Action<TimeOfDay> OnTimeOfDayChanged;
    public void Initialize(Tilemap Ground, Tilemap Walls, Tilemap Exits, RegionManager RegionManager,
                           Text RegionText, Text DayText, Text LevelText, GameObject LevelImage,
                           TilemapRevealAnimator TilemapRevealAnimator)
    {
        TilemapGround       = Ground;
        TilemapWalls        = Walls;
        TilemapExits        = Exits;
        this.RegionManager  = RegionManager;
        this.RegionText     = RegionText;
        this.DayText        = DayText;
        this.LevelText      = LevelText;
        this.LevelImage     = LevelImage;
        this.TilemapRevealAnimator = TilemapRevealAnimator;
    }
    public void InitializeLevel()
    {
        LevelImage.SetActive(true);
        OnLoadingScreenVisibilityChanged?.Invoke(true);
        LevelText.gameObject.SetActive(true);
        DayText.gameObject.SetActive(true);
        RegionText.gameObject.SetActive(true);
        RegionText.text = RegionManager.CurrentRegion.Name;
        DayText.text = $"DAY {Day}";
        UpdateTimeOfDay(emitEvent: true);
        SpawnTile = TilemapGround.WorldToCell(GameManager.Instance.PlayerStartPosition);
        GenerateGround();
        GenerateWalls();
        TilemapRevealAnimator.PrepareTilesForReveal(SpawnTile, GroundTilePositions, WallTilePositions);
        OnLevelInitialized?.Invoke();
        Invoke(nameof(HideLevelLoadScreen), levelStartDelay);
    }
    /// <summary>
    /// Hides level load screen after delay ends
    /// </summary>
    private void HideLevelLoadScreen()
    {
        LevelImage.SetActive(false);
        OnLoadingScreenVisibilityChanged?.Invoke(false);
        LevelText.gameObject.SetActive(false);
        DayText.gameObject.SetActive(false);
        RegionText.gameObject.SetActive(false);
    }
    /// <summary>
    /// Increments level and clears tilemaps
    /// </summary>
    public void PrepareNextLevel()
    {
        Level++;
        if (Level % timeOfDayNames.Length == 0) Day++;
        DayText.text = $"DAY {Day}";
        UpdateTimeOfDay(emitEvent: true);
        // Track region progression
        RegionManager.CompleteGrid();
        RegionManager.TryAdvanceRegion();
        ClearTilemaps();
    }
    /// <summary>
    /// Resets level/day counters and clears existing tiles for a fresh run
    /// </summary>
    public void ResetLevelProgress()
    {
        Level = 0;
        Day = 1;
        DayText.text = "DAY 1";
        UpdateTimeOfDay(emitEvent: true);
        RegionText.text = string.Empty;
        LevelImage.SetActive(false);
        LevelText.gameObject.SetActive(false);
        DayText.gameObject.SetActive(false);
        RegionText.gameObject.SetActive(false);
        ClearTilemaps();
    }
    /// <summary>
    /// Clears all tiles from TilemapGround and TileMapWalls
    /// </summary>
    private void ClearTilemaps()
    {
        TilemapRevealAnimator.ResetTileTransforms();
        TilemapGround.ClearAllTiles();
        TilemapWalls.ClearAllTiles();
        GroundTilePositions.Clear();
        WallTilePositions.Clear();
    }
    /// <summary>
    /// Generates ground tiles
    /// </summary>
    private void GenerateGround()
    {
        GroundTiles = RegionManager.GetCurrentGroundTiles();
        GroundTilePositions.Clear();
        for (int x = GameConfig.Grid.MinX; x <= GameConfig.Grid.MaxX; x++)
        {
            for (int y = GameConfig.Grid.MinY; y <= GameConfig.Grid.MaxY; y++)
            {
                Vector3Int Position = new(x, y);
                TilemapGround.SetTile(Position, GroundTiles[Random.Range(0, GroundTiles.Length)]);
                GroundTilePositions.Add(Position);
            }
        }
    }
    /// <summary>
    /// Generates walls on top of the ground tiles
    /// </summary>
    private void GenerateWalls()
    {
        WallTiles = RegionManager.GetCurrentWallTiles();
        WallTilePositions.Clear();
        MapGenerator.GenerateMap(TilemapWalls, WallTiles, RegionManager.GetCurrentWallWeights(), WallTilePositions);
    }
    /// <summary>
    /// Returns true a wall is at the given position
    /// </summary>
    public bool HasWallAtPosition(Vector3Int Position) => TilemapWalls.HasTile(Position);
    /// <summary>
    /// Returns true an exit tile is at the given position
    /// </summary>
    public bool HasExitTileAtPosition(Vector3Int Position) => TilemapExits.HasTile(Position);
    /// <summary>
    /// Displays game over screen with stats
    /// </summary>
    public void ShowGameOver()
    {
        RegionText.gameObject.SetActive(true);
        DayText.gameObject.SetActive(true);
        RegionText.text = "YOU DIED";
        DayText.text = Day == 1 ? "AFTER 1 DAY" : $"AFTER {(Level / timeOfDayNames.Length) + 1} DAYS";
        LevelImage.SetActive(true);
    }
    private void UpdateTimeOfDay(bool emitEvent)
    {
        // Radiant Cascades is locked to permanent night regardless of the day cycle
        if (RegionManager != null
            && RegionManager.CurrentRegion != null
            && RegionManager.CurrentRegionIndex == (int)RegionInfo.Tags.RadiantCascades)
            CurrentTimeOfDay = TimeOfDay.Night;
        else
            CurrentTimeOfDay = (TimeOfDay)(Level % timeOfDayNames.Length);
        LevelText.text = timeOfDayNames[(int)CurrentTimeOfDay];
        if (emitEvent)
            OnTimeOfDayChanged?.Invoke(CurrentTimeOfDay);
    }
}
