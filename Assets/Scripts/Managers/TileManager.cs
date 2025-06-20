using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public GameObject TileDot;
    [SerializeField] private GameObject TileAreaTemplate;
    [SerializeField] private GameObject TargetTemplate;
    [SerializeField] private GameObject TracerTemplate;
    private readonly List<GameObject> TileAreas = new();
    private readonly List<GameObject> Targets = new();
    private readonly List<GameObject> Tracers = new();
    private List<Vector3> TracerPath = new();
    private Dictionary<Vector3Int, Node> TileAreasToDraw = null;
    public void Initialize(GameObject TileDot, GameObject TileArea, GameObject TargetTemplate, GameObject TracerTemplate)
    {
        this.TileDot = TileDot;
        TileAreaTemplate = TileArea;
        this.TargetTemplate = TargetTemplate;
        this.TracerTemplate = TracerTemplate;
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
    
    public void DrawTileAreas(Dictionary<Vector3Int, Node> AreasToDraw)
    {
        ClearTileAreas();
        TileAreasToDraw = AreasToDraw;
        if (TileAreasToDraw != null && TileAreasToDraw.Count > 0)
        {
            foreach (KeyValuePair<Vector3Int, Node> TileAreaPosition in TileAreasToDraw)
            {
                Vector3 ShiftedDistance = TileAreaPosition.Value.Position + new Vector3(0.5f, 0.5f, 0);
                GameObject TileArea = Instantiate(TileAreaTemplate, ShiftedDistance, Quaternion.identity);
                TileAreas.Add(TileArea);
            }
        }
    }
    public void ClearTargetsAndTracers()
    {
        ClearTargets();
        ClearTracers();
    }
    private void ClearTargets()
    {
        Targets.ForEach(Target => Destroy(Target));
        Targets.Clear();
    }
    private void ClearTracers()
    {
        Tracers.ForEach(Tracer => Destroy(Tracer));
        Tracers.Clear();
    }    
    public void DrawTargetsAndTracers(List<Enemy> Enemies, Vector3 PlayerPosition, int weaponRange, bool isStunning, Tilemap Walls)
    {
        ClearTargetsAndTracers();
        if (weaponRange <= 0)
        {
            return;
        }
        foreach (Enemy Enemy in Enemies)
        {
            if (Enemy.StunIcon.activeSelf && isStunning)
            {
                continue;
            }
            Vector3 EnemyPosition = Enemy.transform.position;
            if (TracerPath.Count > 0)
            {
                foreach (Vector3 tracerPosition in TracerPath)
                {
                    GameObject Tracer = Instantiate(TracerTemplate, tracerPosition, Quaternion.identity);
                    Tracers.Add(Tracer);
                }
            }
            if (IsInLineOfSight(PlayerPosition, EnemyPosition, weaponRange, Walls))
            {
                GameObject Target = Instantiate(TargetTemplate, EnemyPosition, Quaternion.identity);
                Targets.Add(Target);
            }
        }
        TracerPath.Clear();
    }
    
    private bool IsInLineOfSight(Vector3 PlayerPosition, Vector3 EnemyPosition, int weaponRange, Tilemap Walls)
    {
        float distance = Mathf.Sqrt(Mathf.Pow(EnemyPosition.x - PlayerPosition.x, 2) + Mathf.Pow(EnemyPosition.y - PlayerPosition.y, 2));
        if (distance > weaponRange)
        {
            return false;
        }
        Vector3Int PlayerPositionInt = new((int)(PlayerPosition.x - 0.5f), (int)(PlayerPosition.y - 0.5f), 0);
        Vector3Int EnemyPositionInt = new((int)(EnemyPosition.x - 0.5f), (int)(EnemyPosition.y - 0.5f), 0);
        TracerPath = BresenhamsAlgorithm(PlayerPositionInt.x, PlayerPositionInt.y, EnemyPositionInt.x, EnemyPositionInt.y);
        foreach (Vector3 tracerPosition in TracerPath)
        {
            Vector3Int tracerPositionInt = new((int)tracerPosition.x, (int)tracerPosition.y, 0);
            if (Walls.HasTile(tracerPositionInt))
            {
                return false;
            }
        }
        return true;
    }    
    private static List<Vector3> BresenhamsAlgorithm(int x0, int y0, int x1, int y1)
    {
        List<Vector3> PointsOnLine = new();
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;
        
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
        return Targets.Find(Target => Target.transform.position == Position);
    }
}