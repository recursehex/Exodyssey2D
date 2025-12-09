using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapRevealAnimator : MonoBehaviour
{
    [SerializeField] private Tilemap TilemapGround;
    [SerializeField] private Tilemap TilemapWalls;
    [SerializeField] private float RingDelay = 0.05f;
    [SerializeField] private float TilePopDuration = 0.20f;
    [SerializeField] private float InitialScale = 0.01f;
    [SerializeField] private float IntraRingStagger = 0.025f;
    private readonly List<TileTarget> Targets = new();
    private readonly List<Coroutine> ActiveAnimations = new();
    private readonly Dictionary<Vector3Int, List<RevealObject>> RevealObjects = new();
    private bool hasPreparedTiles;
    private bool isRevealing;
    private readonly struct TileTarget
    {
        public readonly Tilemap Tilemap;
        public readonly Vector3Int Position;
        public readonly int Radius;
        public TileTarget(Tilemap Tilemap, Vector3Int Position, Vector3Int SpawnTile)
        {
            this.Tilemap = Tilemap;
            this.Position = Position;
            float distance = Vector3.Distance(SpawnTile, Position);
            Radius = Mathf.RoundToInt(distance);
        }
    }
    private struct RevealObject
    {
        public Transform Transform;
        public Vector3 OriginalScale;
    }
    public bool HasPreparedTiles => hasPreparedTiles;
    public bool IsRevealing => isRevealing;
    public void Initialize(Tilemap Ground, Tilemap Walls)
    {
        TilemapGround = Ground;
        TilemapWalls = Walls;
    }
    /// <summary>
    /// Registers a world-space object to pop in when its cell is revealed.
    /// </summary>
    public void RegisterObjectAtCell(Vector3Int Cell, Transform ObjectTransform)
    {
        if (ObjectTransform == null || !hasPreparedTiles)
            return;
        if (!RevealObjects.TryGetValue(Cell, out List<RevealObject> Objects))
        {
            Objects = new List<RevealObject>();
            RevealObjects[Cell] = Objects;
        }
        if (Objects.Exists(Object => Object.Transform == ObjectTransform))
            return;
        RevealObject Entry = new()
        {
            Transform = ObjectTransform,
            OriginalScale = ObjectTransform.localScale
        };
        ObjectTransform.localScale = Entry.OriginalScale * InitialScale;
        Objects.Add(Entry);
    }
    /// <summary>
    /// Clears any ongoing animation and restores transforms to identity.
    /// </summary>
    public void ResetTileTransforms()
    {
        StopAllCoroutines();
        foreach (TileTarget Target in Targets)
            Target.Tilemap.SetTransformMatrix(Target.Position, Matrix4x4.identity);
        foreach (var Pair in RevealObjects)
        {
            foreach (RevealObject Object in Pair.Value)
            {
                if (Object.Transform != null)
                    Object.Transform.localScale = Object.OriginalScale;
            }
        }
        ActiveAnimations.Clear();
        Targets.Clear();
        RevealObjects.Clear();
        hasPreparedTiles = false;
        isRevealing = false;
    }
    /// <summary>
    /// Prepares tile data and hides tiles by scaling them down.
    /// </summary>
    public void PrepareTilesForReveal(Vector3Int SpawnTile, IReadOnlyCollection<Vector3Int> GroundTiles, IReadOnlyCollection<Vector3Int> WallTiles)
    {
        ResetTileTransforms();
        if (TilemapGround == null || TilemapWalls == null)
            return;
        AddTargets(GroundTiles, TilemapGround, SpawnTile);
        AddTargets(WallTiles, TilemapWalls, SpawnTile);
        foreach (TileTarget Target in Targets)
            Target.Tilemap.SetTransformMatrix(Target.Position, GetScaleMatrix(InitialScale));
        hasPreparedTiles = Targets.Count > 0;
    }
    public IEnumerator PlayReveal()
    {
        if (!hasPreparedTiles || Targets.Count == 0)
            yield break;
        isRevealing = true;
        ActiveAnimations.Clear();
        SortedDictionary<int, List<TileTarget>> Buckets = BucketTargetsByRadius();
        float maxTileDelay = 0f;
        foreach (var Bucket in Buckets)
        {
            foreach (var Target in Bucket.Value)
            {
                float tileDelay = IntraRingStagger > 0f ? Random.Range(0f, IntraRingStagger) : 0f;
                maxTileDelay = Mathf.Max(maxTileDelay, tileDelay);
                Coroutine Animation = StartCoroutine(AnimateTile(Target, tileDelay));
                ActiveAnimations.Add(Animation);
            }
            yield return new WaitForSeconds(RingDelay);
        }
        float finalWait = TilePopDuration + maxTileDelay;
        if (finalWait > 0f)
            yield return new WaitForSeconds(finalWait);
        ActiveAnimations.Clear();
        RevealObjects.Clear();
        hasPreparedTiles = false;
        isRevealing = false;
    }
    private IEnumerator AnimateTile(TileTarget Target, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        RevealObjects.TryGetValue(Target.Position, out List<RevealObject> Objects);
        float elapsed = 0f;
        while (elapsed < TilePopDuration)
        {
            float t = elapsed / TilePopDuration;
            float scale = Mathf.Lerp(InitialScale, 1f, t);
            Target.Tilemap.SetTransformMatrix(Target.Position, GetScaleMatrix(scale));
            if (Objects != null)
            {
                foreach (RevealObject Object in Objects)
                {
                    if (Object.Transform != null)
                        Object.Transform.localScale = Object.OriginalScale * scale;
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Target.Tilemap.SetTransformMatrix(Target.Position, Matrix4x4.identity);
        if (Objects != null)
        {
            foreach (var Object in Objects)
            {
                if (Object.Transform != null)
                    Object.Transform.localScale = Object.OriginalScale;
            }
        }
    }
    private void AddTargets(IReadOnlyCollection<Vector3Int> Tiles, Tilemap Tilemap, Vector3Int SpawnTile)
    {
        foreach (var Tile in Tiles)
            Targets.Add(new(Tilemap, Tile, SpawnTile));
    }
    private SortedDictionary<int, List<TileTarget>> BucketTargetsByRadius()
    {
        SortedDictionary<int, List<TileTarget>> Buckets = new();
        foreach (TileTarget Target in Targets)
        {
            if (!Buckets.TryGetValue(Target.Radius, out var Bucket))
            {
                Bucket = new List<TileTarget>();
                Buckets[Target.Radius] = Bucket;
            }
            Bucket.Add(Target);
        }
        return Buckets;
    }
    private static Matrix4x4 GetScaleMatrix(float scale)
    {
        Vector3 ScaleVector = new(scale, scale, 1f);
        return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, ScaleVector);
    }
}
