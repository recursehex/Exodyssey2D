using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages all active fire tiles, handling spread, damage ticks, and lifecycle
/// </summary>
public class FireManager : MonoBehaviour
{
    [SerializeField] private Tilemap TilemapGround;
    [SerializeField] private Tilemap TilemapWalls;
    [SerializeField] private Player Player;
    [SerializeField] private EnemyManager EnemyManager;
    [SerializeField] private VehicleManager VehicleManager;
    [SerializeField] private GameObject FireTemplate;
    private const string BushSpriteResourcePath = "Sprites/bush";
    private const string BushSpriteName = "bush";
    private const string BurnedSpriteResourcePath = "Sprites/burned";
    private Sprite BushSprite;
    private Sprite BurnedSprite;
    private Tile BurnedTile;
    [Header("Behavior")]
    [SerializeField] private int lifetime = 3;
    [SerializeField] private int fireDamage = 1;
    [SerializeField] private int naturalWildfireSeeds = 2;
    [SerializeField, Range(0f, 1f)] private float naturalWildfireChance = 0.15f;
    [SerializeField] private int wildfireSpawnBudget = 2;
    [SerializeField] private int wildfireAttemptsPerSeed = 8;
    [SerializeField] private int wildfireEdgeInset = 2;
    [SerializeField] private int maxNeighborSpread = 1;
    private readonly List<Fire> ActiveFires = new();
    private readonly HashSet<Vector3Int> FireCells = new();
    private readonly HashSet<Vector3Int> BurnedCells = new();
    private readonly HashSet<Vector3Int> PendingBurnCells = new();
    private readonly HashSet<Vector3Int> PendingSpawnCells = new();
    public IReadOnlyList<Fire> Fires => ActiveFires;
    private static readonly Vector3Int[] NeighborOffsets = new Vector3Int[]
    {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
    };
    public bool HasActiveFires()
    {
        for (int i = ActiveFires.Count - 1; i >= 0; i--)
        {
            if (ActiveFires[i] == null)
                ActiveFires.RemoveAt(i);
        }
        return ActiveFires.Count > 0;
    }
    public void Initialize(Tilemap Ground, Tilemap Walls, Player Player,
                           EnemyManager EnemyManager, VehicleManager VehicleManager, GameObject FireTemplate)
    {
        TilemapGround = Ground;
        TilemapWalls = Walls;
        this.Player = Player;
        this.EnemyManager = EnemyManager;
        this.VehicleManager = VehicleManager;
        this.FireTemplate = FireTemplate;
        BushSprite = Resources.Load<Sprite>(BushSpriteResourcePath);
        if (BurnedTile == null)
        {
            if (BurnedSprite == null)
                BurnedSprite = Resources.Load<Sprite>(BurnedSpriteResourcePath);
            if (BurnedSprite != null)
            {
                BurnedTile = ScriptableObject.CreateInstance<Tile>();
                BurnedTile.sprite = BurnedSprite;
                BurnedTile.colliderType = Tile.ColliderType.None;
            }
        }
    }
    /// <summary>
    /// Clears existing fires and optionally seeds a natural wildfire when a level starts
    /// </summary>
    public void ResetForLevel(bool allowNaturalWildfire = true)
    {
        DestroyAllFires();
        if (allowNaturalWildfire)
            TrySpawnNaturalWildfire();
    }
    /// <summary>
    /// Destroys and clears all active fires
    /// </summary>
    public void DestroyAllFires()
    {
        foreach (Fire Fire in ActiveFires)
        {
            if (Fire != null)
                Destroy(Fire.gameObject);
        }
        ActiveFires.Clear();
        FireCells.Clear();
        BurnedCells.Clear();
        PendingBurnCells.Clear();
        PendingSpawnCells.Clear();
    }
    /// <summary>
    /// Checks if there is fire at a specific cell
    /// </summary>
    public bool HasFireAtCell(Vector3Int Cell) => FireCells.Contains(Cell);
    /// <summary>
    /// Checks if there is fire at a specific world position
    /// </summary>
    public bool HasFireAtWorld(Vector3 WorldPosition)
    {
        Vector3Int Cell = TilemapGround.WorldToCell(WorldPosition);
        return HasFireAtCell(Cell);
    }
    /// <summary>
    /// Extinguishes fire at a specific cell if present.
    /// Returns true if fire was found and removed
    /// </summary>
    public bool ExtinguishFire(Vector3Int Cell)
    {
        if (!FireCells.Contains(Cell))
            return false;
        int index = ActiveFires.FindIndex(Fire => Fire != null && Fire.CellPosition == Cell);
        if (index >= 0)
            RemoveFireAt(index);
        return index >= 0;
    }
    /// <summary>
    /// Spawns a new fire tile at the specified cell if valid
    /// </summary>
    public bool TrySpawnFire(Vector3Int Cell, bool isWildfire = false, bool allowBurnedCell = false)
    {
        if (FireCells.Contains(Cell)
            || (!isWildfire && !allowBurnedCell && BurnedCells.Contains(Cell))
            || !TilemapGround.cellBounds.Contains(Cell)
            || IsBlockingWall(Cell))
        {
            return false;
        }
        Vector3 WorldPosition = TilemapGround.GetCellCenterWorld(Cell);
        GameObject Instance = FireTemplate != null
            ? Instantiate(FireTemplate, WorldPosition, Quaternion.identity)
            : new GameObject("Fire");
        if (!Instance.TryGetComponent(out Fire Fire))
            Fire = Instance.AddComponent<Fire>();
        Fire.Initialize(Cell, isWildfire, lifetime, WorldPosition);
        ActiveFires.Add(Fire);
        FireCells.Add(Cell);
        if (isWildfire)
            GameManager.Instance.RegisterObjectForTileReveal(WorldPosition, Fire.transform);
        // Wildfire reclaiming a burned cell should clear the burned marker to allow full takeover
        if (isWildfire || allowBurnedCell)
            BurnedCells.Remove(Cell);
        QueueEnvironmentBurn(Cell);
        return true;
    }
    /// <summary>
    /// Returns true if the cell contains a wall tile that should block fire.
    /// </summary>
    private bool IsBlockingWall(Vector3Int Cell)
    {
        if (!TilemapWalls.HasTile(Cell))
            return false;
        return !IsBushWallTile(Cell);
    }
    /// <summary>
    /// Returns true if the wall tile at the cell uses the bush sprite.
    /// </summary>
    private bool IsBushWallTile(Vector3Int Cell)
    {
        Sprite WallSprite = TilemapWalls.GetSprite(Cell);
        if (WallSprite == null)
            return false;
        if (BushSprite != null)
            return WallSprite == BushSprite;
        return WallSprite.name == BushSpriteName;
    }
    /// <summary>
    /// Advances fire state at the start of a turn.
    /// Returns true when fire successfully spreads this tick
    /// </summary>
    public bool HandleTurnStart(bool isPlayerTurn)
    {
        if (ActiveFires.Count == 0)
            return false;
        // Only progress spread/burn on enemy turn to keep a single tick per round.
        if (!isPlayerTurn)
        {
            ResolvePendingBurns();
            ApplyStandingDamage();
            return SpreadAndBurnDown();
        }
        return false;
    }
    /// <summary>
    /// Attempts to start a natural wildfire for the current grid.
    /// Wildfires do not expire naturally and try to fill the grid
    /// </summary>
    private void TrySpawnNaturalWildfire()
    {
        if (naturalWildfireChance <= 0f || Random.value > naturalWildfireChance)
            return;
        Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
        int seeds = Mathf.Max(1, naturalWildfireSeeds);
        int attempts = seeds * wildfireAttemptsPerSeed;
        while (seeds > 0 && attempts-- > 0)
        {
            Vector3Int Cell = GetBiasedWildfireCell();
            if (IsBlockingWall(Cell)
                || FireCells.Contains(Cell)
                || Cell == PlayerCell
                || GameManager.Instance.HasExitTileAtPosition(Cell))
                continue;
            if (TrySpawnFire(Cell, true))
                seeds--;
        }
    }
    /// <summary>
    /// Queue delayed burning for items, bushes, and ground after a full turn on fire
    /// </summary>
    private void QueueEnvironmentBurn(Vector3Int Cell)
    {
        PendingBurnCells.Add(Cell);
    }
    /// <summary>
    /// Handles delayed burning for flammable items, bush walls, and ground tiles
    /// </summary>
    private void ResolvePendingBurns()
    {
        if (PendingBurnCells.Count == 0)
            return;
        List<Vector3Int> CellsToProcess = new(PendingBurnCells);
        PendingBurnCells.Clear();
        foreach (Vector3Int Cell in CellsToProcess)
        {
            if (!HasFireAtCell(Cell))
                continue;
            BurnFlammableItemAt(Cell);
            BurnBushWallAt(Cell);
            MarkBurnedGroundAt(Cell);
        }
    }
    private void BurnFlammableItemAt(Vector3Int Cell)
    {
        Vector3 World = TilemapGround.GetCellCenterWorld(Cell);
        Item ItemAtCell = GameManager.Instance.GetItemAtPosition(World);
        if (ItemAtCell != null
            && ItemAtCell.Info != null
            && ItemAtCell.Info.IsFlammable)
        {
            GameManager.Instance.RemoveItemAtPosition(ItemAtCell);
            Destroy(ItemAtCell.gameObject);
        }
    }
    private void BurnBushWallAt(Vector3Int Cell)
    {
        if (!IsBushWallTile(Cell))
            return;
        TilemapWalls.SetTile(Cell, null);
    }
    private void MarkBurnedGroundAt(Vector3Int Cell)
    {
        BurnedCells.Add(Cell);
        if (BurnedTile == null)
            return;
        TilemapGround.SetTile(Cell, BurnedTile);
    }
    /// <summary>
    /// Picks a cell guaranteed to be near the top and bottom edges
    /// </summary>
    private Vector3Int GetBiasedWildfireCell()
    {
        BoundsInt Bounds = TilemapGround.cellBounds;
        int x = Random.Range(Bounds.xMin, Bounds.xMax);
        bool pickTop = Random.value < 0.5f;
        int edgeBase = pickTop ? Bounds.yMax - 1 : Bounds.yMin;
        // Allow up to 2 tiles inward from the chosen edge
        int offset = Random.Range(0, wildfireEdgeInset + 1); // 0 to inset tiles offset
        int y = pickTop ? edgeBase - offset : edgeBase + offset;
        // Safety clamp to bounds in case the map is very small
        y = Mathf.Clamp(y, Bounds.yMin, Bounds.yMax - 1);
        return new Vector3Int(x, y, 0);
    }
    /// <summary>
    /// Applies damage to entities on fire at enemy turn start
    /// </summary>
    private void ApplyStandingDamage()
    {
        // Damage enemies on fire
        for (int i = EnemyManager.Enemies.Count - 1; i >= 0; i--)
        {
            Enemy Enemy = EnemyManager.Enemies[i];
            if (Enemy == null)
                continue;
            Vector3Int Cell = TilemapGround.WorldToCell(Enemy.transform.position);
            if (HasFireAtCell(Cell))
                EnemyManager.HandleDamageToEnemy(Enemy, fireDamage, false);
        }
        // Damage vehicles on fire
        for (int i = VehicleManager.Vehicles.Count - 1; i >= 0; i--)
        {
            Vehicle Vehicle = VehicleManager.Vehicles[i];
            if (Vehicle == null)
                continue;
            Vector3Int Cell = TilemapGround.WorldToCell(Vehicle.transform.position);
            if (HasFireAtCell(Cell))
                VehicleManager.ApplyDamageToVehicle(Vehicle, fireDamage);
        }
        // Damage player on fire if not in vehicle
        if (!Player.IsInVehicle && HasFireAtWorld(Player.transform.position))
            Player.DecreaseHealthBy(fireDamage, false);
    }
    /// <summary>
    /// Handles fire spread and burnout for all active fires
    /// </summary>
    private bool SpreadAndBurnDown()
    {
        PendingSpawnCells.Clear();
        List<(Vector3Int Cell, bool isWildfire)> NewFires = new();
        List<Fire> ExpiredFires = new();
        int wildfireBudget = wildfireSpawnBudget;
        foreach (Fire Fire in ActiveFires)
        {
            if (Fire == null)
                continue;
            if (Fire.ShouldExtinguishAfterTurn())
            {
                ExpiredFires.Add(Fire);
                continue;
            }
            int spawnAllowance = Fire.IsWildfire ? wildfireBudget : maxNeighborSpread;
            int spawned = TrySpreadFrom(Fire, NewFires, spawnAllowance);
            if (Fire.IsWildfire)
                wildfireBudget = Mathf.Max(0, wildfireBudget - spawned);
        }
        NewFires.ForEach(Fire => TrySpawnFire(Fire.Cell, Fire.isWildfire));
        ExpiredFires.ForEach(Fire => RemoveFire(Fire, true));
        return NewFires.Count > 0;
    }
    /// <summary>
    /// Attempts to spread fire from a given fire tile to neighboring cells
    /// </summary>
    private int TrySpreadFrom(Fire Fire, List<(Vector3Int Cell, bool isWildfire)> NewFires, int spawnBudget)
    {
        if (spawnBudget <= 0)
            return 0;
        // Each fire chooses a random number of tiles (0-maxNeighborSpread) to ignite if available
        List<Vector3Int> Candidates = GetNeighbors(Fire.CellPosition);
        Candidates.RemoveAll(Neighbor =>
            IsBlockingWall(Neighbor)
            || FireCells.Contains(Neighbor)
            || (!Fire.IsWildfire && BurnedCells.Contains(Neighbor)));
        if (Candidates.Count == 0)
            return 0;
        // Shuffle candidates to avoid directional bias
        for (int i = 0; i < Candidates.Count; i++)
        {
            int swapIndex = Random.Range(i, Candidates.Count);
            (Candidates[i], Candidates[swapIndex]) = (Candidates[swapIndex], Candidates[i]);
        }
        int spreadCount = Random.Range(0, maxNeighborSpread + 1); // inclusive 0-maxNeighborSpread
        spreadCount = Mathf.Min(spreadCount, Candidates.Count);
        spreadCount = Mathf.Min(spreadCount, spawnBudget);
        int spawned = 0;
        for (int i = 0; i < spreadCount; i++)
        {
            Vector3Int Target = Candidates[i];
            if (PendingSpawnCells.Add(Target))
            {
                NewFires.Add((Target, Fire.IsWildfire));
                spawned++;
            }
        }
        return spawned;
    }
    /// <summary>
    /// Gets valid neighboring cells for fire spread
    /// </summary>
    private List<Vector3Int> GetNeighbors(Vector3Int Origin)
    {
        List<Vector3Int> Neighbors = new();
        foreach (Vector3Int Offset in NeighborOffsets)
        {
            Vector3Int Candidate = Origin + Offset;
            if (TilemapGround.cellBounds.Contains(Candidate))
                Neighbors.Add(Candidate);
        }
        return Neighbors;
    }
    /// <summary>
    /// Removes a specific fire tile
    /// </summary>
    private void RemoveFire(Fire Fire, bool markBurned = false)
    {
        if (Fire == null)
            return;
        if (markBurned)
            BurnedCells.Add(Fire.CellPosition);
        FireCells.Remove(Fire.CellPosition);
        ActiveFires.Remove(Fire);
        Destroy(Fire.gameObject);
    }
    /// <summary>
    /// Removes fire at a specific index in the ActiveFires list
    /// </summary>
    private void RemoveFireAt(int index)
    {
        Fire Fire = ActiveFires[index];
        if (Fire != null)
        {
            FireCells.Remove(Fire.CellPosition);
            Destroy(Fire.gameObject);
        }
        ActiveFires.RemoveAt(index);
    }
}
