using System;
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
    private Dictionary<Vector3Int, Node> TileAreasToDraw = null;
    public void Initialize(GameObject TileDot, GameObject TileArea, GameObject TargetTemplate)
    {
        this.TileDot = TileDot;
        TileAreaTemplate = TileArea;
        this.TargetTemplate = TargetTemplate;
    }
    public void ClearTileAreas()
    {
        if (TileAreas.Count == 0)
        {
            return;
        }
        TileAreas.ForEach(Area => Destroy(Area));
        TileAreas.Clear();
        TileAreasToDraw = null;
    }
    /// <summary>
    /// Draws tile areas based on areas to draw
    /// </summary>
    /// <param name="AreasToDraw"></param>
    public void DrawTileAreas(Dictionary<Vector3Int, Node> AreasToDraw)
    {
        // Need to clear previous areas
        ClearTileAreas();
        TileAreasToDraw = AreasToDraw;
        // Return if no areas to draw
        if (TileAreasToDraw == null || TileAreasToDraw.Count <= 0)
        {
            return;
        }
        // Draw new areas
        foreach (KeyValuePair<Vector3Int, Node> TileAreaPosition in TileAreasToDraw)
        {
            Vector3 ShiftedDistance = TileAreaPosition.Value.Position + new Vector3(0.5f, 0.5f);
            GameObject TileArea = Instantiate(TileAreaTemplate, ShiftedDistance, Quaternion.identity);
            TileAreas.Add(TileArea);
        }
    }
    public void ClearTargets()
    {
        Targets.ForEach(Target => Destroy(Target));
        Targets.Clear();
    }
    /// <summary>
    /// Draws targets for enemies in range and line of sight
    /// </summary>
    /// <param name="Enemies"></param>
    /// <param name="PlayerPosition"></param>
    /// <param name="weaponRange"></param>
    /// <param name="isStunning"></param>
    /// <param name="Walls"></param>
    public void DrawTargets(List<Enemy> Enemies, Vector3 PlayerPosition, int weaponRange, bool isStunning, Tilemap Walls)
    {
        // Need to clear previous targets
        ClearTargets();
        // Return if weapon has no range
        if (weaponRange <= 0)
        {
            return;
        }
        // Draw new targets
        foreach (Enemy Enemy in Enemies)
        {
            // Skip enemies that are stunned and if weapon is stunning
            if (Enemy.StunIcon.activeSelf && isStunning)
            {
                continue;
            }
            Vector3 EnemyPosition = Enemy.transform.position;
            // Draw targets if enemy is in line of sight and range
            if (IsInLineOfSight(PlayerPosition, EnemyPosition, weaponRange, Walls))
            {
                GameObject Target = Instantiate(TargetTemplate, EnemyPosition, Quaternion.identity);
                Targets.Add(Target);
            }
        }
    }
    /// <summary>
    /// Checks if enemy is in line of sight and range
    /// </summary>
    /// <param name="PlayerPosition"></param>
    /// <param name="EnemyPosition"></param>
    /// <param name="weaponRange"></param>
    /// <param name="Walls"></param>
    /// <returns></returns>
    private bool IsInLineOfSight(Vector3 PlayerPosition, Vector3 EnemyPosition, int weaponRange, Tilemap Walls)
    {
        float distance = Vector3.Distance(PlayerPosition, EnemyPosition);
        // Return if enemy is out of range
        if (distance > weaponRange)
        {
            return false;
        }
        // Check if there are walls in the line of sight
        List<Vector3> Path = BresenhamsAlgorithm(PlayerPosition, EnemyPosition);
        foreach (Vector3 Position in Path)
        {
            Vector3Int PositionInt = Vector3Int.FloorToInt(Position);
            if (Walls.HasTile(PositionInt))
            {
                return false;
            }
        }
        return true;
    }    
    /// <summary>
    /// Bresenham's Line Algorithm to get points between two positions
    /// </summary>
    /// <param name="Start"></param>
    /// <param name="End"></param>
    /// <returns></returns>
    private static List<Vector3> BresenhamsAlgorithm(Vector3 Start, Vector3 End)
    {
        // Ensure Start and End are Vector3Int
        Vector3Int StartInt = Vector3Int.FloorToInt(Start);
        Vector3Int EndInt   = Vector3Int.FloorToInt(End);
        int x0 = StartInt.x;
        int y0 = StartInt.y;
        int x1 = EndInt.x;
        int y1 = EndInt.y;
        // Initialize list to hold points on the line
        List<Vector3> PointsOnLine = new();
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
            {
                break;
            }
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
    public bool IsInMovementRange(Vector3Int Position)
    {
        return TileAreasToDraw?.ContainsKey(Position) == true;
    }
    public bool IsInRangedWeaponRange(Vector3 Position)
    {
        return Targets.Exists(Target => Target.transform.position == Position);
    }
}