using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGen
{
    // 0 means no wall, 1 means place wall
    readonly int[,] template1 = new int[3, 3] {
   {1, 1, 0} ,
   {1, 0, 0} ,
   {1, 0, 0}
};
    readonly int[,] template2 = new int[3, 3] {
   {1, 1, 0} ,
   {1, 1, 0} ,
   {0, 0, 0}
};
    readonly int[,] template3 = new int[3, 3] {
   {1, 1, 0} ,
   {1, 0, 0} ,
   {0, 0, 1}
};
    readonly int[,] template4 = new int[3, 3] {
   {1, 0, 0} ,
   {1, 0, 1} ,
   {0, 0, 1}
};
    readonly List<int[,]> allTemplates;
    readonly int[,] quadrants = new int[6, 2]
    {
        {-4, 4} ,
        {-1, 4} ,
        { 2, 4} ,
        {-4, -2} ,
        {-1, -2} ,
        { 2, -2}
    };

    public MapGen()
    {
        allTemplates = new List<int[,]>
        {
            template1,
            template2,
            template3,
            template4
        };
    }

    int[,] Rotate3by3(int[,] t)
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

    readonly float minimumChance = 0.15f;

    public void GenerateMap(Tilemap tilemapWalls, Tile[] wallTiles)
    {
        int totalTemplates = allTemplates.Count;
        int nGenerated = 0;
        int numberOfQuadrants = 6;
        for (int i = 0; i < numberOfQuadrants; i++)
        {
            if (Random.value > minimumChance)
            {
                nGenerated++;
                int templateIdx = Random.Range(0, totalTemplates);
                int baseX = quadrants[i, 0];
                int baseY = quadrants[i, 1];
                int[,] t = allTemplates[templateIdx];
                int nRotations = Random.Range(0, 4);
                for (int r = 0; r < nRotations; r++)
                {
                    t = Rotate3by3(t);
                }
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (t[y, x] > 0)
                        {
                            Vector3Int p = new(baseX + x, baseY - y, 0);
                            tilemapWalls.SetTile(p, wallTiles[Random.Range(0, wallTiles.Length)]);
                        }
                    }
                }
            }
        }
    }
}
