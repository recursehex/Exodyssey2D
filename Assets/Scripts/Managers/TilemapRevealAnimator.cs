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
    private readonly List<TileAnimation> AnimationScratch = new();
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
    private struct TileAnimation
    {
        public TileTarget Target;
        public float StartTime;
        public bool Completed;
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
        AnimationScratch.Clear();
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
        yield return StartCoroutine(AnimatePreparedTiles(true));
        RevealObjects.Clear();
        hasPreparedTiles = false;
        isRevealing = false;
    }
    public IEnumerator PlayCollapse()
    {
        if (!hasPreparedTiles || Targets.Count == 0)
            yield break;
        isRevealing = true;
        yield return StartCoroutine(AnimatePreparedTiles(false));
        hasPreparedTiles = false;
        isRevealing = false;
    }
    private IEnumerator AnimatePreparedTiles(bool isReveal)
    {
        AnimationScratch.Clear();
        SortedDictionary<int, List<TileTarget>> Buckets = BucketTargetsByRadius();
        List<int> BucketKeys = new(Buckets.Keys);
        float ringStart = 0f;
        for (int keyIndex = 0; keyIndex < BucketKeys.Count; keyIndex++)
        {
            int bucketIndex = isReveal ? keyIndex : BucketKeys.Count - 1 - keyIndex;
            List<TileTarget> Bucket = Buckets[BucketKeys[bucketIndex]];
            foreach (TileTarget Target in Bucket)
            {
                float stagger = IntraRingStagger > 0f ? Random.Range(0f, IntraRingStagger) : 0f;
                AnimationScratch.Add(new TileAnimation
                {
                    Target = Target,
                    StartTime = ringStart + stagger,
                    Completed = false,
                });
            }
            ringStart += RingDelay;
        }
        float elapsed = 0f;
        int remaining = AnimationScratch.Count;
        float startScale = isReveal ? HiddenScale : FullScale;
        float endScale = isReveal ? FullScale : HiddenScale;
        while (remaining > 0)
        {
            for (int i = 0; i < AnimationScratch.Count; i++)
            {
                TileAnimation Animation = AnimationScratch[i];
                if (Animation.Completed || elapsed < Animation.StartTime)
                    continue;
                float animationElapsed = elapsed - Animation.StartTime;
                if (TilePopDuration <= 0f || animationElapsed >= TilePopDuration)
                {
                    ApplyTargetScale(Animation.Target, endScale, isReveal);
                    Animation.Completed = true;
                    AnimationScratch[i] = Animation;
                    remaining--;
                    continue;
                }
                float scale = Mathf.Lerp(startScale, endScale, animationElapsed / TilePopDuration);
                ApplyTargetScale(Animation.Target, scale, false);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        AnimationScratch.Clear();
    }
    private void ApplyTargetScale(TileTarget Target, float scale, bool setIdentity)
    {
        Target.Tilemap.SetTransformMatrix(Target.Position,
            setIdentity ? Matrix4x4.identity : GetScaleMatrix(scale));
        if (Target.Tilemap != TilemapGround)
            return;
        RevealObjects.TryGetValue(Target.Position, out List<RevealObject> Objects);
        if (Objects != null)
        {
            foreach (RevealObject Object in Objects)
            {
                if (Object.Transform != null)
                    Object.Transform.localScale = Object.OriginalScale * (setIdentity ? FullScale : scale);
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
