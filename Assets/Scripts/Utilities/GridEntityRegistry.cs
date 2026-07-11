using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GridEntityRegistry<T> where T : Component
{
	private readonly Func<T, Vector3Int> GetCurrentCell;
	private readonly Dictionary<Vector3Int, List<T>> EntitiesByCell = new();
	private readonly Dictionary<T, Vector3Int> CellByEntity = new();
	public List<T> Entities { get; } = new();

	public GridEntityRegistry(Func<T, Vector3Int> GetCurrentCell)
	{
		this.GetCurrentCell = GetCurrentCell;
	}

	public void Add(T Entity) => Add(Entity, GetCurrentCell(Entity));

	public void Add(T Entity, Vector3Int Cell)
	{
		if (Entity == null || CellByEntity.ContainsKey(Entity))
			return;
		Entities.Add(Entity);
		AddToCell(Entity, Cell);
	}

	public void UpdateCell(T Entity, Vector3Int Cell)
	{
		if (Entity == null)
			return;
		if (!CellByEntity.TryGetValue(Entity, out Vector3Int PreviousCell))
		{
			Add(Entity, Cell);
			return;
		}
		if (PreviousCell == Cell)
			return;
		RemoveFromCell(Entity, PreviousCell);
		AddToCell(Entity, Cell);
	}

	public void UpdateCell(T Entity) => UpdateCell(Entity, GetCurrentCell(Entity));

	public bool Remove(T Entity)
	{
		if (Entity is null || !CellByEntity.TryGetValue(Entity, out Vector3Int Cell))
			return false;
		CellByEntity.Remove(Entity);
		RemoveFromCell(Entity, Cell);
		return Entities.Remove(Entity);
	}

	public T GetFirst(Vector3Int Cell)
	{
		if (!EntitiesByCell.TryGetValue(Cell, out List<T> AtCell))
			return null;
		for (int i = AtCell.Count - 1; i >= 0; i--)
		{
			T Entity = AtCell[i];
			if (Entity == null)
			{
				AtCell.RemoveAt(i);
				CellByEntity.Remove(Entity);
				Entities.Remove(Entity);
				continue;
			}
			Vector3Int CurrentCell = GetCurrentCell(Entity);
			if (CurrentCell == Cell)
				return Entity;
			AtCell.RemoveAt(i);
			AddToCell(Entity, CurrentCell);
		}
		if (AtCell.Count == 0)
			EntitiesByCell.Remove(Cell);
		return null;
	}

	public bool Contains(Vector3Int Cell) => GetFirst(Cell) != null;

	public void Clear()
	{
		Entities.Clear();
		EntitiesByCell.Clear();
		CellByEntity.Clear();
	}

	private void AddToCell(T Entity, Vector3Int Cell)
	{
		if (!EntitiesByCell.TryGetValue(Cell, out List<T> AtCell))
		{
			AtCell = new();
			EntitiesByCell[Cell] = AtCell;
		}
		AtCell.Add(Entity);
		CellByEntity[Entity] = Cell;
	}

	private void RemoveFromCell(T Entity, Vector3Int Cell)
	{
		if (!EntitiesByCell.TryGetValue(Cell, out List<T> AtCell))
			return;
		AtCell.Remove(Entity);
		if (AtCell.Count == 0)
			EntitiesByCell.Remove(Cell);
	}
}
