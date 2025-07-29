using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
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
    public delegate void LevelInitializedDelegate();
    public event LevelInitializedDelegate OnLevelInitialized;
    public void Initialize(Tilemap Ground, Tilemap Walls, Tilemap Exits, Tile[] GroundTiles, Tile[] WallTiles, 
                          Text DayText, Text LevelText, GameObject LevelImage)
    {
        TilemapGround       = Ground;
        TilemapWalls        = Walls;
        TilemapExits        = Exits;
        this.GroundTiles    = GroundTiles;
        this.WallTiles      = WallTiles;
        this.DayText        = DayText;
        this.LevelText      = LevelText;
        this.LevelImage     = LevelImage;
    }
    public void InitializeLevel()
    {
        LevelImage.SetActive(true);
        LevelText.gameObject.SetActive(true);
        DayText.gameObject.SetActive(true);
        GenerateGround();
        GenerateWalls();
        OnLevelInitialized?.Invoke();
        Invoke(nameof(HideLevelLoadScreen), levelStartDelay);
    }
    private void HideLevelLoadScreen()
    {
        LevelImage.SetActive(false);
        LevelText.gameObject.SetActive(false);
        DayText.gameObject.SetActive(false);
    }
    public void PrepareNextLevel()
    {
        Level++;
        LevelText.text = timeOfDayNames[Level % timeOfDayNames.Length];
        if (Level % 5 == 0)
        {
            Day++;
            DayText.text = $"DAY {Day}";
        }
        ClearTilemaps();
    }
    private void ClearTilemaps()
    {
        TilemapGround.ClearAllTiles();
        TilemapWalls.ClearAllTiles();
    }
    private void GenerateGround()
    {
        for (int x = -4; x < 5; x++)
        {
            for (int y = -4; y < 5; y++)
            {
                TilemapGround.SetTile(new Vector3Int(x, y, 0),
                                      GroundTiles[Random.Range(0, GroundTiles.Length)]);
            }
        }
    }
    private void GenerateWalls()
    {
        MapGeneration.GenerateMap(TilemapWalls, WallTiles);
    }
    public bool HasWallAtPosition(Vector3Int Position)
    {
        return TilemapWalls.HasTile(Position);
    }
    public bool HasExitTileAtPosition(Vector3Int Position)
    {
        return TilemapExits.HasTile(Position);
    }
    public void ShowGameOver()
    {
        DayText.gameObject.SetActive(true);
        LevelText.gameObject.SetActive(true);
        DayText.text = "YOU DIED";
        LevelText.text = Day == 1 ? "AFTER 1 DAY" : $"AFTER {(Level / 5) + 1} DAYS";
        LevelImage.SetActive(true);
    }
}