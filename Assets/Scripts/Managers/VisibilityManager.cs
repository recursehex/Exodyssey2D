using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VisibilityManager : MonoBehaviour
{
	private const int maxLightSources = 32;
	private const int flareRadius = 2;
	private const int overlaySortingOrder = 32000;
	private const int fireSortingOrderOffset = 1;
	[Header("Overlay")]
	[SerializeField] private float transitionSpeed = 6f;
	[SerializeField] private float duskAmbient = 0.62f;
	[SerializeField] private float nightAmbient = 0.08f;
	[SerializeField] private float nightVisionAmbient = 0.95f;
	[SerializeField] private float overlayDarkAlpha = 0.95f;
	[SerializeField] private float nightVisionTintStrength = 0.24f;
	[SerializeField] private Color nightVisionTint = new(0.33f, 0.82f, 0.35f, 1f);
	private readonly HashSet<Vector3Int> VisibleCells = new();
	private readonly List<Vector4> TargetLightData = new(maxLightSources);
	private readonly List<int> TargetLightKeys = new(maxLightSources);
	private readonly Dictionary<int, Vector4> AppliedLights = new();
	private readonly List<int> FadingKeys = new();
	private readonly Vector4[] ShaderLightData = new Vector4[maxLightSources];
	private int PlayerLightIndex = -1;
	private readonly List<int> InventoryFlareLightIndices = new();
	private readonly List<int> VehicleLightIndices = new();
	private GameManager GameManager;
	private Tilemap TilemapGround;
	private Tilemap TilemapWalls;
	private Tilemap TilemapExit;
	private Player Player;
	private LevelManager LevelManager;
	private EnemyManager EnemyManager;
	private ItemManager ItemManager;
	private VehicleManager VehicleManager;
	private StructureManager StructureManager;
	private FireManager FireManager;
	private GameObject OverlayObject;
	private MeshRenderer OverlayRenderer;
	private Material OverlayMaterial;
	private bool IsInitialized;
	private bool NeedsVisibilityRefresh = true;
	private bool TargetOverlayEnabled;
	private float TargetAmbient = 1f;
	private float AppliedAmbient = 1f;
	private float TargetNightVision = 0f;
	private float AppliedNightVision = 0f;
	private int TargetLightCount;
	private int CurrentFireSortingOrder = overlaySortingOrder + fireSortingOrderOffset;
	private Vector3Int LastPlayerCell;
	private Vector3Int LastVehicleCell;
	private bool LastVehicleIgnitionState;
	private bool LastWasInVehicle;
	private bool LastNightVisionState;
	private static readonly int ambientId = Shader.PropertyToID("_Ambient");
	private static readonly int baseDarkColorId = Shader.PropertyToID("_BaseDarkColor");
	private static readonly int nightVisionTintId = Shader.PropertyToID("_NightVisionTint");
	private static readonly int nightVisionStrengthId = Shader.PropertyToID("_NightVisionStrength");
	private static readonly int lightCountId = Shader.PropertyToID("_LightCount");
	private static readonly int lightDataId = Shader.PropertyToID("_LightData");
	public void Initialize(GameManager GameManager,
		Tilemap TilemapGround,
		Tilemap TilemapWalls,
		Tilemap TilemapExit,
		Player Player,
		LevelManager LevelManager,
		EnemyManager EnemyManager,
		ItemManager ItemManager,
		VehicleManager VehicleManager,
		StructureManager StructureManager,
		FireManager FireManager)
	{
		this.GameManager = GameManager;
		this.TilemapGround = TilemapGround;
		this.TilemapWalls = TilemapWalls;
		this.TilemapExit = TilemapExit;
		this.Player = Player;
		this.LevelManager = LevelManager;
		this.EnemyManager = EnemyManager;
		this.ItemManager = ItemManager;
		this.VehicleManager = VehicleManager;
		this.StructureManager = StructureManager;
		this.FireManager = FireManager;
		if (this.LevelManager != null)
			this.LevelManager.OnTimeOfDayChanged += HandleTimeOfDayChanged;
		EnsureOverlay();
		CacheDynamicState();
		IsInitialized = true;
		RefreshVisibility();
	}
	private void OnDestroy()
	{
		if (LevelManager != null)
			LevelManager.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
		if (OverlayMaterial != null)
			Destroy(OverlayMaterial);
		if (OverlayObject != null)
			Destroy(OverlayObject);
	}
	private void Update()
	{
		if (!IsInitialized)
			return;
		TrackDynamicState();
		if (NeedsVisibilityRefresh)
			RebuildVisibilityState();
		UpdateTrackedLightPositions();
		UpdateOverlay();
		ApplyEntityVisibility();
	}
	private void FlushIfNeeded()
	{
		if (!NeedsVisibilityRefresh)
			return;
		RebuildVisibilityState();
		UpdateTrackedLightPositions();
	}
	public bool IsVisibilityRestricted => IsNightTime && Player != null && !Player.HasNightVision;
	public bool IsCellVisible(Vector3Int Cell)
	{
		FlushIfNeeded();
		if (!IsVisibilityRestricted)
			return true;
		return VisibleCells.Contains(Cell);
	}
	public Dictionary<Vector3Int, Node> FilterVisibleCells(Dictionary<Vector3Int, Node> Cells)
	{
		FlushIfNeeded();
		if (Cells == null)
			return null;
		if (!IsVisibilityRestricted)
			return Cells;
		Dictionary<Vector3Int, Node> VisibleOnly = new();
		foreach (KeyValuePair<Vector3Int, Node> Entry in Cells)
		{
			if (VisibleCells.Contains(Entry.Key))
				VisibleOnly[Entry.Key] = Entry.Value;
		}
		return VisibleOnly;
	}
	public void RefreshVisibility()
	{
		NeedsVisibilityRefresh = true;
	}
	public void ClearAllLights()
	{
		TargetLightData.Clear();
		TargetLightKeys.Clear();
		TargetLightCount = 0;
		AppliedLights.Clear();
		PlayerLightIndex = -1;
		InventoryFlareLightIndices.Clear();
		VehicleLightIndices.Clear();
		TargetAmbient = 1f;
		AppliedAmbient = 1f;
		TargetNightVision = 0f;
		AppliedNightVision = 0f;
		TargetOverlayEnabled = false;
		NeedsVisibilityRefresh = false;
		SetOverlayActive(false);
		ApplyOverlayProperties();
	}
	public void TickActiveFlaresOnRoundStart()
	{
		if (!IsInitialized)
			return;
		bool changed = TickInventoryFlares();
		changed |= TickGroundFlares();
		if (changed)
			RefreshVisibility();
	}
	private bool TickInventoryFlares()
	{
		InventoryUI InventoryUI = Player != null ? Player.InventoryUI : null;
		Inventory Inventory = InventoryUI != null ? InventoryUI.Inventory : null;
		if (Inventory == null)
			return false;
		bool changed = false;
		bool inventoryChanged = false;
		bool clearedSelection = false;
		for (int i = 0; i < Inventory.Size; i++)
		{
			ItemInfo ItemInfo = Inventory[i];
			if (ItemInfo == null
				|| ItemInfo.Tag != ItemInfo.Tags.Flare
				|| !ItemInfo.IsActiveFlare)
			{
				continue;
			}
			changed = true;
			if (!ItemInfo.TickActiveFlare())
				continue;
			if (Player.SelectedItemInfo == ItemInfo)
			{
				Player.SelectedItemInfo = null;
				clearedSelection = true;
			}
			Inventory[i] = null;
			inventoryChanged = true;
		}
		if (!changed || InventoryUI == null)
			return changed;
		if (clearedSelection)
		{
			InventoryUI.SetNoneSelected();
			if (GameManager != null)
				GameManager.ClearTargets();
		}
		if (inventoryChanged)
			InventoryUI.RefreshInventoryIcons();
		if (!clearedSelection
			&& InventoryUI.SelectedIndex >= 0
			&& Inventory.HasItemAt(InventoryUI.SelectedIndex))
		{
			InventoryUI.SetCurrentSelected(InventoryUI.SelectedIndex);
		}
		else
		{
			InventoryUI.RefreshText();
		}
		if (GameManager != null)
			GameManager.UpdateTileAreas();
		return true;
	}
	private bool TickGroundFlares()
	{
		if (ItemManager == null)
			return false;
		bool changed = false;
		for (int i = ItemManager.Items.Count - 1; i >= 0; i--)
		{
			Item Item = ItemManager.Items[i];
			if (Item == null || Item.Info == null)
				continue;
			if (Item.Info.Tag != ItemInfo.Tags.Flare || !Item.Info.IsActiveFlare)
				continue;
			changed = true;
			if (!Item.Info.TickActiveFlare())
				continue;
			ItemManager.RemoveItemAtPosition(Item);
			Destroy(Item.gameObject);
		}
		return changed;
	}
	private bool IsNightTime => LevelManager != null && LevelManager.CurrentTimeOfDay == LevelManager.TimeOfDay.Night;
	private bool IsDuskTime => LevelManager != null && LevelManager.CurrentTimeOfDay == LevelManager.TimeOfDay.Dusk;
	private void CacheDynamicState()
	{
		if (Player == null || TilemapGround == null)
			return;
		LastPlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		LastNightVisionState = Player.HasNightVision;
		LastWasInVehicle = Player.IsInVehicle && Player.Vehicle != null;
		if (!LastWasInVehicle)
			return;
		LastVehicleCell = TilemapGround.WorldToCell(Player.Vehicle.transform.position);
		LastVehicleIgnitionState = Player.Vehicle.Info != null && Player.Vehicle.Info.IsOn;
	}
	private void TrackDynamicState()
	{
		if (Player == null || TilemapGround == null)
			return;
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		if (PlayerCell != LastPlayerCell)
		{
			LastPlayerCell = PlayerCell;
			RefreshVisibility();
		}
		bool hasNightVision = Player.HasNightVision;
		if (hasNightVision != LastNightVisionState)
		{
			LastNightVisionState = hasNightVision;
			RefreshVisibility();
		}
		bool isInVehicle = Player.IsInVehicle && Player.Vehicle != null;
		if (isInVehicle != LastWasInVehicle)
		{
			LastWasInVehicle = isInVehicle;
			RefreshVisibility();
		}
		if (!isInVehicle)
			return;
		Vector3Int VehicleCell = TilemapGround.WorldToCell(Player.Vehicle.transform.position);
		if (VehicleCell != LastVehicleCell)
		{
			LastVehicleCell = VehicleCell;
			RefreshVisibility();
		}
		bool isIgnitionOn = Player.Vehicle.Info != null && Player.Vehicle.Info.IsOn;
		if (isIgnitionOn != LastVehicleIgnitionState)
		{
			LastVehicleIgnitionState = isIgnitionOn;
			RefreshVisibility();
		}
	}
	private void HandleTimeOfDayChanged(LevelManager.TimeOfDay TimeOfDay)
	{
		RefreshVisibility();
	}
	private void RebuildVisibilityState()
	{
		NeedsVisibilityRefresh = false;
		VisibleCells.Clear();
		TargetLightData.Clear();
		TargetLightKeys.Clear();
		TargetLightCount = 0;
		PlayerLightIndex = -1;
		InventoryFlareLightIndices.Clear();
		VehicleLightIndices.Clear();
		ConfigureOverlaySorting();
		bool isNight = IsNightTime;
		bool isDusk = IsDuskTime;
		bool playerHasNV = Player != null && Player.HasNightVision;
		bool restricted = IsVisibilityRestricted;
		if (!isNight || playerHasNV)
			FillAllCellsVisible();
		else
		{
			AddPlayerNightFootprint();
			AddVehicleBeam();
			AddPlayerLocalLight();
		}
		AddActiveFlares(restricted);
		AddFireLights(restricted);
		TargetAmbient = 1f;
		TargetNightVision = 0f;
		TargetOverlayEnabled = false;
		if (isDusk)
		{
			TargetAmbient = playerHasNV ? 1f : duskAmbient;
			TargetNightVision = playerHasNV ? nightVisionTintStrength : 0f;
			TargetOverlayEnabled = true;
		}
		else if (isNight)
		{
			TargetOverlayEnabled = true;
			if (playerHasNV)
			{
				TargetAmbient = nightVisionAmbient;
				TargetNightVision = nightVisionTintStrength;
			}
			else
			{
				TargetAmbient = nightAmbient;
			}
		}
		TargetLightCount = Mathf.Min(TargetLightData.Count, maxLightSources);
		if (TargetOverlayEnabled)
			SetOverlayActive(true);
		ApplyEntityVisibility();
	}
	private void FillAllCellsVisible()
	{
		for (int x = GameConfig.Grid.MinX; x <= GameConfig.Grid.MaxX; x++)
		{
			for (int y = GameConfig.Grid.MinY; y <= GameConfig.Grid.MaxY; y++)
				VisibleCells.Add(new Vector3Int(x, y, 0));
		}
	}
	private void AddPlayerNightFootprint()
	{
		if (Player == null || TilemapGround == null)
			return;
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		AddCellIfInsideBounds(PlayerCell);
		AddCellIfInsideBounds(PlayerCell + new Vector3Int(1, 0, 0));
		AddCellIfInsideBounds(PlayerCell + new Vector3Int(-1, 0, 0));
		AddCellIfInsideBounds(PlayerCell + new Vector3Int(0, 1, 0));
		AddCellIfInsideBounds(PlayerCell + new Vector3Int(0, -1, 0));
	}
	private void AddPlayerLocalLight()
	{
		if (Player == null || TilemapGround == null)
			return;
		PlayerLightIndex = TargetLightData.Count;
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		AddLightAtCell(PlayerCell, 1.65f, 0.85f, LightKeyPlayer());
	}
	private void AddVehicleBeam()
	{
		VehicleLightIndices.Clear();
		if (Player == null
			|| !Player.IsInVehicle
			|| Player.Vehicle == null
			|| Player.Vehicle.Info == null
			|| !Player.Vehicle.Info.IsOn
			|| TilemapGround == null)
		{
			return;
		}
		Vector3Int VehicleCell = TilemapGround.WorldToCell(Player.Vehicle.transform.position);
		for (int i = 1; i <= 3; i++)
		{
			Vector3Int BeamCell = VehicleCell + new Vector3Int(i, 0, 0);
			AddCellIfInsideBounds(BeamCell);
			VehicleLightIndices.Add(TargetLightData.Count);
			AddLightAtCell(BeamCell, 1.1f, 0.65f, LightKeyVehicleBeam(i));
		}
	}
	private void AddActiveFlares(bool addVisibilityFootprint)
	{
		if (Player != null)
		{
			Inventory Inventory = Player.InventoryUI != null ? Player.InventoryUI.Inventory : null;
			if (Inventory != null)
			{
				Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
				for (int i = 0; i < Inventory.Size; i++)
				{
					ItemInfo ItemInfo = Inventory[i];
					if (ItemInfo == null
						|| ItemInfo.Tag != ItemInfo.Tags.Flare
						|| !ItemInfo.IsActiveFlare)
					{
						continue;
					}
					if (addVisibilityFootprint)
						AddFlareArea(PlayerCell);
					InventoryFlareLightIndices.Add(TargetLightData.Count);
					AddLightAtCell(PlayerCell, flareRadius + 0.8f, 1f, LightKeyInventoryFlare(i));
				}
			}
		}
		if (ItemManager == null)
			return;
		foreach (Item Item in ItemManager.Items)
		{
			if (Item == null
				|| Item.Info == null
				|| Item.Info.Tag != ItemInfo.Tags.Flare
				|| !Item.Info.IsActiveFlare)
			{
				continue;
			}
			Vector3Int FlareCell = TilemapGround.WorldToCell(Item.transform.position);
			if (addVisibilityFootprint)
				AddFlareArea(FlareCell);
			AddLightAtCell(FlareCell, flareRadius + 0.8f, 1f, LightKeyGroundFlare(FlareCell));
		}
	}
	private void AddFireLights(bool addVisibilityFootprint)
	{
		if (FireManager == null || TilemapGround == null)
			return;
		foreach (Fire Fire in FireManager.Fires)
		{
			if (Fire == null)
				continue;
			Vector3Int FireCell = TilemapGround.WorldToCell(Fire.transform.position);
			if (addVisibilityFootprint)
			{
				for (int x = -1; x <= 1; x++)
				{
					for (int y = -1; y <= 1; y++)
						AddCellIfInsideBounds(FireCell + new Vector3Int(x, y, 0));
				}
			}
			AddLightAtCell(FireCell, 1.7f, 1f, LightKeyFire(FireCell));
		}
	}
	private void AddFlareArea(Vector3Int SourceCell)
	{
		for (int x = -flareRadius; x <= flareRadius; x++)
		{
			for (int y = -flareRadius; y <= flareRadius; y++)
			{
				if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) > flareRadius)
					continue;
				AddCellIfInsideBounds(SourceCell + new Vector3Int(x, y, 0));
			}
		}
	}
	private void UpdateTrackedLightPositions()
	{
		if (Player == null)
			return;
		bool isInVehicle = Player.IsInVehicle && Player.Vehicle != null;
		if (PlayerLightIndex >= 0 && PlayerLightIndex < TargetLightData.Count)
		{
			Vector4 Light = TargetLightData[PlayerLightIndex];
			// Use vehicle position during vehicle movement, otherwise player position
			Vector3 TrackedPos = isInVehicle
				? Player.Vehicle.transform.position
				: Player.transform.position;
			Light.x = TrackedPos.x;
			Light.y = TrackedPos.y;
			TargetLightData[PlayerLightIndex] = Light;
		}
		// Inventory flare lights follow the player (or vehicle if in one)
		if (InventoryFlareLightIndices.Count > 0)
		{
			Vector3 TrackedPos = isInVehicle
				? Player.Vehicle.transform.position
				: Player.transform.position;
			foreach (int index in InventoryFlareLightIndices)
			{
				if (index < 0 || index >= TargetLightData.Count)
					continue;
				Vector4 Light = TargetLightData[index];
				Light.x = TrackedPos.x;
				Light.y = TrackedPos.y;
				TargetLightData[index] = Light;
			}
		}
		if (VehicleLightIndices.Count > 0 && isInVehicle)
		{
			Vector3 VehicleWorldPos = Player.Vehicle.transform.position;
			for (int i = 0; i < VehicleLightIndices.Count; i++)
			{
				int index = VehicleLightIndices[i];
				if (index < 0 || index >= TargetLightData.Count)
					continue;
				Vector4 Light = TargetLightData[index];
				float offsetX = (i + 1);
				Light.x = VehicleWorldPos.x + offsetX;
				Light.y = VehicleWorldPos.y;
				TargetLightData[index] = Light;
			}
		}
	}
	private void AddCellIfInsideBounds(Vector3Int Cell)
	{
		if (Cell.x < GameConfig.Grid.MinX
			|| Cell.x > GameConfig.Grid.MaxX
			|| Cell.y < GameConfig.Grid.MinY
			|| Cell.y > GameConfig.Grid.MaxY)
		{
			return;
		}
		VisibleCells.Add(Cell);
	}
	private static int LightKeyPlayer() => 1;
	private static int LightKeyVehicleBeam(int offset) => 100 + offset;
	private static int LightKeyInventoryFlare(int slot) => 1000 + slot;
	private static int LightKeyGroundFlare(Vector3Int Cell) => 10000 + (Cell.x + 128) * 512 + (Cell.y + 128);
	private static int LightKeyFire(Vector3Int Cell) => 300000 + (Cell.x + 128) * 512 + (Cell.y + 128);
	private void AddLightAtCell(Vector3Int Cell, float radius, float intensity, int key)
	{
		if (TargetLightData.Count >= maxLightSources || TilemapGround == null)
			return;
		Vector3 WorldCenter = TilemapGround.GetCellCenterWorld(Cell);
		TargetLightData.Add(new Vector4(WorldCenter.x, WorldCenter.y, radius, intensity));
		TargetLightKeys.Add(key);
	}
	private void EnsureOverlay()
	{
		if (OverlayObject != null)
			return;
		Shader OverlayShader = Shader.Find("Custom/GridDarknessOverlay");
		if (OverlayShader == null)
		{
			Debug.LogWarning("GridDarknessOverlay shader not found. Night visibility overlay disabled.");
			return;
		}
		OverlayObject = new GameObject("GridDarknessOverlay");
		OverlayObject.transform.SetParent(transform, false);
		OverlayObject.layer = TilemapGround != null ? TilemapGround.gameObject.layer : 0;
		MeshFilter MeshFilter = OverlayObject.AddComponent<MeshFilter>();
		OverlayRenderer = OverlayObject.AddComponent<MeshRenderer>();
		OverlayMaterial = new Material(OverlayShader);
		OverlayRenderer.sharedMaterial = OverlayMaterial;
		ConfigureOverlaySorting();
		MeshFilter.sharedMesh = BuildOverlayMesh();
		SetOverlayActive(false);
		ApplyOverlayProperties();
	}
	private void ConfigureOverlaySorting()
	{
		if (OverlayRenderer == null)
			return;
		int topLayerId = OverlayRenderer.sortingLayerID;
		int topLayerValue = int.MinValue;
		TryConsumeRendererLayer(TilemapGround, ref topLayerId, ref topLayerValue);
		TryConsumeRendererLayer(TilemapWalls, ref topLayerId, ref topLayerValue);
		TryConsumeRendererLayer(TilemapExit, ref topLayerId, ref topLayerValue);
		TryConsumeRendererLayer(Player != null ? Player.GetComponent<SpriteRenderer>() : null, ref topLayerId, ref topLayerValue);
		foreach (Enemy Enemy in EnemyManager != null ? EnemyManager.Enemies : new List<Enemy>())
		{
			TryConsumeRendererLayer(Enemy != null ? Enemy.GetComponent<SpriteRenderer>() : null, ref topLayerId, ref topLayerValue);
		}
		foreach (Item Item in ItemManager != null ? ItemManager.Items : new List<Item>())
		{
			TryConsumeRendererLayer(Item != null ? Item.GetComponent<SpriteRenderer>() : null, ref topLayerId, ref topLayerValue);
		}
		foreach (Vehicle Vehicle in VehicleManager != null ? VehicleManager.Vehicles : new List<Vehicle>())
		{
			TryConsumeRendererLayer(Vehicle != null ? Vehicle.GetComponent<SpriteRenderer>() : null, ref topLayerId, ref topLayerValue);
		}
		foreach (Structure Structure in StructureManager != null ? StructureManager.Structures : new List<Structure>())
		{
			TryConsumeRendererLayer(Structure != null ? Structure.GetComponent<SpriteRenderer>() : null, ref topLayerId, ref topLayerValue);
		}
		if (topLayerValue == int.MinValue && TilemapGround != null && TilemapGround.TryGetComponent(out TilemapRenderer GroundRenderer))
		{
			topLayerId = GroundRenderer.sortingLayerID;
		}
		int maxWorldOrder = int.MinValue;
		TryConsumeRendererOrder(TilemapGround, topLayerId, ref maxWorldOrder);
		TryConsumeRendererOrder(TilemapWalls, topLayerId, ref maxWorldOrder);
		TryConsumeRendererOrder(TilemapExit, topLayerId, ref maxWorldOrder);
		TryConsumeRendererOrder(Player != null ? Player.GetComponent<SpriteRenderer>() : null, topLayerId, ref maxWorldOrder);
		foreach (Enemy Enemy in EnemyManager != null ? EnemyManager.Enemies : new List<Enemy>())
		{
			TryConsumeRendererOrder(Enemy != null ? Enemy.GetComponent<SpriteRenderer>() : null, topLayerId, ref maxWorldOrder);
		}
		foreach (Item Item in ItemManager != null ? ItemManager.Items : new List<Item>())
		{
			TryConsumeRendererOrder(Item != null ? Item.GetComponent<SpriteRenderer>() : null, topLayerId, ref maxWorldOrder);
		}
		foreach (Vehicle Vehicle in VehicleManager != null ? VehicleManager.Vehicles : new List<Vehicle>())
		{
			TryConsumeRendererOrder(Vehicle != null ? Vehicle.GetComponent<SpriteRenderer>() : null, topLayerId, ref maxWorldOrder);
		}
		foreach (Structure Structure in StructureManager != null ? StructureManager.Structures : new List<Structure>())
		{
			TryConsumeRendererOrder(Structure != null ? Structure.GetComponent<SpriteRenderer>() : null, topLayerId, ref maxWorldOrder);
		}
		int desiredOverlayOrder = Mathf.Max(overlaySortingOrder, maxWorldOrder + 1);
		int minCanvasOrder = int.MaxValue;
		Canvas[] Canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
		foreach (Canvas Canvas in Canvases)
		{
			if (Canvas == null || !Canvas.isActiveAndEnabled)
				continue;
			if (Canvas.sortingLayerID != topLayerId)
				continue;
			minCanvasOrder = Mathf.Min(minCanvasOrder, Canvas.sortingOrder);
		}
		if (minCanvasOrder != int.MaxValue && desiredOverlayOrder >= minCanvasOrder)
			desiredOverlayOrder = minCanvasOrder - 1;
		if (desiredOverlayOrder <= maxWorldOrder)
			desiredOverlayOrder = maxWorldOrder + 1;
		OverlayRenderer.sortingLayerID = topLayerId;
		OverlayRenderer.sortingOrder = desiredOverlayOrder;
		CurrentFireSortingOrder = desiredOverlayOrder + fireSortingOrderOffset;
	}
	private void TryConsumeRendererLayer(Component RendererComponent, ref int topLayerId, ref int topLayerValue)
	{
		if (RendererComponent == null)
			return;
		if (!TryGetSortingInfo(RendererComponent, out int layerId, out int layerValue))
			return;
		if (layerValue < topLayerValue)
			return;
		topLayerValue = layerValue;
		topLayerId = layerId;
	}
	private bool TryGetSortingInfo(Component RendererComponent, out int sortingLayerId, out int sortingLayerValue)
	{
		sortingLayerId = 0;
		sortingLayerValue = 0;
		int layerId;
		if (RendererComponent is SpriteRenderer SpriteRenderer)
			layerId = SpriteRenderer.sortingLayerID;
		else if (RendererComponent is Tilemap Tilemap && Tilemap.TryGetComponent(out TilemapRenderer TilemapRenderer))
			layerId = TilemapRenderer.sortingLayerID;
		else if (RendererComponent is TilemapRenderer TilemapRendererComponent)
			layerId = TilemapRendererComponent.sortingLayerID;
		else
			return false;
		string LayerName = SortingLayer.IDToName(layerId);
		if (IsUiSortingLayerName(LayerName))
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
	private static bool IsUiSortingLayerName(string LayerName)
	{
		if (string.IsNullOrWhiteSpace(LayerName))
			return false;
		return LayerName.IndexOf("ui", StringComparison.OrdinalIgnoreCase) >= 0;
	}
	private void TryConsumeRendererOrder(Component RendererComponent, int targetLayerId, ref int maxSortingOrder)
	{
		if (RendererComponent == null)
			return;
		if (!TryGetSortingOrder(RendererComponent, out int layerId, out int sortingOrder))
			return;
		if (layerId != targetLayerId)
			return;
		if (sortingOrder > maxSortingOrder)
			maxSortingOrder = sortingOrder;
	}
	private bool TryGetSortingOrder(Component RendererComponent, out int sortingLayerId, out int sortingOrder)
	{
		sortingLayerId = 0;
		sortingOrder = 0;
		if (RendererComponent is SpriteRenderer SpriteRenderer)
		{
			sortingLayerId = SpriteRenderer.sortingLayerID;
			sortingOrder = SpriteRenderer.sortingOrder;
			return true;
		}
		if (RendererComponent is Tilemap Tilemap && Tilemap.TryGetComponent(out TilemapRenderer TilemapRenderer))
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
	private Mesh BuildOverlayMesh()
	{
		float MinX = GameConfig.Grid.MinX;
		float MinY = GameConfig.Grid.MinY;
		float MaxX = GameConfig.Grid.MaxX + 1f;
		float MaxY = GameConfig.Grid.MaxY + 1f;
		Mesh Mesh = new();
		Mesh.vertices = new Vector3[]
		{
			new(MinX, MinY, 0f),
			new(MaxX, MinY, 0f),
			new(MaxX, MaxY, 0f),
			new(MinX, MaxY, 0f),
		};
		Mesh.uv = new Vector2[]
		{
			new(0f, 0f),
			new(1f, 0f),
			new(1f, 1f),
			new(0f, 1f),
		};
		Mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
		Mesh.RecalculateNormals();
		Mesh.RecalculateBounds();
		return Mesh;
	}
	private void SetOverlayActive(bool isActive)
	{
		if (OverlayRenderer != null)
			OverlayRenderer.enabled = isActive;
	}
	private void UpdateOverlay()
	{
		if (OverlayMaterial == null)
			return;
		float interpolation = Mathf.Clamp01(Time.deltaTime * transitionSpeed);
		AppliedAmbient = Mathf.Lerp(AppliedAmbient, TargetAmbient, interpolation);
		AppliedNightVision = Mathf.Lerp(AppliedNightVision, TargetNightVision, interpolation);
		// Build active target key set
		HashSet<int> activeTargetKeys = new(TargetLightCount);
		for (int i = 0; i < TargetLightCount; i++)
			activeTargetKeys.Add(TargetLightKeys[i]);
		// Fade out removed lights in-place (glow shrink)
		FadingKeys.Clear();
		foreach (var kvp in AppliedLights)
		{
			if (!activeTargetKeys.Contains(kvp.Key))
				FadingKeys.Add(kvp.Key);
		}
		for (int i = FadingKeys.Count - 1; i >= 0; i--)
		{
			int key = FadingKeys[i];
			Vector4 current = AppliedLights[key];
			float newRadius = Mathf.Lerp(current.z, 0f, interpolation);
			float newIntensity = Mathf.Lerp(current.w, 0f, interpolation);
			if (newRadius < 0.01f && newIntensity < 0.01f)
				AppliedLights.Remove(key);
			else
				AppliedLights[key] = new Vector4(current.x, current.y, newRadius, newIntensity);
		}
		// Update existing lights and grow in new lights (glow expand)
		for (int i = 0; i < TargetLightCount; i++)
		{
			int key = TargetLightKeys[i];
			Vector4 target = TargetLightData[i];
			if (AppliedLights.TryGetValue(key, out Vector4 current))
			{
				// Existing light: snap position, lerp radius/intensity
				AppliedLights[key] = new Vector4(target.x, target.y,
					Mathf.Lerp(current.z, target.z, interpolation),
					Mathf.Lerp(current.w, target.w, interpolation));
			}
			else
			{
				// New light: correct position, zero radius — will grow over subsequent frames
				AppliedLights[key] = new Vector4(target.x, target.y, 0f, 0f);
			}
		}
		bool shouldDisplayOverlay = TargetOverlayEnabled
			|| AppliedAmbient < 0.995f
			|| AppliedNightVision > 0.005f
			|| AppliedLights.Count > 0;
		SetOverlayActive(shouldDisplayOverlay);
		ApplyOverlayProperties();
	}
	private void ApplyOverlayProperties()
	{
		if (OverlayMaterial == null)
			return;
		int lightCount = 0;
		foreach (var kvp in AppliedLights)
		{
			if (lightCount >= maxLightSources)
				break;
			ShaderLightData[lightCount] = kvp.Value;
			lightCount++;
		}
		for (int i = lightCount; i < maxLightSources; i++)
			ShaderLightData[i] = Vector4.zero;
		OverlayMaterial.SetFloat(ambientId, AppliedAmbient);
		OverlayMaterial.SetColor(baseDarkColorId, new Color(0f, 0f, 0f, overlayDarkAlpha));
		OverlayMaterial.SetColor(nightVisionTintId, nightVisionTint);
		OverlayMaterial.SetFloat(nightVisionStrengthId, AppliedNightVision);
		OverlayMaterial.SetFloat(lightCountId, lightCount);
		OverlayMaterial.SetVectorArray(lightDataId, ShaderLightData);
	}
	private void ApplyEntityVisibility()
	{
		if (EnemyManager == null || ItemManager == null || VehicleManager == null || FireManager == null || TilemapGround == null)
			return;
		if (!IsVisibilityRestricted)
		{
			SetEntityListVisibility(EnemyManager.Enemies, true);
			foreach (Enemy Enemy in EnemyManager.Enemies)
			{
				if (Enemy != null && Enemy.StunIcon != null)
					SetRenderersVisible(Enemy.StunIcon, true);
			}
			SetEntityListVisibility(ItemManager.Items, true);
			SetEntityListVisibility(VehicleManager.Vehicles, true);
			if (StructureManager != null)
				SetEntityListVisibility(StructureManager.Structures, true);
			SetEntityListVisibility(FireManager.Fires, true);
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
		foreach (Item Item in ItemManager.Items)
		{
			if (Item == null)
				continue;
			bool isVisible = IsCellVisible(TilemapGround.WorldToCell(Item.transform.position));
			SetRenderersVisible(Item.gameObject, isVisible);
		}
		foreach (Vehicle Vehicle in VehicleManager.Vehicles)
		{
			if (Vehicle == null)
				continue;
			// Player's own vehicle is always visible
			if (Player != null && Player.IsInVehicle && Player.Vehicle == Vehicle)
			{
				SetRenderersVisible(Vehicle.gameObject, true);
				continue;
			}
			bool isVisible = IsCellVisible(TilemapGround.WorldToCell(Vehicle.transform.position));
			SetRenderersVisible(Vehicle.gameObject, isVisible);
		}
		if (StructureManager != null)
		{
			foreach (Structure Structure in StructureManager.Structures)
			{
				if (Structure == null)
					continue;
				bool isVisible = IsCellVisible(TilemapGround.WorldToCell(Structure.transform.position));
				SetRenderersVisible(Structure.gameObject, isVisible);
			}
		}
			foreach (Fire Fire in FireManager.Fires)
			{
				if (Fire == null)
					continue;
				bool isVisible = IsCellVisible(TilemapGround.WorldToCell(Fire.transform.position));
				SetRenderersVisible(Fire.gameObject, isVisible);
				if (isVisible)
					PromoteFireRenderers(Fire);
			}
		}
	private void PromoteFireRenderers(Fire Fire)
	{
		if (Fire == null || OverlayRenderer == null)
			return;
		SpriteRenderer[] SpriteRenderers = Fire.GetComponentsInChildren<SpriteRenderer>(true);
		int fireSortingLayerId = OverlayRenderer.sortingLayerID;
		int fireOrder = CurrentFireSortingOrder;
		foreach (SpriteRenderer SpriteRenderer in SpriteRenderers)
		{
			SpriteRenderer.sortingLayerID = fireSortingLayerId;
			SpriteRenderer.sortingOrder = fireOrder;
		}
	}
	private static void SetRenderersVisible(GameObject Object, bool isVisible)
	{
		if (Object == null)
			return;
		SpriteRenderer[] SpriteRenderers = Object.GetComponentsInChildren<SpriteRenderer>(true);
		foreach (SpriteRenderer SpriteRenderer in SpriteRenderers)
			SpriteRenderer.enabled = isVisible;
		MeshRenderer[] MeshRenderers = Object.GetComponentsInChildren<MeshRenderer>(true);
		foreach (MeshRenderer MeshRenderer in MeshRenderers)
			MeshRenderer.enabled = isVisible;
	}
	private static void SetEntityListVisibility<T>(IEnumerable<T> Entities, bool isVisible) where T : Component
	{
		if (Entities == null)
			return;
		foreach (T Entity in Entities)
		{
			if (Entity == null)
				continue;
			SetRenderersVisible(Entity.gameObject, isVisible);
		}
	}
}
