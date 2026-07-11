using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class EntityVisibilityController
{
	private const int fireSortingOrderOffset = 1;
	private sealed class RendererSet
	{
		public SpriteRenderer[] SpriteRenderers;
		public MeshRenderer[] MeshRenderers;
	}
	private readonly Dictionary<GameObject, RendererSet> RendererCache = new();
	private Tilemap TilemapGround;
	private Tilemap TilemapWalls;
	private Tilemap TilemapExit;
	private Player Player;
	private EnemyManager EnemyManager;
	private ItemManager ItemManager;
	private VehicleManager VehicleManager;
	private StructureManager StructureManager;
	private FireManager FireManager;
	private int lastSortingSignature = int.MinValue;
	private int CurrentOverlaySortingLayerId;
	private int CurrentFireSortingOrder;
	private bool anyEntityHidden;

	public void Initialize(
		Tilemap TilemapGround,
		Tilemap TilemapWalls,
		Tilemap TilemapExit,
		Player Player,
		EnemyManager EnemyManager,
		ItemManager ItemManager,
		VehicleManager VehicleManager,
		StructureManager StructureManager,
		FireManager FireManager)
	{
		this.TilemapGround = TilemapGround;
		this.TilemapWalls = TilemapWalls;
		this.TilemapExit = TilemapExit;
		this.Player = Player;
		this.EnemyManager = EnemyManager;
		this.ItemManager = ItemManager;
		this.VehicleManager = VehicleManager;
		this.StructureManager = StructureManager;
		this.FireManager = FireManager;
	}

	public void ClearCache()
	{
		RendererCache.Clear();
		anyEntityHidden = false;
	}

	public void InvalidateSorting() => lastSortingSignature = int.MinValue;

	public void ConfigureOverlaySorting(MeshRenderer OverlayRenderer, int baseSortingOrder)
	{
		if (OverlayRenderer == null)
			return;
		int signature = ComputeSortingSignature();
		if (signature == lastSortingSignature)
			return;
		lastSortingSignature = signature;
		int topLayerId = OverlayRenderer.sortingLayerID;
		int topLayerValue = int.MinValue;
		TryConsumeRendererLayer(TilemapGround, ref topLayerId, ref topLayerValue);
		TryConsumeRendererLayer(TilemapWalls, ref topLayerId, ref topLayerValue);
		TryConsumeRendererLayer(TilemapExit, ref topLayerId, ref topLayerValue);
		TryConsumeRendererLayer(Player != null ? Player.GetComponent<SpriteRenderer>() : null, ref topLayerId, ref topLayerValue);
		ConsumeEntityLayers(EnemyManager?.Enemies, ref topLayerId, ref topLayerValue);
		ConsumeEntityLayers(ItemManager?.Items, ref topLayerId, ref topLayerValue);
		ConsumeEntityLayers(VehicleManager?.Vehicles, ref topLayerId, ref topLayerValue);
		ConsumeEntityLayers(StructureManager?.Structures, ref topLayerId, ref topLayerValue);
		if (topLayerValue == int.MinValue
			&& TilemapGround != null
			&& TilemapGround.TryGetComponent(out TilemapRenderer GroundRenderer))
			topLayerId = GroundRenderer.sortingLayerID;
		int maxWorldOrder = int.MinValue;
		TryConsumeRendererOrder(TilemapGround, topLayerId, ref maxWorldOrder);
		TryConsumeRendererOrder(TilemapWalls, topLayerId, ref maxWorldOrder);
		TryConsumeRendererOrder(TilemapExit, topLayerId, ref maxWorldOrder);
		TryConsumeRendererOrder(Player != null ? Player.GetComponent<SpriteRenderer>() : null, topLayerId, ref maxWorldOrder);
		ConsumeEntityOrders(EnemyManager?.Enemies, topLayerId, ref maxWorldOrder);
		ConsumeEntityOrders(ItemManager?.Items, topLayerId, ref maxWorldOrder);
		ConsumeEntityOrders(VehicleManager?.Vehicles, topLayerId, ref maxWorldOrder);
		ConsumeEntityOrders(StructureManager?.Structures, topLayerId, ref maxWorldOrder);
		int desiredOverlayOrder = Mathf.Max(baseSortingOrder, maxWorldOrder + 1);
		int minCanvasOrder = int.MaxValue;
		Canvas[] Canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
		foreach (Canvas Canvas in Canvases)
		{
			if (Canvas == null
				|| !Canvas.isActiveAndEnabled
				|| Canvas.sortingLayerID != topLayerId)
				continue;
			minCanvasOrder = Mathf.Min(minCanvasOrder, Canvas.sortingOrder);
		}
		if (minCanvasOrder != int.MaxValue && desiredOverlayOrder >= minCanvasOrder)
			desiredOverlayOrder = minCanvasOrder - 1;
		if (desiredOverlayOrder <= maxWorldOrder)
			desiredOverlayOrder = maxWorldOrder + 1;
		OverlayRenderer.sortingLayerID = topLayerId;
		OverlayRenderer.sortingOrder = desiredOverlayOrder;
		CurrentOverlaySortingLayerId = topLayerId;
		CurrentFireSortingOrder = desiredOverlayOrder + fireSortingOrderOffset;
	}

	public void ApplyVisibility(bool isRestricted, Func<Vector3Int, bool> IsCellVisible)
	{
		if (EnemyManager == null
			|| ItemManager == null
			|| VehicleManager == null
			|| FireManager == null
			|| TilemapGround == null)
			return;
		if (!isRestricted)
		{
			if (anyEntityHidden)
			{
				SetEntityListVisibility(EnemyManager.Enemies, true);
				foreach (Enemy Enemy in EnemyManager.Enemies)
				{
					if (Enemy != null && Enemy.StunIcon != null)
						SetRenderersVisible(Enemy.StunIcon, true);
				}
				SetEntityListVisibility(ItemManager.Items, true);
				SetEntityListVisibility(VehicleManager.Vehicles, true);
				SetEntityListVisibility(StructureManager?.Structures, true);
				SetEntityListVisibility(FireManager.Fires, true);
				anyEntityHidden = false;
			}
			foreach (Fire Fire in FireManager.Fires)
			{
				if (Fire != null)
					PromoteFireRenderers(Fire);
			}
			return;
		}
		foreach (Enemy Enemy in EnemyManager.Enemies)
		{
			if (Enemy == null)
				continue;
			bool isVisible = IsCellVisible(TilemapGround.WorldToCell(Enemy.transform.position));
			SetRenderersVisible(Enemy.gameObject, isVisible);
			if (Enemy.StunIcon != null)
				SetRenderersVisible(Enemy.StunIcon, isVisible);
		}
		SetEntityVisibility(ItemManager.Items, IsCellVisible);
		foreach (Vehicle Vehicle in VehicleManager.Vehicles)
		{
			if (Vehicle == null)
				continue;
			if (Player != null && Player.IsInVehicle && Player.Vehicle == Vehicle)
			{
				SetRenderersVisible(Vehicle.gameObject, true);
				continue;
			}
			bool isVisible = IsCellVisible(TilemapGround.WorldToCell(Vehicle.transform.position));
			SetRenderersVisible(Vehicle.gameObject, isVisible);
		}
		SetEntityVisibility(StructureManager?.Structures, IsCellVisible);
		foreach (Fire Fire in FireManager.Fires)
		{
			if (Fire == null)
				continue;
			bool isVisible = IsCellVisible(TilemapGround.WorldToCell(Fire.transform.position));
			SetRenderersVisible(Fire.gameObject, isVisible);
			if (isVisible)
				PromoteFireRenderers(Fire);
		}
		anyEntityHidden = true;
	}

	private int ComputeSortingSignature()
	{
		int signature = 17;
		signature = signature * 31 + (EnemyManager != null ? EnemyManager.Enemies.Count : -1);
		signature = signature * 31 + (ItemManager != null ? ItemManager.Items.Count : -1);
		signature = signature * 31 + (VehicleManager != null ? VehicleManager.Vehicles.Count : -1);
		signature = signature * 31 + (StructureManager != null ? StructureManager.Structures.Count : -1);
		return signature;
	}

	private void ConsumeEntityLayers<T>(IEnumerable<T> Entities, ref int topLayerId, ref int topLayerValue) where T : Component
	{
		if (Entities == null)
			return;
		foreach (T Entity in Entities)
			TryConsumeRendererLayer(Entity != null ? Entity.GetComponent<SpriteRenderer>() : null, ref topLayerId, ref topLayerValue);
	}

	private void ConsumeEntityOrders<T>(IEnumerable<T> Entities, int topLayerId, ref int maxWorldOrder) where T : Component
	{
		if (Entities == null)
			return;
		foreach (T Entity in Entities)
			TryConsumeRendererOrder(Entity != null ? Entity.GetComponent<SpriteRenderer>() : null, topLayerId, ref maxWorldOrder);
	}

	private void TryConsumeRendererLayer(Component RendererComponent, ref int topLayerId, ref int topLayerValue)
	{
		if (RendererComponent == null
			|| !TryGetSortingInfo(RendererComponent, out int layerId, out int layerValue)
			|| layerValue < topLayerValue)
			return;
		topLayerValue = layerValue;
		topLayerId = layerId;
	}

	private bool TryGetSortingInfo(Component RendererComponent, out int sortingLayerId, out int sortingLayerValue)
	{
		sortingLayerId = 0;
		sortingLayerValue = 0;
		if (!TryGetSortingOrder(RendererComponent, out int layerId, out _))
			return false;
		string LayerName = SortingLayer.IDToName(layerId);
		if (!string.IsNullOrWhiteSpace(LayerName)
			&& LayerName.IndexOf("ui", StringComparison.OrdinalIgnoreCase) >= 0)
			return false;
		sortingLayerId = layerId;
		foreach (SortingLayer Layer in SortingLayer.layers)
		{
			if (Layer.id != layerId)
				continue;
			sortingLayerValue = Layer.value;
			return true;
		}
		return false;
	}

	private void TryConsumeRendererOrder(Component RendererComponent, int targetLayerId, ref int maxSortingOrder)
	{
		if (RendererComponent == null
			|| !TryGetSortingOrder(RendererComponent, out int layerId, out int sortingOrder)
			|| layerId != targetLayerId)
			return;
		maxSortingOrder = Mathf.Max(maxSortingOrder, sortingOrder);
	}

	private static bool TryGetSortingOrder(Component RendererComponent, out int sortingLayerId, out int sortingOrder)
	{
		sortingLayerId = 0;
		sortingOrder = 0;
		if (RendererComponent is SpriteRenderer SpriteRenderer)
		{
			sortingLayerId = SpriteRenderer.sortingLayerID;
			sortingOrder = SpriteRenderer.sortingOrder;
			return true;
		}
		if (RendererComponent is Tilemap Tilemap
			&& Tilemap.TryGetComponent(out TilemapRenderer TilemapRenderer))
		{
			sortingLayerId = TilemapRenderer.sortingLayerID;
			sortingOrder = TilemapRenderer.sortingOrder;
			return true;
		}
		if (RendererComponent is TilemapRenderer TilemapRendererComponent)
		{
			sortingLayerId = TilemapRendererComponent.sortingLayerID;
			sortingOrder = TilemapRendererComponent.sortingOrder;
			return true;
		}
		return false;
	}

	private void PromoteFireRenderers(Fire Fire)
	{
		RendererSet Renderers = GetRendererSet(Fire.gameObject);
		foreach (SpriteRenderer SpriteRenderer in Renderers.SpriteRenderers)
		{
			if (SpriteRenderer == null)
				continue;
			SpriteRenderer.sortingLayerID = CurrentOverlaySortingLayerId;
			SpriteRenderer.sortingOrder = CurrentFireSortingOrder;
		}
	}

	private RendererSet GetRendererSet(GameObject Object)
	{
		if (RendererCache.TryGetValue(Object, out RendererSet Cached))
			return Cached;
		RendererSet Created = new()
		{
			SpriteRenderers = Object.GetComponentsInChildren<SpriteRenderer>(true),
			MeshRenderers = Object.GetComponentsInChildren<MeshRenderer>(true),
		};
		RendererCache[Object] = Created;
		return Created;
	}

	private void SetRenderersVisible(GameObject Object, bool isVisible)
	{
		if (Object == null)
			return;
		RendererSet Renderers = GetRendererSet(Object);
		foreach (SpriteRenderer SpriteRenderer in Renderers.SpriteRenderers)
		{
			if (SpriteRenderer != null && SpriteRenderer.enabled != isVisible)
				SpriteRenderer.enabled = isVisible;
		}
		foreach (MeshRenderer MeshRenderer in Renderers.MeshRenderers)
		{
			if (MeshRenderer != null && MeshRenderer.enabled != isVisible)
				MeshRenderer.enabled = isVisible;
		}
	}

	private void SetEntityVisibility<T>(IEnumerable<T> Entities, Func<Vector3Int, bool> IsCellVisible) where T : Component
	{
		if (Entities == null)
			return;
		foreach (T Entity in Entities)
		{
			if (Entity == null)
				continue;
			bool isVisible = IsCellVisible(TilemapGround.WorldToCell(Entity.transform.position));
			SetRenderersVisible(Entity.gameObject, isVisible);
		}
	}

	private void SetEntityListVisibility<T>(IEnumerable<T> Entities, bool isVisible) where T : Component
	{
		if (Entities == null)
			return;
		foreach (T Entity in Entities)
		{
			if (Entity != null)
				SetRenderersVisible(Entity.gameObject, isVisible);
		}
	}
}
