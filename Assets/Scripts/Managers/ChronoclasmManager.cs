using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChronoclasmManager : MonoBehaviour
{
	private class ChronoclasmSnapshot
	{
		public Vector3 PlayerPosition;
		public Vehicle Vehicle;
		public Vector3 VehiclePosition;
		public bool wasInVehicle;
		public bool vehicleWasOn;
	}
	private struct UndoSnapshot
	{
		public Vector3 PlayerPosition;
		public Vehicle Vehicle;
		public Vector3 VehiclePosition;
		public int playerEnergy;
		public ItemInfo[] InventoryItems;
		public int selectedInventoryIndex;
		public bool hadGroundItemSnapshot;
		public Vector3 GroundItemPosition;
		public ItemInfo GroundItemInfo;
		public bool hadSpentActionPointsThisTurn;
		public bool wasInVehicle;
		public bool vehicleWasOn;
	}
	[Header("Chronoclasm")]
	[SerializeField] private int chronoclasmGridsRequired = 3;
	private const int chronoclasmEnergyCost = 1;
	private int gridsSinceChronoclasm = 0;
	private bool chronoclasmReadyOverride = false;
	private ChronoclasmSnapshot CurrentChronoclasmSnapshot;
	private string LastChronoclasmFailureReason = string.Empty;
	[Header("Undo")]
	private readonly Stack<UndoSnapshot> UndoSnapshots = new();
	private bool undoBlockedThisTurn = false;
	private string LastUndoBlockReason = string.Empty;
	private bool hasSpentActionPointsThisTurn = false;
	private GameManager GameManager;
	private TurnManager TurnManager;
	private TileManager TileManager;
	private Player Player;
	private Tilemap TilemapGround;
	public bool IsChronoclasmReady => IsChronoclasmUnlocked() && IsChronoclasmOffCooldown();
	public bool HasChronoclasmSnapshot => CurrentChronoclasmSnapshot != null;
	public int ChronoclasmGridsRemaining => Mathf.Max(0, GetChronoclasmCooldownGrids() - gridsSinceChronoclasm);
	public string ChronoclasmStatusLabel => GetChronoclasmStatusLabel();
	public string ChronoclasmFailureReason => LastChronoclasmFailureReason;
	public bool CanUndo => CanUndoNow();
	public string UndoBlockReason => LastUndoBlockReason;
	public void Initialize(GameManager GameManager, TurnManager TurnManager, TileManager TileManager, Player Player, Tilemap TilemapGround)
	{
		this.GameManager = GameManager;
		this.TurnManager = TurnManager;
		this.TileManager = TileManager;
		this.Player = Player;
		this.TilemapGround = TilemapGround;
		ResetForNewRun();
	}
	public void ResetForNewRun()
	{
		ResetChronoclasmState();
		ResetUndoHistoryForTurn();
	}
	public void HandleGridExit()
	{
		HandleGridCompleted();
		ClearChronoclasmSnapshot();
		ResetUndoHistoryForTurn();
	}
	public void OnPlayerTurnStart()
	{
		CaptureChronoclasmSnapshot();
		ResetUndoHistoryForTurn();
	}
	public void OnTurnTimerExpired() => BlockUndoForTurn("Undo blocked after turn timer expired.");
	public void ForceChronoclasmReady() => chronoclasmReadyOverride = true;
	public void ClearChronoclasmReadyOverride() => chronoclasmReadyOverride = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
	private bool chronoclasmUnlockOverride = false;
	/// <summary>
	/// Cheat: makes Chronoclasm immediately usable by bypassing the region lock and cooldown,
	/// marking an AP as spent, and ensuring a destination snapshot exists.
	/// </summary>
	public void Debug_ForceChronoclasmUsable()
	{
		chronoclasmUnlockOverride = true;
		chronoclasmReadyOverride = true;
		hasSpentActionPointsThisTurn = true;
		if (CurrentChronoclasmSnapshot == null)
			CaptureChronoclasmSnapshot();
	}
#endif
	public void RecordUndoSnapshot(bool recordGroundItem = false)
	{
		if (Player == null || TilemapGround == null)
			return;
		if (undoBlockedThisTurn)
			return;
		LastUndoBlockReason = string.Empty;
		UndoSnapshot Snapshot = new();
		Snapshot.PlayerPosition = GetTileCenterPosition(Player.transform.position);
		Snapshot.playerEnergy = Player.CurrentEnergy;
		Snapshot.hadSpentActionPointsThisTurn = hasSpentActionPointsThisTurn;
		Snapshot.wasInVehicle = Player.IsInVehicle;
		Snapshot.Vehicle = Player.Vehicle;
		CaptureInventorySnapshot(ref Snapshot);
		if (recordGroundItem)
			CaptureGroundItemSnapshot(ref Snapshot);
		if (Player.IsInVehicle && Player.Vehicle != null)
		{
			Vector3 VehiclePosition = GetTileCenterPosition(Player.Vehicle.transform.position);
			Snapshot.VehiclePosition = VehiclePosition;
			Snapshot.vehicleWasOn = Player.Vehicle.Info != null && Player.Vehicle.Info.IsOn;
		}
		UndoSnapshots.Push(Snapshot);
	}
	private void CaptureInventorySnapshot(ref UndoSnapshot Snapshot)
	{
		InventoryUI InventoryUI = Player != null ? Player.InventoryUI : null;
		Inventory Inventory = InventoryUI != null ? InventoryUI.Inventory : null;
		if (Inventory == null)
		{
			Snapshot.InventoryItems = null;
			Snapshot.selectedInventoryIndex = -1;
			return;
		}
		ItemInfo[] InventoryItems = new ItemInfo[Inventory.Size];
		for (int i = 0; i < Inventory.Size; i++)
			InventoryItems[i] = Inventory[i]?.Clone();
		Snapshot.InventoryItems = InventoryItems;
		Snapshot.selectedInventoryIndex = InventoryUI.SelectedIndex;
	}
	private void RestoreInventorySnapshot(UndoSnapshot Snapshot)
	{
		InventoryUI InventoryUI = Player != null ? Player.InventoryUI : null;
		Inventory Inventory = InventoryUI != null ? InventoryUI.Inventory : null;
		if (Inventory == null || InventoryUI == null || Snapshot.InventoryItems == null)
			return;
		int slotCount = Mathf.Min(Inventory.Size, Snapshot.InventoryItems.Length);
		for (int i = 0; i < slotCount; i++)
			Inventory[i] = Snapshot.InventoryItems[i];
		for (int i = slotCount; i < Inventory.Size; i++)
			Inventory[i] = null;
		InventoryUI.RefreshInventoryIcons();
		if (Snapshot.selectedInventoryIndex >= 0
			&& Snapshot.selectedInventoryIndex < Inventory.Size
			&& Inventory.HasItemAt(Snapshot.selectedInventoryIndex))
		{
			InventoryUI.SetCurrentSelected(Snapshot.selectedInventoryIndex);
			Player.SelectedItemInfo = Inventory[Snapshot.selectedInventoryIndex];
		}
		else
		{
			InventoryUI.SetNoneSelected();
			Player.SelectedItemInfo = null;
		}
		InventoryUI.SyncSelectionVisuals();
		InventoryUI.RefreshText();
	}
	private void CaptureGroundItemSnapshot(ref UndoSnapshot Snapshot)
	{
		if (GameManager == null || Player == null)
			return;
		Vector3 GroundPosition = GetTileCenterPosition(Player.transform.position);
		Snapshot.hadGroundItemSnapshot = true;
		Snapshot.GroundItemPosition = GroundPosition;
		Item GroundItem = GameManager.GetItemAtPosition(GroundPosition);
		Snapshot.GroundItemInfo = GroundItem != null ? GroundItem.Info?.Clone() : null;
	}
	private void RestoreGroundItemSnapshot(UndoSnapshot Snapshot)
	{
		if (!Snapshot.hadGroundItemSnapshot || GameManager == null)
			return;
		Vector3 GroundPosition = Snapshot.GroundItemPosition;
		Item ExistingItem = GameManager.GetItemAtPosition(GroundPosition);
		if (Snapshot.GroundItemInfo == null)
		{
			if (ExistingItem != null)
				DestroyGroundItem(ExistingItem);
			return;
		}
		if (ExistingItem != null)
			DestroyGroundItem(ExistingItem);
		GameManager.SpawnItem(Snapshot.GroundItemInfo, GroundPosition);
	}
	private void DestroyGroundItem(Item Item)
	{
		if (GameManager == null || Item == null)
			return;
		GameManager.RemoveItemAtPosition(Item);
		Destroy(Item.gameObject);
	}
	public void MarkActionPointSpentThisTurn()
	{
		hasSpentActionPointsThisTurn = true;
		if (LastChronoclasmFailureReason == "Chronoclasm requires an AP-costing action before use.")
			LastChronoclasmFailureReason = string.Empty;
	}
	public void ClearUndoHistory(string Reason)
	{
		if (undoBlockedThisTurn)
			return;
		UndoSnapshots.Clear();
		LastUndoBlockReason = Reason ?? string.Empty;
		if (!string.IsNullOrEmpty(Reason))
			Debug.Log(Reason);
	}
	public bool TryUseChronoclasm()
	{
		if (!CanUseChronoclasm(out string Reason))
		{
			NotifyChronoclasmFailure(Reason);
			return false;
		}
		ChronoclasmSnapshot Snapshot = CurrentChronoclasmSnapshot;
		if (!IsChronoclasmDestinationValid(Snapshot, out Reason))
		{
			NotifyChronoclasmFailure(Reason);
			return false;
		}
		if (!ApplySnapshot(Snapshot.wasInVehicle, Snapshot.Vehicle, Snapshot.PlayerPosition, Snapshot.VehiclePosition, Snapshot.vehicleWasOn))
		{
			NotifyChronoclasmFailure("Chronoclasm failed to restore player position.");
			return false;
		}
		Player.SpendEnergy(chronoclasmEnergyCost);
		if (Player.HasEnergy)
			TurnManager.TurnTimer.RefundElapsedTime();
		gridsSinceChronoclasm = 0;
		LastChronoclasmFailureReason = string.Empty;
		Player.RechargePlasmaRailgun();
		RefreshAfterReposition();
		BlockUndoForTurn("Undo blocked after Chronoclasm.");
		return true;
	}
	public bool TryUndoLastMove()
	{
		if (!CanUndoNow(out string Reason))
		{
			NotifyUndoUnavailable(Reason);
			return false;
		}
		UndoSnapshot Snapshot = UndoSnapshots.Pop();
		if (!ApplySnapshot(Snapshot.wasInVehicle, Snapshot.Vehicle, Snapshot.PlayerPosition, Snapshot.VehiclePosition, Snapshot.vehicleWasOn))
		{
			NotifyUndoUnavailable("Undo failed to restore player position.");
			return false;
		}
		Player.SetEnergy(Snapshot.playerEnergy);
		hasSpentActionPointsThisTurn = Snapshot.hadSpentActionPointsThisTurn;
		RestoreInventorySnapshot(Snapshot);
		RestoreGroundItemSnapshot(Snapshot);
		RefreshAfterReposition();
		return true;
	}
	private bool CanUseChronoclasm(out string Reason)
	{
		Reason = string.Empty;
		if (!IsChronoclasmUnlocked())
		{
			Reason = "Chronoclasm is locked.";
			return false;
		}
		if (!IsChronoclasmOffCooldown())
		{
			Reason = "Chronoclasm is on cooldown.";
			return false;
		}
		if (CurrentChronoclasmSnapshot == null)
		{
			Reason = "Chronoclasm snapshot is missing.";
			return false;
		}
		if (!hasSpentActionPointsThisTurn)
		{
			Reason = "Chronoclasm requires an AP-costing action before use.";
			return false;
		}
		if (GameManager != null && GameManager.IsDoingSetup)
		{
			Reason = "Chronoclasm is unavailable during setup.";
			return false;
		}
		if (TurnManager == null || Player == null)
		{
			Reason = "Chronoclasm is unavailable.";
			return false;
		}
		if (!TurnManager.IsPlayersTurn)
		{
			Reason = "Chronoclasm is only available on the player's turn.";
			return false;
		}
		if (Player.IsInMovement || Player.IsInVehicle && Player.Vehicle != null && Player.Vehicle.IsInMovement)
		{
			Reason = "Chronoclasm is unavailable while moving.";
			return false;
		}
		if (Player.CurrentEnergy < chronoclasmEnergyCost)
		{
			Reason = "Not enough energy for Chronoclasm.";
			return false;
		}
		if (TurnManager.TurnTimer.timeRemaining <= 0f)
		{
			Reason = "Chronoclasm is unavailable after the timer expires.";
			return false;
		}
		return true;
	}
	private bool IsChronoclasmDestinationValid(ChronoclasmSnapshot Snapshot, out string Reason)
	{
		// Chronoclasm blocks if the saved tile is unsafe to avoid altering world state.
		Reason = string.Empty;
		if (Snapshot == null)
		{
			Reason = "Chronoclasm snapshot is missing.";
			return false;
		}
		if (TilemapGround == null || GameManager == null)
		{
			Reason = "Chronoclasm destination cannot be resolved.";
			return false;
		}
		if (Snapshot.wasInVehicle)
		{
			if (Snapshot.Vehicle == null)
			{
				Reason = "Chronoclasm vehicle is missing.";
				return false;
			}
			Vector3Int TargetCellPosition = TilemapGround.WorldToCell(Snapshot.VehiclePosition);
			if (GameManager.HasWallAtPosition(TargetCellPosition))
			{
				Reason = "Chronoclasm destination is blocked by a wall.";
				return false;
			}
			if (GameManager.HasFireAtPosition(TargetCellPosition))
			{
				Reason = "Chronoclasm destination is on fire.";
				return false;
			}
			Vector3 TargetWorldPosition = TargetCellPosition + new Vector3(0.5f, 0.5f);
			if (GameManager.HasEnemyAtPosition(TargetWorldPosition))
			{
				Reason = "Chronoclasm destination is occupied by an enemy.";
				return false;
			}
			Vehicle OccupyingVehicle = GameManager.GetVehicleAtPosition(TargetCellPosition);
			if (OccupyingVehicle != null && OccupyingVehicle != Snapshot.Vehicle)
			{
				Reason = "Chronoclasm destination is occupied by another vehicle.";
				return false;
			}
			return true;
		}
		Vector3Int PlayerCellPosition = TilemapGround.WorldToCell(Snapshot.PlayerPosition);
		if (GameManager.HasWallAtPosition(PlayerCellPosition))
		{
			Reason = "Chronoclasm destination is blocked by a wall.";
			return false;
		}
		if (GameManager.HasFireAtPosition(PlayerCellPosition))
		{
			Reason = "Chronoclasm destination is on fire.";
			return false;
		}
		Vector3 PlayerWorldPosition = PlayerCellPosition + new Vector3(0.5f, 0.5f);
		if (GameManager.HasEnemyAtPosition(PlayerWorldPosition))
		{
			Reason = "Chronoclasm destination is occupied by an enemy.";
			return false;
		}
		if (GameManager.HasVehicleAtPosition(PlayerWorldPosition))
		{
			Reason = "Chronoclasm destination is occupied by a vehicle.";
			return false;
		}
		return true;
	}
	private bool ApplySnapshot(bool wasInVehicle, Vehicle Vehicle, Vector3 PlayerPosition, Vector3 VehiclePosition, bool vehicleWasOn)
	{
		if (Player == null)
			return false;
		if (wasInVehicle)
		{
			if (Vehicle == null)
				return false;
			Vehicle.transform.position = VehiclePosition;
			EnsureVehicleIgnitionState(Vehicle, vehicleWasOn);
			Player.SetVehicleState(Vehicle, true, VehiclePosition);
		}
		else
		{
			Player.SetVehicleState(null, false, PlayerPosition);
		}
		Player.IsInMovement = false;
		return true;
	}
	private void EnsureVehicleIgnitionState(Vehicle Vehicle, bool desiredOn)
	{
		if (Vehicle == null || Vehicle.Info == null)
			return;
		if (Vehicle.Info.IsOn == desiredOn)
			return;
		Vehicle.Info.SwitchIgnition();
	}
	private void CaptureChronoclasmSnapshot()
	{
		if (Player == null || TilemapGround == null)
			return;
		ChronoclasmSnapshot Snapshot = new();
		Snapshot.PlayerPosition = GetTileCenterPosition(Player.transform.position);
		Snapshot.wasInVehicle = Player.IsInVehicle;
		Snapshot.Vehicle = Player.Vehicle;
		if (Player.IsInVehicle && Player.Vehicle != null)
		{
			Vector3 VehiclePosition = GetTileCenterPosition(Player.Vehicle.transform.position);
			Snapshot.VehiclePosition = VehiclePosition;
			Snapshot.vehicleWasOn = Player.Vehicle.Info != null && Player.Vehicle.Info.IsOn;
		}
		CurrentChronoclasmSnapshot = Snapshot;
	}
	private void ClearChronoclasmSnapshot() => CurrentChronoclasmSnapshot = null;
	private void ResetChronoclasmState()
	{
		chronoclasmReadyOverride = false;
		gridsSinceChronoclasm = GetChronoclasmCooldownGrids();
		LastChronoclasmFailureReason = string.Empty;
		ClearChronoclasmSnapshot();
	}
	private void HandleGridCompleted()
	{
		int cooldown = GetChronoclasmCooldownGrids();
		if (cooldown <= 0)
			return;
		gridsSinceChronoclasm = Mathf.Min(gridsSinceChronoclasm + 1, cooldown);
	}
	private void ResetUndoHistoryForTurn()
	{
		UndoSnapshots.Clear();
		undoBlockedThisTurn = false;
		LastUndoBlockReason = string.Empty;
		hasSpentActionPointsThisTurn = false;
	}
	private void BlockUndoForTurn(string Reason)
	{
		if (undoBlockedThisTurn)
			return;
		undoBlockedThisTurn = true;
		LastUndoBlockReason = Reason;
		UndoSnapshots.Clear();
		if (!string.IsNullOrEmpty(Reason))
			Debug.Log(Reason);
	}
	private bool CanUndoNow() => CanUndoNow(out _);
	private bool CanUndoNow(out string Reason)
	{
		Reason = string.Empty;
		if (undoBlockedThisTurn)
		{
			Reason = string.IsNullOrEmpty(LastUndoBlockReason)
				? "Undo is blocked this turn."
				: LastUndoBlockReason;
			return false;
		}
		if (UndoSnapshots.Count == 0)
		{
			Reason = string.IsNullOrEmpty(LastUndoBlockReason)
				? "Undo history is empty."
				: LastUndoBlockReason;
			return false;
		}
		if (GameManager != null && GameManager.IsDoingSetup)
		{
			Reason = "Undo is unavailable during setup.";
			return false;
		}
		if (TurnManager == null || Player == null)
		{
			Reason = "Undo is unavailable.";
			return false;
		}
		if (!TurnManager.IsPlayersTurn)
		{
			Reason = "Undo is only available on the player's turn.";
			return false;
		}
		if (Player.IsInMovement || Player.IsInVehicle && Player.Vehicle != null && Player.Vehicle.IsInMovement)
		{
			Reason = "Undo is unavailable while moving.";
			return false;
		}
		if (TurnManager.TurnTimer.timeRemaining <= 0f)
		{
			Reason = "Undo is unavailable after the timer expires.";
			return false;
		}
		return true;
	}
	private void RefreshAfterReposition()
	{
		if (TileManager != null)
			TileManager.ClearTargets();
		if (GameManager != null)
		{
			GameManager.UpdateTileAreas();
			if (Player != null && !Player.IsInVehicle)
				GameManager.UpdateTargets();
			GameManager.RefreshVisibility();
		}
		TurnManager.SetEndTurnButtonInteractable(true);
	}
	private int GetChronoclasmCooldownGrids() => Mathf.Max(chronoclasmGridsRequired, 0);
	private bool IsChronoclasmUnlocked()
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		if (chronoclasmUnlockOverride)
			return true;
#endif
		RegionInfo CurrentRegion = global::RegionManager.CurrentRegion;
		if (CurrentRegion == null)
			return false;
		return CurrentRegion.Tag >= RegionInfo.Tags.FragmentedCoast;
	}
	private bool IsChronoclasmOffCooldown() => chronoclasmReadyOverride || gridsSinceChronoclasm >= GetChronoclasmCooldownGrids();
	private string GetChronoclasmStatusLabel()
	{
		if (!IsChronoclasmUnlocked())
			return "LOCKED";
		if (IsChronoclasmReady)
			return "READY";
		int gridsRemaining = ChronoclasmGridsRemaining;
		if (gridsRemaining <= 0)
			return "READY";
		return gridsRemaining == 1 ? "1 GRID TO READY" : $"{gridsRemaining} GRIDS TO READY";
	}
	private Vector3 GetTileCenterPosition(Vector3 WorldPosition)
	{
		Vector3Int Cell = TilemapGround.WorldToCell(WorldPosition);
		return Cell + new Vector3(0.5f, 0.5f);
	}
	private void NotifyChronoclasmFailure(string Reason)
	{
		LastChronoclasmFailureReason = Reason ?? string.Empty;
		if (!string.IsNullOrEmpty(Reason))
			Debug.Log(Reason);
	}
	private void NotifyUndoUnavailable(string Reason)
	{
		if (!string.IsNullOrEmpty(Reason))
			Debug.Log(Reason);
	}
}
