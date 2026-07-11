using UnityEngine;

public static class GridCoordinates
{
	private static readonly Vector3 CellCenterOffset = new(0.5f, 0.5f, 0f);

	public static Vector3 GetCellCenter(Vector3Int Cell) => Cell + CellCenterOffset;
	public static Vector3Int GetCell(Vector3 WorldPosition) => Vector3Int.FloorToInt(WorldPosition);
	public static bool IsInsideGrid(Vector3Int Cell) =>
		Cell.x >= GameConfig.Grid.MinX
		&& Cell.x <= GameConfig.Grid.MaxX
		&& Cell.y >= GameConfig.Grid.MinY
		&& Cell.y <= GameConfig.Grid.MaxY;
	public static bool IsInSafeZone(Vector3Int Cell) =>
		Cell.x <= GameConfig.Grid.SafeZoneMaxX
		&& Cell.y >= GameConfig.Grid.SafeZoneMinY
		&& Cell.y <= GameConfig.Grid.SafeZoneMaxY;
	public static bool AreOrthogonallyAdjacent(Vector3Int First, Vector3Int Second)
	{
		int horizontalDistance = Mathf.Abs(First.x - Second.x);
		int verticalDistance = Mathf.Abs(First.y - Second.y);
		return horizontalDistance + verticalDistance == 1;
	}
}
