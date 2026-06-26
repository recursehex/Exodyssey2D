using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapGenerator
{
    // 0 means no wall, 1 means place wall
    private static readonly int[,] template1 = new int[3, 3]
    {
        {1, 1, 0},
        {1, 0, 0},
        {1, 0, 0}
    };
    private static readonly int[,] template2 = new int[3, 3]
    {
        {1, 1, 0},
        {1, 0, 1},
        {0, 0, 0}
    };
    private static readonly int[,] template3 = new int[3, 3]
    {
        {1, 1, 0},
        {1, 0, 0},
        {0, 0, 1}
    };
    private static readonly int[,] template4 = new int[3, 3]
    {
        {1, 0, 0},
        {1, 0, 1},
        {0, 0, 1}
    };
    private static readonly List<int[,]> Templates = new()
    {
        template1,
        template2,
        template3,
        template4
    };
    private static int[,] Rotate3by3(int[,] t)
    {
        int[,] res = new int[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                res[col, 2 - row] = t[row, col];
            }
        }
        return res;
    }
    private const float minimumChance = 0.15f;
    // Weight used for wall tiles with no WallInfo definition
    private const int defaultWallWeight = 1;
    /// <summary>
    /// Picks a wall tile weighted by its spawn weight so some tiles appear more often.
    /// Region weights override the global WallInfo weight. Falls back to a uniform pick
    /// if no tile carries a positive weight.
    /// </summary>
    private static Tile PickWeightedWall(Tile[] WallTiles, IReadOnlyDictionary<string, int> WallWeights)
    {
        int totalWeight = 0;
        for (int i = 0; i < WallTiles.Length; i++)
            totalWeight += GetWallWeight(WallTiles[i], WallWeights);
        if (totalWeight <= 0)
            return WallTiles[Random.Range(0, WallTiles.Length)];
        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < WallTiles.Length; i++)
        {
            roll -= GetWallWeight(WallTiles[i], WallWeights);
            if (roll < 0)
                return WallTiles[i];
        }
        return WallTiles[WallTiles.Length - 1];
    }
    /// <summary>
    /// Returns the spawn weight for a wall tile by sprite name: the region override if present,
    /// otherwise the global WallInfo weight, otherwise the default
    /// </summary>
    private static int GetWallWeight(Tile WallTile, IReadOnlyDictionary<string, int> WallWeights)
    {
        if (WallTile == null || WallTile.sprite == null)
            return 0;
        string SpriteName = WallTile.sprite.name;
        if (WallWeights != null && WallWeights.TryGetValue(SpriteName, out int RegionWeight))
            return RegionWeight;
        return WallInfo.Get(SpriteName)?.SpawnWeight ?? defaultWallWeight;
    }
    public static void GenerateMap(Tilemap TilemapWalls, Tile[] WallTiles, IReadOnlyDictionary<string, int> WallWeights = null, ICollection<Vector3Int> PlacedPositions = null)
    {
        int totalTemplates = Templates.Count;
        int numberGenerated = 0;
        int numberOfQuadrants = GameConfig.Grid.WallQuadrantAnchors.Length;
        for (int i = 0; i < numberOfQuadrants; i++)
        {
            if (Random.value > minimumChance)
            {
                numberGenerated++;
                int templateIndex = Random.Range(0, totalTemplates);
                int baseX = GameConfig.Grid.WallQuadrantAnchors[i].x;
                int baseY = GameConfig.Grid.WallQuadrantAnchors[i].y;
                int[,] template = Templates[templateIndex];
                int nRotations = Random.Range(0, 4);
                for (int r = 0; r < nRotations; r++)
                {
                    template = Rotate3by3(template);
                }
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (template[y, x] > 0)
                        {
                            Vector3Int Position = new(baseX + x, baseY - y);
                            TilemapWalls.SetTile(Position, PickWeightedWall(WallTiles, WallWeights));
                            PlacedPositions?.Add(Position);
                        }
                    }
                }
            }
        }
    }
}
