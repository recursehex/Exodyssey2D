using System;
using System.Collections.Generic;
using UnityEngine;

public class Structure : MonoBehaviour
{
	[NonSerialized] public StructureInfo Info;
	private static readonly Dictionary<string, Sprite> SpriteCache = new();
#if UNITY_EDITOR
	[Header("Debug")]
	[SerializeField] private StructureInfo.Tags StructureTag = StructureInfo.Tags.Unknown;
	[SerializeField] private string StructureName = string.Empty;
	[SerializeField] private bool isLooted = false;
	private void LateUpdate()
	{
		if (Info == null)
		{
			StructureTag = StructureInfo.Tags.Unknown;
			StructureName = string.Empty;
			isLooted = false;
			return;
		}
		StructureTag = Info.Tag;
		StructureName = Info.Name;
		isLooted = Info.IsLooted;
	}
#endif
	public Sprite GetSprite()
	{
		if (Info == null)
			return null;
		string key = Info.IsLooted
			? $"{Info.Tag.ToString().ToLowerInvariant()}_open"
			: Info.Tag.ToString().ToLowerInvariant();
		if (SpriteCache.TryGetValue(key, out Sprite CachedSprite))
			return CachedSprite;
		string ResourcePath = $"Sprites/{key}";
		Sprite Sprite = Resources.Load<Sprite>(ResourcePath);
		if (Sprite != null)
			SpriteCache[key] = Sprite;
		return Sprite;
	}
	public void UpdateSprite()
	{
		Sprite NewSprite = GetSprite();
		if (NewSprite != null && TryGetComponent(out SpriteRenderer Renderer))
			Renderer.sprite = NewSprite;
	}
	public bool OccupiesCell(Vector3Int Cell)
	{
		Vector3Int BaseCell = new(
			Mathf.FloorToInt(transform.position.x),
			Mathf.FloorToInt(transform.position.y),
			0);
		return Cell.x >= BaseCell.x && Cell.x < BaseCell.x + Info.Width
			&& Cell.y >= BaseCell.y && Cell.y < BaseCell.y + Info.Height;
	}
	public bool IsAdjacentTo(Vector3 WorldPosition)
	{
		Vector3Int PlayerCell = new(
			Mathf.FloorToInt(WorldPosition.x),
			Mathf.FloorToInt(WorldPosition.y),
			0);
		Vector3Int BaseCell = new(
			Mathf.FloorToInt(transform.position.x),
			Mathf.FloorToInt(transform.position.y),
			0);
		for (int ox = 0; ox < Info.Width; ox++)
		{
			for (int oy = 0; oy < Info.Height; oy++)
			{
				Vector3Int OccupiedCell = new(BaseCell.x + ox, BaseCell.y + oy, 0);
				int dx = Mathf.Abs(PlayerCell.x - OccupiedCell.x);
				int dy = Mathf.Abs(PlayerCell.y - OccupiedCell.y);
				if ((dx == 1 && dy == 0) || (dx == 0 && dy == 1))
					return true;
			}
		}
		return false;
	}
}
