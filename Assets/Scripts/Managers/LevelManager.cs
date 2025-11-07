using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Text RegionText;
    [SerializeField] private Text DayText;
    [SerializeField] private Text LevelText;
    [SerializeField] private GameObject LevelImage;
    public int Level { get; private set; } = 0;
    public int Day { get; private set; } = 1;
    private readonly float levelStartDelay = 1.5f;
    private readonly string[] timeOfDayNames = { "DAWN", "NOON", "AFTERNOON", "DUSK", "NIGHT" };
    private Tilemap TilemapGround;
    private Tilemap TilemapWalls;
    private Tilemap TilemapExits;
    private Tile[] GroundTiles;
    private Tile[] WallTiles;
    private RegionManager RegionManager;
    public delegate void LevelInitializedDelegate();
    public event LevelInitializedDelegate OnLevelInitialized;
    public void Initialize(Tilemap Ground, Tilemap Walls, Tilemap Exits, RegionManager RegionManager,
                           Text RegionText, Text DayText, Text LevelText, GameObject LevelImage)
    {
        TilemapGround       = Ground;
        TilemapWalls        = Walls;
        TilemapExits        = Exits;
        this.RegionManager  = RegionManager;
        this.RegionText     = RegionText;
        this.DayText        = DayText;
        this.LevelText        = LevelText;
        this.LevelImage     = LevelImage;
    }
    public void InitializeLevel()
    {
        LevelImage.SetActive(true);
        LevelText.gameObject.SetActive(true);
        DayText.gameObject.SetActive(true);
        RegionText.gameObject.SetActive(true);
        RegionText.text = RegionManager.CurrentRegion.Name;
        GenerateGround();
        GenerateWalls();
        OnLevelInitialized?.Invoke();
        Invoke(nameof(HideLevelLoadScreen), levelStartDelay);
    }
    /// <summary>
    /// Hides level load screen after delay ends
    /// </summary>
    private void HideLevelLoadScreen()
    {
        LevelImage.SetActive(false);
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
        LevelText.text = $"{timeOfDayNames[Level % timeOfDayNames.Length]}";
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
        LevelText.text = timeOfDayNames[0];
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
        TilemapGround.ClearAllTiles();
        TilemapWalls.ClearAllTiles();
    }
    /// <summary>
    /// Generates ground tiles
    /// </summary>
    private void GenerateGround()
    {
        GroundTiles = RegionManager.GetCurrentGroundTiles();
        for (int x = -4; x < 5; x++)
        {
            for (int y = -4; y < 5; y++)
            {
                TilemapGround.SetTile(new(x, y), GroundTiles[Random.Range(0, GroundTiles.Length)]);
            }
        }
    }
    /// <summary>
    /// Generates walls on top of the ground tiles
    /// </summary>
    private void GenerateWalls()
    {
        WallTiles = RegionManager.GetCurrentWallTiles();
        MapGenerator.GenerateMap(TilemapWalls, WallTiles);
    }
    /// <summary>
    /// Returns true a wall is at the given position
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
    public bool HasWallAtPosition(Vector3Int Position) => TilemapWalls.HasTile(Position);
    /// <summary>
    /// Returns true an exit tile is at the given position
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
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
}