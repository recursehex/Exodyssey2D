using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public GameObject TileDot;
    [SerializeField] private GameObject TileAreaTemplate;
    [SerializeField] private GameObject TargetTemplate;
    private readonly List<GameObject> TileAreas = new();
    private readonly List<GameObject> Targets = new();
    private readonly Stack<GameObject> TileAreaPool = new();
    private readonly Stack<GameObject> TargetPool = new();
    private Dictionary<Vector3Int, Node> TileAreasToDraw = null;
    public System.Action OnMarkersChanged;
    public void Initialize(GameObject TileDot, GameObject TileArea, GameObject TargetTemplate)
    {
        this.TileDot        = TileDot;
        TileAreaTemplate    = TileArea;
        this.TargetTemplate = TargetTemplate;
    }
    /// <summary>
    /// Clears all tile areas
    /// </summary>
    public void ClearTileAreas()
    {
        if (!ClearTileAreasInternal())
            return;
        NotifyMarkersChanged();
    }
    /// <summary>
    /// Draws tile areas based on areas to draw
    /// </summary>
    public void DrawTileAreas(Dictionary<Vector3Int, Node> AreasToDraw)
    {
        // Need to clear previous areas
        bool hadAreas = ClearTileAreasInternal();
        TileAreasToDraw = AreasToDraw;
        // Return if no areas to draw
        if (TileAreasToDraw == null || TileAreasToDraw.Count <= 0)
        {
            if (hadAreas)
                NotifyMarkersChanged();
            return;
        }
        // Draw new areas
        foreach (KeyValuePair<Vector3Int, Node> TileAreaPosition in TileAreasToDraw)
        {
            Vector3 ShiftedDistance = TileAreaPosition.Value.Position + new Vector3(0.5f, 0.5f);
            GameObject TileArea = SpawnMarker(TileAreaPool, TileAreaTemplate, ShiftedDistance);
            TileAreas.Add(TileArea);
        }
        NotifyMarkersChanged();
    }
    /// <summary>
    /// Clears all targets
    /// </summary>
    public void ClearTargets()
    {
        if (!ClearTargetsInternal())
            return;
        NotifyMarkersChanged();
    }
    /// <summary>
    /// Draws targets for enemies in range and line of sight
    /// </summary>
    public void DrawTargets(List<Enemy> Enemies, Vector3 PlayerPosition, int weaponRange, bool isStunning, Tilemap Walls)
    {
        // Need to clear previous targets
        bool hadTargets = ClearTargetsInternal();
        // Return if weapon has no range
        if (weaponRange <= 0)
        {
            if (hadTargets)
                NotifyMarkersChanged();
            return;
        }
        // Draw new targets
        foreach (Enemy Enemy in Enemies)
        {
            // Skip enemies that are stunned and if weapon is stunning
            if (Enemy.StunIcon.activeSelf && isStunning)
                continue;
            Vector3 EnemyPosition = Enemy.transform.position;
            // Draw targets if enemy is in line of sight and range
            if (IsInLineOfSight(PlayerPosition, EnemyPosition, weaponRange, Walls))
            {
                GameObject Target = SpawnMarker(TargetPool, TargetTemplate, EnemyPosition);
                Targets.Add(Target);
            }
        }
        if (Targets.Count > 0 || hadTargets)
            NotifyMarkersChanged();
    }
    /// <summary>
    /// Calculates firestarter area using adjacency or line of sight
    /// </summary>
    public Dictionary<Vector3Int, Node> CalculateFirestarterArea(Vector3 PlayerPosition, Vector3Int PlayerCell, int range, bool useLineOfSight, Tilemap Walls, BoundsInt Bounds, System.Func<Vector3Int, bool> IsValidTarget)
    {
        if (IsValidTarget == null)
            return null;
        Dictionary<Vector3Int, Node> Area = new();
        if (useLineOfSight)
        {
            if (range <= 0)
                return Area;
            for (int x = Bounds.xMin; x < Bounds.xMax; x++)
            {
                for (int y = Bounds.yMin; y < Bounds.yMax; y++)
                {
                    Vector3Int Cell = new(x, y);
                    if (Cell == PlayerCell)
                        continue;
                    if (!IsValidTarget(Cell))
                        continue;
                    Vector3 WorldPosition = Cell + new Vector3(0.5f, 0.5f);
                    if (!IsInLineOfSight(PlayerPosition, WorldPosition, range, Walls))
                        continue;
                    Area[Cell] = new Node(Cell);
                }
            }
            return Area;
        }
        Vector3Int[] Offsets = new Vector3Int[]
        {
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
            new(0, -1, 0),
        };
        foreach (Vector3Int Offset in Offsets)
        {
            Vector3Int Cell = PlayerCell + Offset;
            if (!Bounds.Contains(Cell))
                continue;
            if (!IsValidTarget(Cell))
                continue;
            Area[Cell] = new Node(Cell);
        }
        return Area;
    }
    /// <summary>
    /// Checks if enemy is in line of sight and range
    /// </summary>
    private bool IsInLineOfSight(Vector3 PlayerPosition, Vector3 EnemyPosition, int weaponRange, Tilemap Walls)
    {
        float distance = Vector3.Distance(PlayerPosition, EnemyPosition);
        // Return if enemy is out of range
        if (distance > weaponRange)
            return false;
        // Check if there are walls in the line of sight
        List<Vector3Int> Path = BresenhamsAlgorithm(PlayerPosition, EnemyPosition);
        foreach (Vector3Int Position in Path)
        {
            if (Walls.HasTile(Position))
                return false;
        }
        return true;
    }
    /// <summary>
    /// Returns true if a wall cell is within range and line of sight from a position,
    /// allowing the target wall itself as the endpoint while blocking on any wall in between
    /// </summary>
    public bool HasLineOfSightToWall(Vector3 FromPosition, Vector3Int WallCell, int range, Tilemap Walls)
    {
        Vector3 WallCenter = WallCell + new Vector3(0.5f, 0.5f);
        if (Vector3.Distance(FromPosition, WallCenter) > range)
            return false;
        List<Vector3Int> Path = BresenhamsAlgorithm(FromPosition, WallCenter);
        foreach (Vector3Int Position in Path)
        {
            if (Position == WallCell)
                continue;
            if (Walls.HasTile(Position))
                return false;
        }
        return true;
    }
    /// <summary>
    /// Bresenham's Line Algorithm to get cells between two positions
    /// </summary>
    public static List<Vector3Int> BresenhamsAlgorithm(Vector3 Start, Vector3 End)
    {
        // Ensure Start and End are Vector3Int
        Vector3Int StartInt = Vector3Int.FloorToInt(Start);
        Vector3Int EndInt   = Vector3Int.FloorToInt(End);
        int x0 = StartInt.x;
        int y0 = StartInt.y;
        int x1 = EndInt.x;
        int y1 = EndInt.y;
        // Initialize list to hold points on the line
        List<Vector3Int> PointsOnLine = new();
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;
        // Bresenham's algorithm loop
        while (true)
        {
            PointsOnLine.Add(new(x0, y0, 0));
            if ((x0 == x1) && (y0 == y1))
                break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        return PointsOnLine;
    }
    /// <summary>
    /// Returns true if position is in movement range
    /// </summary>
    public bool IsInTileArea(Vector3Int Position) => TileAreasToDraw?.ContainsKey(Position) == true;
    /// <summary>
    /// Returns true if position is in ranged weapon range
    /// </summary>
    public bool IsInRangedWeaponRange(Vector3 Position) => Targets.Exists(Target => Target.transform.position == Position);
    /// <summary>
    /// Destroys all active and pooled markers (tile areas & tracers)
    /// </summary>
    public void DestroyAllMarkers()
    {
        bool hadMarkers = TileAreas.Count > 0 || Targets.Count > 0 || TileAreasToDraw != null;
        DestroyMarkerCollection(TileAreas, TileAreaPool);
        DestroyMarkerCollection(Targets, TargetPool);
        TileAreasToDraw = null;
        if (hadMarkers)
            NotifyMarkersChanged();
    }
    private void NotifyMarkersChanged() => OnMarkersChanged?.Invoke();
    private bool ClearTileAreasInternal()
    {
        bool hadAreas = TileAreas.Count > 0;
        if (hadAreas)
            RecycleMarkers(TileAreas, TileAreaPool);
        TileAreasToDraw = null;
        return hadAreas;
    }
    private bool ClearTargetsInternal()
    {
        if (Targets.Count == 0)
            return false;
        RecycleMarkers(Targets, TargetPool);
        return true;
    }
    /// <summary>
    /// Reuses existing marker instances when possible
    /// </summary>
    private GameObject SpawnMarker(Stack<GameObject> Pool, GameObject Template, Vector3 Position)
    {
        GameObject Marker;
        if (Pool.Count > 0)
        {
            Marker = Pool.Pop();
            Marker.transform.SetPositionAndRotation(Position, Quaternion.identity);
            Marker.SetActive(true);
        }
        else Marker = Instantiate(Template, Position, Quaternion.identity);
        return Marker;
    }
    /// <summary>
    /// Returns active markers to the pool by disabling them
    /// </summary>
    private static void RecycleMarkers(List<GameObject> ActiveMarkers, Stack<GameObject> Pool)
    {
        foreach (GameObject Marker in ActiveMarkers)
        {
            if (Marker == null)
                continue;
            Marker.SetActive(false);
            Pool.Push(Marker);
        }
        ActiveMarkers.Clear();
    }
    /// <summary>
    /// Destroys both active and pooled markers to fully reset the pools
    /// </summary>
    private static void DestroyMarkerCollection(List<GameObject> ActiveMarkers, Stack<GameObject> Pool)
    {
        foreach (GameObject Marker in ActiveMarkers)
        {
            if (Marker == null)
                continue;
            Destroy(Marker);
        }
        ActiveMarkers.Clear();
        while (Pool.Count > 0)
        {
            GameObject PooledMarker = Pool.Pop();
            if (PooledMarker == null)
                continue;
            Destroy(PooledMarker);
        }
    }
}
