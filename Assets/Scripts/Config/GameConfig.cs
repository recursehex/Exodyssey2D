using UnityEngine;

/// <summary>
/// Central location for cross-system gameplay constants.
/// </summary>
public static class GameConfig
{
	public static class Grid
	{
		public const int MinX = -4;
		public const int MaxX = 4;
		public const int MinY = -4;
		public const int MaxY = 4;
		public const int QuadrantSize = 3;
		public const int SecondRowYOffset = 2;
		public const int SafeZoneMaxX = -2;
		public const int SafeZoneMinY = -1;
		public const int SafeZoneMaxY = 1;
		public static readonly Vector3Int[] WallQuadrantAnchors = new Vector3Int[]
		{
            new(MinX, MaxY, 0),
			new(MinX + QuadrantSize, MaxY, 0),
			new(MinX + QuadrantSize * 2, MaxY, 0),
			new(MinX, MinY + SecondRowYOffset, 0),
			new(MinX + QuadrantSize, MinY + SecondRowYOffset, 0),
			new(MinX + QuadrantSize * 2, MinY + SecondRowYOffset, 0)
		};
	}
}
