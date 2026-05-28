using System.Collections.Generic;
using UnityEngine;

public class StructureManager : MonoBehaviour
{
	public List<Structure> Structures { get; private set; } = new();
	[Header("Spawning")]
	[SerializeField] private int minSpawnCount = 0;
	[SerializeField] private int maxSpawnCountExclusive = 3;
	private static readonly int lastStructureIndex = (int)StructureInfo.Tags.Unknown;
	public Structure SpawnStructure(int index, Vector3 Position)
	{
		StructureInfo Info = new(index);
		GameObject GO = new(Info.Tag.ToString());
		GO.transform.position = Position;
		SpriteRenderer Renderer = GO.AddComponent<SpriteRenderer>();
		Renderer.sortingLayerName = "Structures";
		Structure Structure = GO.AddComponent<Structure>();
		Structure.Info = Info;
		Sprite Sprite = Structure.GetSprite();
		if (Sprite != null)
			Renderer.sprite = Sprite;
		Structures.Add(Structure);
		return Structure;
	}
	public void GenerateStructures()
	{
		int count = Random.Range(minSpawnCount, maxSpawnCountExclusive);
		int cap = count * 3;
		while (cap > 0 && count > 0)
		{
			if (TrySpawnRandomStructure())
				count--;
			cap--;
		}
	}
	private bool TrySpawnRandomStructure()
	{
		int x = Random.Range(GameConfig.Grid.MinX, GameConfig.Grid.MaxX + 1);
		int y = Random.Range(GameConfig.Grid.MinY, GameConfig.Grid.MaxY + 1);
		Vector3Int Cell = new(x, y);
		if (GameManager.Instance.HasWallAtPosition(Cell)
			|| GameManager.Instance.HasFireAtPosition(Cell)
			|| GameManager.Instance.HasExitTileAtPosition(Cell)
			|| HasStructureAtCell(Cell)
			|| (x <= GameConfig.Grid.SafeZoneMaxX
				&& y <= GameConfig.Grid.SafeZoneMaxY
				&& y >= GameConfig.Grid.SafeZoneMinY))
			return false;
		Vector3 ShiftedPosition = Cell + new Vector3(0.5f, 0.5f);
		if (GameManager.Instance.HasItemAtPosition(ShiftedPosition)
			|| GameManager.Instance.HasEnemyAtPosition(ShiftedPosition)
			|| GameManager.Instance.HasVehicleAtPosition(ShiftedPosition))
			return false;
		int index = Random.Range(0, lastStructureIndex);
		GameManager.Instance.SpawnStructure(index, ShiftedPosition);
		return true;
	}
	public bool HasStructureAtCell(Vector3Int Cell) => GetStructureAtCell(Cell) != null;
	public Structure GetStructureAtCell(Vector3Int Cell) =>
		Structures.Find(s => s != null && s.OccupiesCell(Cell));
	public bool HasStructureAtPosition(Vector3 Position)
	{
		Vector3Int Cell = new(
			Mathf.FloorToInt(Position.x),
			Mathf.FloorToInt(Position.y),
			0);
		return HasStructureAtCell(Cell);
	}
	public void DestroyAllStructures()
	{
		Structures.ForEach(s => { if (s != null) Destroy(s.gameObject); });
		Structures.Clear();
	}
}
