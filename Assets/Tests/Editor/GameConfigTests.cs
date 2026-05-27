using NUnit.Framework;
using UnityEngine;

public class GameConfigTests
{
    [Test]
    public void Grid_IsNineByNine()
    {
        int width = GameConfig.Grid.MaxX - GameConfig.Grid.MinX + 1;
        int height = GameConfig.Grid.MaxY - GameConfig.Grid.MinY + 1;
        Assert.AreEqual(9, width);
        Assert.AreEqual(9, height);
    }

    [Test]
    public void Grid_IsSymmetric()
    {
        Assert.AreEqual(-GameConfig.Grid.MinX, GameConfig.Grid.MaxX);
        Assert.AreEqual(-GameConfig.Grid.MinY, GameConfig.Grid.MaxY);
    }

    [Test]
    public void WallQuadrantAnchors_HasSixEntries()
    {
        Assert.AreEqual(6, GameConfig.Grid.WallQuadrantAnchors.Length);
    }

    [Test]
    public void QuadrantSize_IsThree()
    {
        Assert.AreEqual(3, GameConfig.Grid.QuadrantSize);
    }

    [Test]
    public void WallQuadrantAnchors_AreWithinGridBounds()
    {
        foreach (Vector3Int anchor in GameConfig.Grid.WallQuadrantAnchors)
        {
            Assert.GreaterOrEqual(anchor.x, GameConfig.Grid.MinX,
                $"Anchor ({anchor.x},{anchor.y}) x is below grid min");
            Assert.LessOrEqual(anchor.x, GameConfig.Grid.MaxX,
                $"Anchor ({anchor.x},{anchor.y}) x exceeds grid max");
            Assert.GreaterOrEqual(anchor.y, GameConfig.Grid.MinY,
                $"Anchor ({anchor.x},{anchor.y}) y is below grid min");
            Assert.LessOrEqual(anchor.y, GameConfig.Grid.MaxY,
                $"Anchor ({anchor.x},{anchor.y}) y exceeds grid max");
        }
    }

    [Test]
    public void SafeZone_IsOnLeftSide()
    {
        Assert.Less(GameConfig.Grid.SafeZoneMaxX, 0, "Safe zone should be on the left side of the grid");
    }

    [Test]
    public void SafeZone_IsVerticallySymmetric()
    {
        Assert.AreEqual(-GameConfig.Grid.SafeZoneMinY, GameConfig.Grid.SafeZoneMaxY);
    }

    [Test]
    public void SafeZone_IsWithinGridBounds()
    {
        Assert.GreaterOrEqual(GameConfig.Grid.SafeZoneMaxX, GameConfig.Grid.MinX);
        Assert.LessOrEqual(GameConfig.Grid.SafeZoneMaxX, GameConfig.Grid.MaxX);
        Assert.GreaterOrEqual(GameConfig.Grid.SafeZoneMinY, GameConfig.Grid.MinY);
        Assert.LessOrEqual(GameConfig.Grid.SafeZoneMaxY, GameConfig.Grid.MaxY);
    }

    [Test]
    public void WallQuadrants_CoverGrid_InTwoRows()
    {
        // First 3 anchors should be top row, next 3 should be bottom row
        Vector3Int[] anchors = GameConfig.Grid.WallQuadrantAnchors;
        // Top row all share the same y
        Assert.AreEqual(anchors[0].y, anchors[1].y);
        Assert.AreEqual(anchors[1].y, anchors[2].y);
        // Bottom row all share the same y
        Assert.AreEqual(anchors[3].y, anchors[4].y);
        Assert.AreEqual(anchors[4].y, anchors[5].y);
        // Top row y > bottom row y
        Assert.Greater(anchors[0].y, anchors[3].y);
    }

    [Test]
    public void WallQuadrants_AreSpacedByQuadrantSize()
    {
        Vector3Int[] anchors = GameConfig.Grid.WallQuadrantAnchors;
        // Check horizontal spacing in top row
        Assert.AreEqual(GameConfig.Grid.QuadrantSize, anchors[1].x - anchors[0].x);
        Assert.AreEqual(GameConfig.Grid.QuadrantSize, anchors[2].x - anchors[1].x);
    }
}
