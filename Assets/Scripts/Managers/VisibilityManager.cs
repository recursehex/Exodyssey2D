using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VisibilityManager : MonoBehaviour
{
	private const int maxLightSources = 32;
	private const int localLightRadius = 1;
	private const int flareRadius = 1;
	private const int lightrodRadius = 2;
	private const int overlaySortingOrder = 32000;
	private const float lightFollowSpeed = 1f;
	[Header("Overlay")]
	[SerializeField] private float transitionSpeed = 6f;
	[SerializeField] private float duskAmbient = 0.62f;
	[Header("Wake Up")]
	// How dark the first grid starts before the darkness recedes (0 = pitch black, 1 = full daylight)
	[SerializeField] private float wakeUpStartAmbient = 0.02f;
	// How quickly the wake-up darkness recedes to daylight; higher is faster
	[SerializeField] private float wakeUpTransitionSpeed = 1f;
	[SerializeField] private float nightAmbient = 0.08f;
	[SerializeField] private float nightVisionAmbient = 0.95f;
	[SerializeField] private float overlayDarkAlpha = 0.95f;
	[SerializeField, Range(0.1f, 1.5f)] private float lightEdgeFeather = 1f;
	[SerializeField, Range(0f, 0.5f)] private float lightCornerRadius = 0.18f;
	[SerializeField, Range(0.1f, 3f)] private float lightFalloffStrength = 0.9f;
	[SerializeField] private float nightVisionTintStrength = 0.24f;
	[SerializeField] private Color nightVisionTint = new(0.33f, 0.82f, 0.35f, 1f);
	private readonly HashSet<Vector3Int> VisibleCells = new();
	private readonly List<Vector4> TargetLightData = new(maxLightSources);
	private readonly List<int> TargetLightKeys = new(maxLightSources);
	private readonly Dictionary<int, Vector4> AppliedLights = new();
	private readonly Dictionary<ItemInfo, int> FlareLightKeys = new();
	private readonly List<int> FadingKeys = new();
	private readonly HashSet<int> ActiveTargetKeys = new();
	private readonly DarknessOverlayRenderer DarknessOverlay = new();
	private readonly EntityVisibilityController EntityVisibility = new();
	private int PlayerLightIndex = -1;
	private int LightrodLightIndex = -1;
	private readonly List<int> InventoryFlareLightIndices = new();
	private readonly List<int> VehicleLightIndices = new();
	private GameManager GameManager;
	private Tilemap TilemapGround;
	private Player Player;
	private LevelManager LevelManager;
	private EnemyManager EnemyManager;
	private ItemManager ItemManager;
	private FireManager FireManager;
	private bool IsInitialized;
	private bool NeedsVisibilityRefresh = true;
	private bool TargetOverlayEnabled;
	// Forces one overlay material apply after targets change, even if no lerp moved
	private bool OverlayNeedsApply = true;
	// True during the first-grid "wake up" effect: the grid starts dark and quickly recedes to daylight,
	// with every light source suppressed (no glow, nothing visible through the darkness) until it clears
	private bool IsWakingUp;
	private float TargetAmbient = 1f;
	private float AppliedAmbient = 1f;
	private float TargetNightVision = 0f;
	private float AppliedNightVision = 0f;
	private int TargetLightCount;
	private Vector3Int LastPlayerCell;
	private Vector3Int LastVehicleCell;
	private bool LastVehicleIgnitionState;
	private bool LastWasInVehicle;
	private bool LastNightVisionState;
	private int nextFlareLightKey = 1000;
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
		this.Player = Player;
		this.LevelManager = LevelManager;
		this.EnemyManager = EnemyManager;
		this.ItemManager = ItemManager;
		this.FireManager = FireManager;
		EntityVisibility.Initialize(TilemapGround, TilemapWalls, TilemapExit, Player, EnemyManager, ItemManager, VehicleManager, StructureManager, FireManager);
		if (this.LevelManager != null)
			this.LevelManager.OnTimeOfDayChanged += HandleTimeOfDayChanged;
		DarknessOverlay.Initialize(transform, TilemapGround != null ? TilemapGround.gameObject.layer : 0);
		EntityVisibility.ConfigureOverlaySorting(DarknessOverlay.Renderer, overlaySortingOrder);
		ApplyOverlayProperties();
		CacheDynamicState();
		IsInitialized = true;
		RefreshVisibility();
	}
	private void OnDestroy()
	{
		if (LevelManager != null)
			LevelManager.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
		DarknessOverlay.Dispose();
		EntityVisibility.ClearCache();
	}
	private void LateUpdate()
	{
		if (!IsInitialized)
			return;
		TrackDynamicState();
		bool rebuilt = NeedsVisibilityRefresh;
		if (rebuilt)
			RebuildVisibilityState();
		UpdateTrackedLightPositions();
		UpdateOverlay();
		// RebuildVisibilityState already applied entity visibility; outside a rebuild,
		// cell occupancy only changes while something is moving between cells, so the
		// per-entity renderer sweep is skipped on idle frames
		if (!rebuilt && IsAnyEntityMoving())
			EntityVisibility.ApplyVisibility(IsVisibilityRestricted, IsCellVisible);
	}
	private bool IsAnyEntityMoving()
	{
		if (Player != null && Player.IsInMovement)
			return true;
		if (Player != null && Player.IsInVehicle && Player.Vehicle != null && Player.Vehicle.IsInMovement)
			return true;
		return EnemyManager != null && EnemyManager.IsProcessingEnemyMovement;
	}
	private bool IsPlayerLightSourceMoving()
	{
		if (Player == null)
			return false;
		if (Player.IsInMovement)
			return true;
		return Player.IsInVehicle
			&& Player.Vehicle != null
			&& Player.Vehicle.IsInMovement;
	}
	private void FlushIfNeeded()
	{
		if (!NeedsVisibilityRefresh)
			return;
		RebuildVisibilityState();
		UpdateTrackedLightPositions();
	}
	public bool IsVisibilityRestricted
	{
		get
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (CheatFlags.RevealAll) return false;
#endif
			return IsNightTime && Player != null && !Player.HasNightVision;
		}
	}
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
	public void RefreshVisibilityImmediately()
	{
		RefreshVisibility();
		FlushIfNeeded();
		AppliedAmbient = TargetAmbient;
		AppliedNightVision = TargetNightVision;
		AppliedLights.Clear();
		for (int i = 0; i < TargetLightCount; i++)
			AppliedLights[TargetLightKeys[i]] = TargetLightData[i];
		OverlayNeedsApply = false;
		DarknessOverlay.SetActive(TargetOverlayEnabled);
		ApplyOverlayProperties();
	}
	/// <summary>
	/// Starts the first-grid "wake up" effect: the whole grid begins dark like night and quickly recedes
	/// to daylight. While waking up, no light sources emit light or show through the darkness.
	/// </summary>
	public void BeginWakeUp()
	{
		if (!IsInitialized)
			return;
		IsWakingUp = true;
		// Snap to darkness so the grid starts black, then the ambient lerps back up to full daylight
		AppliedAmbient = wakeUpStartAmbient;
		AppliedNightVision = 0f;
		// Drop any existing light glows so nothing is visible through the darkness while waking
		AppliedLights.Clear();
		RefreshVisibility();
		FlushIfNeeded();
		DarknessOverlay.SetActive(true);
		ApplyOverlayProperties();
	}
	public void ClearAllLights()
	{
		// Grid transition: force the next rebuild to recompute overlay sorting even
		// if the new grid happens to spawn the same entity counts
		EntityVisibility.InvalidateSorting();
		TargetLightData.Clear();
		TargetLightKeys.Clear();
		TargetLightCount = 0;
		AppliedLights.Clear();
		PlayerLightIndex = -1;
		LightrodLightIndex = -1;
		InventoryFlareLightIndices.Clear();
		VehicleLightIndices.Clear();
		FlareLightKeys.Clear();
		nextFlareLightKey = 1000;
		EntityVisibility.ClearCache();
		TargetAmbient = 1f;
		AppliedAmbient = 1f;
		TargetNightVision = 0f;
		AppliedNightVision = 0f;
		TargetOverlayEnabled = false;
		NeedsVisibilityRefresh = false;
		IsWakingUp = false;
		DarknessOverlay.SetActive(false);
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
			FlareLightKeys.Remove(ItemInfo);
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
			FlareLightKeys.Remove(Item.Info);
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
		bool isLightSourceMoving = IsPlayerLightSourceMoving();
		if (!isLightSourceMoving)
		{
			Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
			if (PlayerCell != LastPlayerCell)
			{
				LastPlayerCell = PlayerCell;
				RefreshVisibility();
			}
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
		if (!isLightSourceMoving)
		{
			Vector3Int VehicleCell = TilemapGround.WorldToCell(Player.Vehicle.transform.position);
			if (VehicleCell != LastVehicleCell)
			{
				LastVehicleCell = VehicleCell;
				RefreshVisibility();
			}
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
		OverlayNeedsApply = true;
		VisibleCells.Clear();
		TargetLightData.Clear();
		TargetLightKeys.Clear();
		TargetLightCount = 0;
		PlayerLightIndex = -1;
		LightrodLightIndex = -1;
		InventoryFlareLightIndices.Clear();
		VehicleLightIndices.Clear();
		EntityVisibility.ConfigureOverlaySorting(DarknessOverlay.Renderer, overlaySortingOrder);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		// Reveal-all cheat: full daylight, every cell visible, no darkness overlay
		if (CheatFlags.RevealAll)
		{
			FillAllCellsVisible();
			TargetAmbient = 1f;
			TargetNightVision = 0f;
			TargetOverlayEnabled = false;
			TargetLightCount = 0;
			EntityVisibility.ApplyVisibility(IsVisibilityRestricted, IsCellVisible);
			return;
		}
#endif
		if (IsWakingUp)
		{
			// The whole grid is uniformly dark and recedes to daylight; keep all cells visible so entities
			// simply darken with the ambient, and emit no light sources so nothing shows through the dark
			FillAllCellsVisible();
			TargetAmbient = 1f;
			TargetNightVision = 0f;
			TargetLightCount = 0;
			// Let the receding ambient keep the overlay on and turn it off once daylight is reached
			TargetOverlayEnabled = false;
			DarknessOverlay.SetActive(true);
			EntityVisibility.ApplyVisibility(IsVisibilityRestricted, IsCellVisible);
			return;
		}
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
		AddLightrodLight(restricted);
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
			DarknessOverlay.SetActive(true);
		EntityVisibility.ApplyVisibility(IsVisibilityRestricted, IsCellVisible);
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
		Vector3Int PlayerCell = GetPlayerLightCell();
		AddCrossArea(PlayerCell, localLightRadius);
	}
	private void AddPlayerLocalLight()
	{
		if (Player == null || TilemapGround == null)
			return;
		PlayerLightIndex = TargetLightData.Count;
		Vector3Int PlayerCell = GetPlayerLightCell();
		AddCrossLightAtCell(PlayerCell, localLightRadius, 0.85f, LightKeyPlayer());
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
		for (int x = VehicleCell.x + 1; x <= GameConfig.Grid.MaxX; x++)
		{
			Vector3Int BeamCell = new(x, VehicleCell.y, 0);
			AddCellIfInsideBounds(BeamCell);
		}
		if (VehicleCell.x >= GameConfig.Grid.MaxX)
			return;
		VehicleLightIndices.Add(TargetLightData.Count);
		AddVehicleBeamLight(Player.Vehicle.transform.position);
	}
	private void AddActiveFlares(bool addVisibilityFootprint)
	{
		if (Player != null)
		{
			Inventory Inventory = Player.InventoryUI != null ? Player.InventoryUI.Inventory : null;
			if (Inventory != null)
			{
				Vector3Int PlayerCell = GetPlayerLightCell();
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
					AddLightAtCell(PlayerCell, flareRadius, 1f, GetFlareLightKey(ItemInfo));
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
			AddLightAtCell(FlareCell, flareRadius, 1f, GetFlareLightKey(Item.Info));
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
				AddSquareArea(FireCell, localLightRadius);
			AddLightAtCell(FireCell, localLightRadius, 1f, LightKeyFire(FireCell));
		}
	}
	private void AddFlareArea(Vector3Int SourceCell)
	{
		AddSquareArea(SourceCell, flareRadius);
	}
	// A selected Lightrod illuminates a 5x5 area centred on the player until it is deselected or dropped.
	// It gives off no light while the player is in a vehicle (only once they exit).
	private void AddLightrodLight(bool addVisibilityFootprint)
	{
		if (Player == null
			|| TilemapGround == null
			|| Player.IsInVehicle
			|| Player.SelectedItemInfo == null
			|| Player.SelectedItemInfo.Tag != ItemInfo.Tags.Lightrod)
		{
			return;
		}
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		if (addVisibilityFootprint)
			AddLightrodArea(PlayerCell);
		LightrodLightIndex = TargetLightData.Count;
		AddLightAtCell(PlayerCell, lightrodRadius, 1f, LightKeyLightrod());
	}
	private void AddLightrodArea(Vector3Int SourceCell)
	{
		AddSquareArea(SourceCell, lightrodRadius);
	}
	private void AddSquareArea(Vector3Int SourceCell, int radius)
	{
		for (int x = -radius; x <= radius; x++)
		{
			for (int y = -radius; y <= radius; y++)
				AddCellIfInsideBounds(SourceCell + new Vector3Int(x, y, 0));
		}
	}
	private void AddCrossArea(Vector3Int SourceCell, int radius)
	{
		AddCellIfInsideBounds(SourceCell);
		for (int offset = 1; offset <= radius; offset++)
		{
			AddCellIfInsideBounds(SourceCell + new Vector3Int(offset, 0, 0));
			AddCellIfInsideBounds(SourceCell + new Vector3Int(-offset, 0, 0));
			AddCellIfInsideBounds(SourceCell + new Vector3Int(0, offset, 0));
			AddCellIfInsideBounds(SourceCell + new Vector3Int(0, -offset, 0));
		}
	}
	private Vector3Int GetPlayerLightCell()
	{
		Vector3 SourcePosition = Player.IsInVehicle && Player.Vehicle != null
			? Player.Vehicle.transform.position
			: Player.transform.position;
		return TilemapGround.WorldToCell(SourcePosition);
	}
	private void UpdateTrackedLightPositions()
	{
		if (Player == null || IsPlayerLightSourceMoving())
			return;
		bool isInVehicle = Player.IsInVehicle && Player.Vehicle != null;
		if (PlayerLightIndex >= 0 && PlayerLightIndex < TargetLightData.Count)
		{
			Vector4 Light = TargetLightData[PlayerLightIndex];
			// Carried lights use the vehicle's final position after movement completes
			Vector3 TrackedPos = isInVehicle
				? Player.Vehicle.transform.position
				: Player.transform.position;
			Light.x = TrackedPos.x;
			Light.y = TrackedPos.y;
			TargetLightData[PlayerLightIndex] = Light;
		}
		// Selected lightrod light follows the player (or vehicle if in one)
		if (LightrodLightIndex >= 0 && LightrodLightIndex < TargetLightData.Count)
		{
			Vector3 TrackedPos = isInVehicle
				? Player.Vehicle.transform.position
				: Player.transform.position;
			Vector4 Light = TargetLightData[LightrodLightIndex];
			Light.x = TrackedPos.x;
			Light.y = TrackedPos.y;
			TargetLightData[LightrodLightIndex] = Light;
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
			foreach (int index in VehicleLightIndices)
			{
				if (index < 0 || index >= TargetLightData.Count)
					continue;
				Vector4 Light = TargetLightData[index];
				float beamLeftEdge = VehicleWorldPos.x + 0.5f;
				float beamRightEdge = GameConfig.Grid.MaxX + 1f;
				float beamHalfWidth = Mathf.Max((beamRightEdge - beamLeftEdge) * 0.5f, 0.0001f);
				Light.x = (beamLeftEdge + beamRightEdge) * 0.5f;
				Light.y = VehicleWorldPos.y;
				Light.z = -beamHalfWidth;
				TargetLightData[index] = Light;
			}
		}
	}
	private void AddCellIfInsideBounds(Vector3Int Cell)
	{
		if (!GridCoordinates.IsInsideGrid(Cell))
			return;
		VisibleCells.Add(Cell);
	}
	private static int LightKeyPlayer() => 1;
	private static int LightKeyLightrod() => 500;
	private static int LightKeyVehicleBeam() => 100;
	private static int LightKeyFire(Vector3Int Cell) => 300000 + (Cell.x + 128) * 512 + (Cell.y + 128);
	private int GetFlareLightKey(ItemInfo ItemInfo)
	{
		if (FlareLightKeys.TryGetValue(ItemInfo, out int key))
			return key;
		key = nextFlareLightKey++;
		FlareLightKeys[ItemInfo] = key;
		return key;
	}
	private void AddLightAtCell(Vector3Int Cell, int radius, float intensity, int key)
	{
		if (TargetLightData.Count >= maxLightSources || TilemapGround == null)
			return;
		Vector3 WorldCenter = TilemapGround.GetCellCenterWorld(Cell);
		// The shader receives the half-extent to the outside edge of the outer cells.
		// A cell radius of 1 therefore covers exactly 3x3 cells, while 2 covers 5x5
		float halfExtent = radius + 0.5f;
		TargetLightData.Add(new Vector4(WorldCenter.x, WorldCenter.y, halfExtent, intensity));
		TargetLightKeys.Add(key);
	}
	private void AddCrossLightAtCell(Vector3Int Cell, int radius, float intensity, int key)
	{
		if (TargetLightData.Count >= maxLightSources || TilemapGround == null)
			return;
		Vector3 WorldCenter = TilemapGround.GetCellCenterWorld(Cell);
		float halfExtent = radius + 0.5f;
		// A negative intensity encodes a cross-shaped light for the overlay shader.
		TargetLightData.Add(new Vector4(WorldCenter.x, WorldCenter.y, halfExtent, -intensity));
		TargetLightKeys.Add(key);
	}
	private void AddVehicleBeamLight(Vector3 VehicleWorldPosition)
	{
		if (TargetLightData.Count >= maxLightSources || TilemapGround == null)
			return;
		float beamLeftEdge = VehicleWorldPosition.x + 0.5f;
		float beamRightEdge = GameConfig.Grid.MaxX + 1f;
		float beamHalfWidth = Mathf.Max((beamRightEdge - beamLeftEdge) * 0.5f, 0.0001f);
		float beamCenterX = (beamLeftEdge + beamRightEdge) * 0.5f;
		// A negative encoded extent tells the shader to render one continuous beam
		TargetLightData.Add(new Vector4(beamCenterX, VehicleWorldPosition.y, -beamHalfWidth, 0.65f));
		TargetLightKeys.Add(LightKeyVehicleBeam());
	}
	private void UpdateOverlay()
	{
		if (!DarknessOverlay.IsReady)
			return;
		float interpolation = Mathf.Clamp01(Time.deltaTime * transitionSpeed);
		float lightFollowDistance = lightFollowSpeed * Time.deltaTime;
		// The wake-up darkness recedes at its own tunable speed so the effect reads as waking up
		float ambientInterpolation = IsWakingUp
			? Mathf.Clamp01(Time.deltaTime * wakeUpTransitionSpeed)
			: interpolation;
		bool overlayChanged = OverlayNeedsApply;
		OverlayNeedsApply = false;
		float newAmbient = LerpWithSnap(AppliedAmbient, TargetAmbient, ambientInterpolation);
		if (newAmbient != AppliedAmbient)
		{
			AppliedAmbient = newAmbient;
			overlayChanged = true;
		}
		float newNightVision = LerpWithSnap(AppliedNightVision, TargetNightVision, interpolation);
		if (newNightVision != AppliedNightVision)
		{
			AppliedNightVision = newNightVision;
			overlayChanged = true;
		}
		// End the wake-up once the grid has brightened back to daylight, then resume normal lighting rules
		if (IsWakingUp && AppliedAmbient >= 0.995f)
		{
			IsWakingUp = false;
			RefreshVisibility();
		}
		// Build active target key set (reused each frame to avoid allocation)
		ActiveTargetKeys.Clear();
		for (int i = 0; i < TargetLightCount; i++)
			ActiveTargetKeys.Add(TargetLightKeys[i]);
		// Fade out removed lights in-place (glow shrink)
		FadingKeys.Clear();
		foreach (var kvp in AppliedLights)
		{
			if (!ActiveTargetKeys.Contains(kvp.Key))
				FadingKeys.Add(kvp.Key);
		}
		for (int i = FadingKeys.Count - 1; i >= 0; i--)
		{
			int key = FadingKeys[i];
			Vector4 current = AppliedLights[key];
			bool isVehicleBeam = current.z < 0f;
			float newRadius;
			float newPositionX = current.x;
			if (isVehicleBeam)
			{
				float currentHalfWidth = -current.z;
				float newHalfWidth = Mathf.Lerp(currentHalfWidth, 0f, interpolation);
				float leftEdge = current.x - currentHalfWidth;
				newPositionX = leftEdge + newHalfWidth;
				newRadius = -newHalfWidth;
			}
			else
			{
				newRadius = Mathf.Lerp(current.z, 0f, interpolation);
			}
			float newIntensity = Mathf.Lerp(current.w, 0f, interpolation);
			if (Mathf.Abs(newRadius) < 0.01f && newIntensity < 0.01f)
				AppliedLights.Remove(key);
			else
				AppliedLights[key] = new Vector4(newPositionX, current.y, newRadius, newIntensity);
			overlayChanged = true;
		}
		// Update existing lights and grow in new lights (glow expand)
		for (int i = 0; i < TargetLightCount; i++)
		{
			int key = TargetLightKeys[i];
			Vector4 Target = TargetLightData[i];
			if (AppliedLights.TryGetValue(key, out Vector4 Current))
			{
				// Once movement completes, follow at a constant world-space speed
				bool isVehicleBeam = Target.z < 0f;
				Vector2 CurrentPosition = isVehicleBeam
					? new Vector2(Current.x + Current.z, Current.y)
					: new Vector2(Current.x, Current.y);
				Vector2 TargetPosition = isVehicleBeam
					? new Vector2(Target.x + Target.z, Target.y)
					: new Vector2(Target.x, Target.y);
				Vector2 UpdatedPosition = Vector2.MoveTowards(
					CurrentPosition,
					TargetPosition,
					lightFollowDistance);
				float updatedRadius = LerpWithSnap(Current.z, Target.z, interpolation);
				float updatedPositionX = UpdatedPosition.x;
				if (isVehicleBeam)
				{
					float currentRightEdge = Current.x - Current.z;
					float targetRightEdge = Target.x - Target.z;
					bool isFullyExpanded = Mathf.Abs(currentRightEdge - targetRightEdge) < 0.01f;
					if (isFullyExpanded)
						updatedRadius = -(targetRightEdge - UpdatedPosition.x) * 0.5f;
					// Preserve the interpolated near edge while the beam grows or follows the vehicle
					updatedPositionX = UpdatedPosition.x - updatedRadius;
				}
				Vector4 Updated = new(
					updatedPositionX,
					UpdatedPosition.y,
					updatedRadius,
					LerpWithSnap(Current.w, Target.w, interpolation));
				if (Updated != Current)
				{
					AppliedLights[key] = Updated;
					overlayChanged = true;
				}
			}
			else
			{
				// Vehicle beams grow rightward from their fixed left edge. Other lights grow outward from their source position
				float startPositionX = Target.z < 0f ? Target.x + Target.z : Target.x;
				AppliedLights[key] = new Vector4(startPositionX, Target.y, 0f, 0f);
				overlayChanged = true;
			}
		}
		// Once every transition has converged there is nothing new to push to the material
		if (!overlayChanged)
			return;
		bool shouldDisplayOverlay = TargetOverlayEnabled
			|| AppliedAmbient < 0.995f
			|| AppliedNightVision > 0.005f
			|| AppliedLights.Count > 0;
		DarknessOverlay.SetActive(shouldDisplayOverlay);
		ApplyOverlayProperties();
	}
	// Lerps asymptote and never land, so values within a hair of the target snap to it,
	// letting the overlay reach a converged state where material writes stop
	private static float LerpWithSnap(float current, float target, float interpolation)
	{
		float value = Mathf.Lerp(current, target, interpolation);
		return Mathf.Abs(value - target) < 0.002f ? target : value;
	}
	private void ApplyOverlayProperties()
	{
		bool capNightBrightness = !IsWakingUp
			&& IsNightTime
			&& Player != null
			&& !Player.HasNightVision;
		DarknessOverlay.Apply(
			AppliedAmbient,
			AppliedNightVision,
			overlayDarkAlpha,
			nightVisionTint,
			lightEdgeFeather,
			lightCornerRadius,
			lightFalloffStrength,
			capNightBrightness ? duskAmbient : 1f,
			AppliedLights);
	}
}
