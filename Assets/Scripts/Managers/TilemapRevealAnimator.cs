using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapRevealAnimator : MonoBehaviour
{
    private const float FullScale = 1f;
    private const float HiddenScale = 0f;
    [SerializeField] private Tilemap TilemapGround;
    [SerializeField] private Tilemap TilemapWalls;
    [SerializeField] private float RingDelay = 0.05f;
    [SerializeField] private float TilePopDuration = 0.20f;
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
        RegisterObjectAtCellInternal(Cell, ObjectTransform, true);
    }
    /// <summary>
    /// Registers a world-space object to shrink when its cell collapses.
    /// </summary>
    public void RegisterObjectAtCellForCollapse(Vector3Int Cell, Transform ObjectTransform)
    {
        RegisterObjectAtCellInternal(Cell, ObjectTransform, false);
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
            Target.Tilemap.SetTransformMatrix(Target.Position, GetScaleMatrix(HiddenScale));
        hasPreparedTiles = Targets.Count > 0;
    }
    /// <summary>
    /// Prepares tile data for a collapse animation without changing tile transforms.
    /// </summary>
    public void PrepareTilesForCollapse(Vector3Int OriginTile, params Tilemap[] ExtraTilemaps)
    {
        ResetTileTransforms();
        if (TilemapGround == null || TilemapWalls == null)
            return;
        AddTargetsFromTilemap(TilemapGround, OriginTile);
        AddTargetsFromTilemap(TilemapWalls, OriginTile);
        if (ExtraTilemaps != null)
        {
            foreach (Tilemap ExtraTilemap in ExtraTilemaps)
                AddTargetsFromTilemap(ExtraTilemap, OriginTile);
        }
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
                Coroutine Animation = StartCoroutine(AnimateTile(Target, tileDelay, HiddenScale, FullScale, true));
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
    public IEnumerator PlayCollapse()
    {
        if (!hasPreparedTiles || Targets.Count == 0)
            yield break;
        isRevealing = true;
        ActiveAnimations.Clear();
        SortedDictionary<int, List<TileTarget>> Buckets = BucketTargetsByRadius();
        float maxTileDelay = 0f;
        List<int> BucketKeys = new(Buckets.Keys);
        for (int i = BucketKeys.Count - 1; i >= 0; i--)
        {
            List<TileTarget> Bucket = Buckets[BucketKeys[i]];
            foreach (var Target in Bucket)
            {
                float tileDelay = IntraRingStagger > 0f ? Random.Range(0f, IntraRingStagger) : 0f;
                maxTileDelay = Mathf.Max(maxTileDelay, tileDelay);
                Coroutine Animation = StartCoroutine(AnimateTile(Target, tileDelay, FullScale, HiddenScale, false));
                ActiveAnimations.Add(Animation);
            }
            yield return new WaitForSeconds(RingDelay);
        }
        float finalWait = TilePopDuration + maxTileDelay;
        if (finalWait > 0f)
            yield return new WaitForSeconds(finalWait);
        ActiveAnimations.Clear();
        hasPreparedTiles = false;
        isRevealing = false;
    }
    private IEnumerator AnimateTile(TileTarget Target, float delay, float startScale, float endScale, bool setIdentityAtEnd)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        RevealObjects.TryGetValue(Target.Position, out List<RevealObject> Objects);
        float elapsed = 0f;
        while (elapsed < TilePopDuration)
        {
            float t = elapsed / TilePopDuration;
            float scale = Mathf.Lerp(startScale, endScale, t);
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
        Target.Tilemap.SetTransformMatrix(Target.Position,
            setIdentityAtEnd ? Matrix4x4.identity : GetScaleMatrix(endScale));
        if (Objects != null)
        {
            foreach (var Object in Objects)
            {
                if (Object.Transform != null)
                    Object.Transform.localScale = Object.OriginalScale * (setIdentityAtEnd ? FullScale : endScale);
            }
        }
    }
    private void AddTargets(IReadOnlyCollection<Vector3Int> Tiles, Tilemap Tilemap, Vector3Int SpawnTile)
    {
        foreach (var Tile in Tiles)
            Targets.Add(new(Tilemap, Tile, SpawnTile));
    }
    private void AddTargetsFromTilemap(Tilemap Tilemap, Vector3Int OriginTile)
    {
        if (Tilemap == null)
            return;
        foreach (Vector3Int Position in Tilemap.cellBounds.allPositionsWithin)
        {
            if (Tilemap.HasTile(Position))
                Targets.Add(new(Tilemap, Position, OriginTile));
        }
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
        Vector3 ScaleVector = new(scale, scale, FullScale);
        return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, ScaleVector);
    }
    private void RegisterObjectAtCellInternal(Vector3Int Cell, Transform ObjectTransform, bool scaleToInitial)
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
        if (scaleToInitial)
            ObjectTransform.localScale = Entry.OriginalScale * HiddenScale;
        Objects.Add(Entry);
    }
}
