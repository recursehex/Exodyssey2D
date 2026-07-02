#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Uniform result for every cheat verb so the UI buttons and the command parser
/// can report success/failure identically.
/// </summary>
public readonly struct CheatResult
{
	public readonly bool Ok;
	public readonly string Message;
	private CheatResult(bool ok, string message) { Ok = ok; Message = message; }
	public static CheatResult Pass(string message) => new(true, message);
	public static CheatResult Fail(string message) => new(false, message);
}

/// <summary>
/// Single source of truth for every cheat verb. Both the on-screen buttons and the
/// text command parser call into these methods, so a cheat is implemented exactly once.
/// </summary>
public class CheatActions
{
	private readonly GameManager GameManager;
	private readonly Player Player;
	private readonly EnemyManager EnemyManager;
	private readonly ItemManager ItemManager;
	private readonly VehicleManager VehicleManager;
	private readonly FireManager FireManager;
	private readonly LevelManager LevelManager;
	private readonly RegionManager RegionManager;

	public static readonly int ItemCount    = (int)ItemInfo.Tags.Unknown;
	public static readonly int EnemyCount   = (int)EnemyInfo.Tags.Unknown;
	public static readonly int VehicleCount = (int)VehicleInfo.Tags.Unknown;

	public CheatActions(GameManager GameManager)
	{
		this.GameManager = GameManager;
		Player          = GameManager.DebugPlayer;
		EnemyManager    = GameManager.GetComponent<EnemyManager>();
		ItemManager     = GameManager.GetComponent<ItemManager>();
		VehicleManager  = GameManager.GetComponent<VehicleManager>();
		FireManager     = GameManager.GetComponent<FireManager>();
		LevelManager    = GameManager.GetComponent<LevelManager>();
		RegionManager   = GameManager.GetRegionManager();
	}

	private bool Ready => GameManager != null && Player != null;

	#region SHARED HELPERS
	/// <summary>
	/// Builds a dropdown/list of enum names, excluding the trailing "Unknown" sentinel.
	/// </summary>
	public static List<string> EnumOptions<T>() where T : Enum =>
		Enum.GetNames(typeof(T)).Where(Name => Name != "Unknown").ToList();

	public static bool TryParseEnum<T>(string Text, out T Value) where T : struct, Enum =>
		Enum.TryParse(Text, true, out Value) && Enum.IsDefined(typeof(T), Value);

	public Vector3Int PlayerCell => Vector3Int.FloorToInt(Player.transform.position);
	private static Vector3 WorldFromCell(Vector3Int Cell) => Cell + new Vector3(0.5f, 0.5f);

	private static bool IsInGrid(Vector3Int Cell) =>
		Cell.x >= GameConfig.Grid.MinX && Cell.x <= GameConfig.Grid.MaxX
		&& Cell.y >= GameConfig.Grid.MinY && Cell.y <= GameConfig.Grid.MaxY;

	/// <summary>
	/// Reuses the existing collision queries to describe why a cell can't host an entity.
	/// Returns null when the cell is free.
	/// </summary>
	private string BlockReason(Vector3Int Cell, bool blockOnPlayer)
	{
		if (!IsInGrid(Cell))
			return $"({Cell.x},{Cell.y}) is off-grid";
		if (GameManager.HasWallAtPosition(Cell))
			return $"({Cell.x},{Cell.y}) is a wall";
		if (GameManager.HasFireAtPosition(Cell))
			return $"({Cell.x},{Cell.y}) is on fire";
		if (GameManager.HasExitTileAtPosition(Cell))
			return $"({Cell.x},{Cell.y}) is an exit";
		if (GameManager.HasStructureAtCell(Cell))
			return $"({Cell.x},{Cell.y}) is occupied by a structure";
		Vector3 World = WorldFromCell(Cell);
		if (GameManager.HasEnemyAtPosition(World))
			return $"({Cell.x},{Cell.y}) is occupied by an enemy";
		if (GameManager.HasVehicleAtPosition(World))
			return $"({Cell.x},{Cell.y}) is occupied by a vehicle";
		if (blockOnPlayer && World == Player.transform.position)
			return $"({Cell.x},{Cell.y}) is the player's tile";
		return null;
	}

	private bool IsEntitySpawnable(Vector3Int Cell) => BlockReason(Cell, true) == null;

	/// <summary>
	/// Finds the nearest entity-spawnable cell to an origin, for command spawns without coords.
	/// </summary>
	private bool TryFindNearestSpawnCell(Vector3Int Origin, out Vector3Int Result)
	{
		Result = Origin;
		bool found = false;
		int best = int.MaxValue;
		for (int x = GameConfig.Grid.MinX; x <= GameConfig.Grid.MaxX; x++)
		{
			for (int y = GameConfig.Grid.MinY; y <= GameConfig.Grid.MaxY; y++)
			{
				Vector3Int Cell = new(x, y);
				if (!IsEntitySpawnable(Cell))
					continue;
				int distance = Mathf.Abs(x - Origin.x) + Mathf.Abs(y - Origin.y);
				if (distance >= best)
					continue;
				best = distance;
				Result = Cell;
				found = true;
			}
		}
		return found;
	}

	/// <summary>
	/// Refreshes lighting, movement areas, and targets after the world changes.
	/// </summary>
	private void RefreshWorld()
	{
		GameManager.RefreshVisibility();
		GameManager.UpdateTileAreas();
		GameManager.UpdateTargets();
	}
	#endregion

	#region PLACEMENT PREVIEWS
	// Lightweight validity checks for the click-to-place highlight (no side effects).
	public bool IsCellOnGrid(Vector3Int Cell) => IsInGrid(Cell);
	public bool CanPlaceEntity(Vector3Int Cell) => Ready && BlockReason(Cell, true) == null;
	public bool CanPlaceItem(Vector3Int Cell) => Ready && IsInGrid(Cell) && !GameManager.HasWallAtPosition(Cell) && !GameManager.HasFireAtPosition(Cell);
	public bool CanPlaceFire(Vector3Int Cell) => Ready && IsInGrid(Cell) && !GameManager.HasFireAtPosition(Cell);
	public bool CanTeleport(Vector3Int Cell) => Ready && IsInGrid(Cell) && !GameManager.HasWallAtPosition(Cell);
	#endregion

	#region SPAWNING
	public CheatResult SpawnItem(int index, Vector3Int Cell)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		if (index < 0 || index >= ItemCount)
			return CheatResult.Fail($"Invalid item index {index}");
		if (!IsInGrid(Cell)) return CheatResult.Fail($"({Cell.x},{Cell.y}) is off-grid");
		if (GameManager.HasWallAtPosition(Cell)) return CheatResult.Fail($"({Cell.x},{Cell.y}) is a wall");
		if (GameManager.HasFireAtPosition(Cell)) return CheatResult.Fail($"({Cell.x},{Cell.y}) is on fire");
		GameManager.SpawnItem(index, WorldFromCell(Cell));
		GameManager.RefreshVisibility();
		return CheatResult.Pass($"Spawned {(ItemInfo.Tags)index} at ({Cell.x},{Cell.y})");
	}

	public CheatResult SpawnEnemy(int index, Vector3Int Cell)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		if (index < 0 || index >= EnemyCount)
			return CheatResult.Fail($"Invalid enemy index {index}");
		string reason = BlockReason(Cell, true);
		if (reason != null) return CheatResult.Fail(reason);
		GameManager.SpawnEnemy(index, WorldFromCell(Cell));
		RefreshWorld();
		return CheatResult.Pass($"Spawned {(EnemyInfo.Tags)index} at ({Cell.x},{Cell.y})");
	}

	public CheatResult SpawnVehicle(int index, int fuel, Vector3Int Cell)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		if (index < 0 || index >= VehicleCount)
			return CheatResult.Fail($"Invalid vehicle index {index}");
		string reason = BlockReason(Cell, true);
		if (reason != null) return CheatResult.Fail(reason);
		GameManager.SpawnVehicle(index, WorldFromCell(Cell), fuel);
		RefreshWorld();
		string fuelText = fuel < 0 ? "full" : fuel.ToString();
		return CheatResult.Pass($"Spawned {(VehicleInfo.Tags)index} (fuel {fuelText}) at ({Cell.x},{Cell.y})");
	}

	public CheatResult SpawnFire(Vector3Int Cell, bool isWildfire)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		if (!IsInGrid(Cell)) return CheatResult.Fail($"({Cell.x},{Cell.y}) is off-grid");
		bool spawned = GameManager.TrySpawnFire(Cell, isWildfire, true);
		if (!spawned)
			return CheatResult.Fail($"Could not place fire at ({Cell.x},{Cell.y})");
		GameManager.RefreshVisibility();
		return CheatResult.Pass($"Spawned {(isWildfire ? "wildfire" : "fire")} at ({Cell.x},{Cell.y})");
	}

	/// <summary>
	/// Resolves a default entity spawn cell near the player (used by commands without coords).
	/// </summary>
	public bool TryDefaultSpawnCell(out Vector3Int Cell) => TryFindNearestSpawnCell(PlayerCell, out Cell);
	#endregion

	#region PLAYER
	public CheatResult SetHealth(int value)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_SetHealth(value);
		return CheatResult.Pass($"Health set to {Player.Debug_CurrentHealth}/{Player.Debug_MaxHealth}");
	}

	public CheatResult SetMaxHealth(int value)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_SetMaxHealth(value);
		return CheatResult.Pass($"Max health set to {Player.Debug_MaxHealth}");
	}

	public CheatResult RestoreFullHealth()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_RestoreFullHealth();
		return CheatResult.Pass("Health restored to full");
	}

	public CheatResult SetEnergy(int value)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_SetEnergy(value);
		GameManager.UpdateTileAreas();
		return CheatResult.Pass($"Energy set to {Player.CurrentEnergy}");
	}

	public CheatResult SetMaxEnergy(int value)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_SetMaxEnergy(value);
		GameManager.UpdateTileAreas();
		return CheatResult.Pass($"Max energy set to {Player.Debug_MaxEnergy}");
	}

	public CheatResult RestoreFullEnergy()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_RestoreFullEnergy();
		GameManager.UpdateTileAreas();
		return CheatResult.Pass("Energy restored to full");
	}

	public CheatResult ToggleInvincibility()
	{
		CheatFlags.Invincibility = !CheatFlags.Invincibility;
		return CheatResult.Pass($"God mode {OnOff(CheatFlags.Invincibility)}");
	}

	public CheatResult ToggleInfiniteEnergy()
	{
		CheatFlags.InfiniteEnergy = !CheatFlags.InfiniteEnergy;
		if (CheatFlags.InfiniteEnergy && Ready)
		{
			Player.Debug_RestoreFullEnergy();
			GameManager.UpdateTileAreas();
		}
		return CheatResult.Pass($"Infinite energy {OnOff(CheatFlags.InfiniteEnergy)}");
	}

	public CheatResult AddItem(int index)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		if (index < 0 || index >= ItemCount)
			return CheatResult.Fail($"Invalid item index {index}");
		if (!Player.Debug_AddItem((ItemInfo.Tags)index))
			return CheatResult.Fail("Inventory is full");
		return CheatResult.Pass($"Added {(ItemInfo.Tags)index} to inventory");
	}

	public CheatResult ClearInventory()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_ClearInventory();
		GameManager.ClearTargets();
		GameManager.UpdateTileAreas();
		return CheatResult.Pass("Inventory cleared");
	}

	public CheatResult EquipHelmet(bool equipped)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_SetHelmet(equipped);
		return CheatResult.Pass($"Helmet {OnOff(equipped)}");
	}

	public CheatResult EquipVest(bool equipped)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_SetVest(equipped);
		return CheatResult.Pass($"Vest {OnOff(equipped)}");
	}

	public CheatResult EquipNightVision(bool equipped)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_SetNightVision(equipped);
		return CheatResult.Pass($"Night vision {OnOff(equipped)}");
	}

	public CheatResult EquipAll()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_SetHelmet(true);
		Player.Debug_SetVest(true);
		Player.Debug_SetNightVision(true);
		return CheatResult.Pass("Equipped helmet, vest, night vision");
	}

	public CheatResult RemoveAllEquipment()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		Player.Debug_RemoveAllEquipment();
		return CheatResult.Pass("Removed all equipment");
	}

	public CheatResult Teleport(Vector3Int Cell)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		if (!IsInGrid(Cell)) return CheatResult.Fail($"({Cell.x},{Cell.y}) is off-grid");
		if (GameManager.HasWallAtPosition(Cell)) return CheatResult.Fail($"({Cell.x},{Cell.y}) is a wall");
		Vector3 World = WorldFromCell(Cell);
		Player.transform.position = World;
		if (Player.IsInVehicle && Player.Vehicle != null)
			Player.Vehicle.transform.position = World;
		RefreshWorld();
		return CheatResult.Pass($"Teleported to ({Cell.x},{Cell.y})");
	}
	#endregion

	#region WORLD
	public CheatResult SetTimeOfDay(LevelManager.TimeOfDay Time)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		LevelManager.Debug_SetTimeOfDay(Time);
		GameManager.RefreshVisibility();
		return CheatResult.Pass($"Time set to {Time}");
	}

	public CheatResult AdvanceLevel()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		GameManager.DebugAdvanceLevel();
		return CheatResult.Pass("Advanced to next grid");
	}

	public CheatResult RegenerateLevel()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		GameManager.DebugRegenerateLevel();
		return CheatResult.Pass("Regenerated current grid");
	}

	public CheatResult SetLevel(int value)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		LevelManager.Debug_SetLevel(value);
		// Level == total grids completed, so advance the region to match natural progression
		RegionManager.Debug_SetProgressFromTotalGrids(LevelManager.Level);
		GameManager.RefreshVisibility();
		return CheatResult.Pass($"Level {LevelManager.Level} — Day {LevelManager.Day}, {LevelManager.CurrentTimeOfDay}, region {RegionManager.CurrentRegionIndex} ({RegionManager.GetRegionName()})");
	}

	public CheatResult SetDay(int value)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		LevelManager.Debug_SetDay(value);
		return CheatResult.Pass($"Day set to {LevelManager.Day}");
	}

	public CheatResult SetRegion(int index)
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		RegionManager.Debug_SetRegion(index);
		return CheatResult.Pass($"Region set to {index} ({RegionManager.CurrentRegionIndex}: {RegionManager.GetRegionName()})");
	}

	public CheatResult KillAllEnemies()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		EnemyManager.DestroyAllEnemies();
		RefreshWorld();
		return CheatResult.Pass("Killed all enemies");
	}

	public CheatResult RemoveAllItems()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		ItemManager.DestroyAllItems();
		GameManager.RefreshVisibility();
		return CheatResult.Pass("Removed all items");
	}

	public CheatResult RemoveAllVehicles()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		if (Player.IsInVehicle)
			Player.ExitVehicle();
		VehicleManager.DestroyAllVehicles();
		RefreshWorld();
		return CheatResult.Pass("Removed all vehicles");
	}

	public CheatResult ExtinguishAllFires()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		FireManager.DestroyAllFires();
		GameManager.RefreshVisibility();
		return CheatResult.Pass("Extinguished all fires");
	}

	public CheatResult ClearEverything()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		GameManager.DebugCleanupWorldEntities();
		return CheatResult.Pass("Cleared all world entities");
	}

	public CheatResult ForceChronoclasmReady()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		// Bypasses region lock + cooldown + AP requirement so it's usable right away
		GameManager.DebugForceChronoclasmUsable();
		return CheatResult.Pass("Chronoclasm forced usable");
	}

	public CheatResult EndTurn()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		GameManager.OnEndTurnPress();
		return CheatResult.Pass("Turn ended");
	}

	public CheatResult ToggleRevealAll()
	{
		CheatFlags.RevealAll = !CheatFlags.RevealAll;
		if (Ready) GameManager.RefreshVisibility();
		return CheatResult.Pass($"Reveal map {OnOff(CheatFlags.RevealAll)}");
	}
	#endregion

	#region DEBUG
	public CheatResult TriggerGameOver()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		GameManager.GameOver();
		return CheatResult.Pass("Triggered game over");
	}

	public CheatResult RestartGame()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		GameManager.StartNewGame();
		return CheatResult.Pass("Restarted game");
	}

	public CheatResult ToggleInvincibleEnemies()
	{
		CheatFlags.InvincibleEnemies = !CheatFlags.InvincibleEnemies;
		return CheatResult.Pass($"Invincible enemies {OnOff(CheatFlags.InvincibleEnemies)}");
	}

	public CheatResult ToggleFreezeEnemies()
	{
		CheatFlags.FreezeEnemies = !CheatFlags.FreezeEnemies;
		return CheatResult.Pass($"Freeze enemies {OnOff(CheatFlags.FreezeEnemies)}");
	}

	public CheatResult LogEntityPositions()
	{
		if (!Ready) return CheatResult.Fail("Game not ready");
		System.Text.StringBuilder Builder = new();
		Builder.AppendLine("=== Entity Positions ===");
		foreach (Enemy Enemy in EnemyManager.Enemies)
			if (Enemy != null) Builder.AppendLine($"Enemy {Enemy.Info?.Tag}: {Enemy.transform.position}");
		foreach (Item Item in ItemManager.Items)
			if (Item != null) Builder.AppendLine($"Item {Item.Info?.Tag}: {Item.transform.position}");
		foreach (Vehicle Vehicle in VehicleManager.Vehicles)
			if (Vehicle != null) Builder.AppendLine($"Vehicle {Vehicle.Info?.Tag}: {Vehicle.transform.position}");
		foreach (Fire Fire in FireManager.Fires)
			if (Fire != null) Builder.AppendLine($"Fire: {Fire.transform.position}");
		Debug.Log(Builder.ToString());
		return CheatResult.Pass("Logged entity positions to console");
	}

	/// <summary>
	/// Live game-state readout for the Debug tab and the `status` command.
	/// </summary>
	public string GetStatus()
	{
		if (!Ready) return "Game not ready";
		int enemies = EnemyManager != null ? EnemyManager.Enemies.Count : 0;
		int items = ItemManager != null ? ItemManager.Items.Count : 0;
		int vehicles = VehicleManager != null ? VehicleManager.Vehicles.Count : 0;
		int fires = FireManager != null ? FireManager.Fires.Count : 0;
		Vector3Int Cell = PlayerCell;
		return
			$"Level: {LevelManager.Level}   Day: {LevelManager.Day}   Time: {LevelManager.CurrentTimeOfDay}\n" +
			$"Region: {RegionManager.CurrentRegionIndex} ({RegionManager.GetRegionName()})\n" +
			$"Player cell: ({Cell.x},{Cell.y})   HP: {Player.Debug_CurrentHealth}/{Player.Debug_MaxHealth}   EN: {Player.CurrentEnergy}/{Player.Debug_MaxEnergy}\n" +
			$"In vehicle: {Player.IsInVehicle}   Helmet: {Player.Debug_HasHelmet}   Vest: {Player.Debug_HasVest}   NV: {Player.HasNightVision}\n" +
			$"Enemies: {enemies}   Items: {items}   Vehicles: {vehicles}   Fires: {fires}\n" +
			$"Flags  God:{CheatFlags.Invincibility} InfEN:{CheatFlags.InfiniteEnergy} InvEnemy:{CheatFlags.InvincibleEnemies} Freeze:{CheatFlags.FreezeEnemies} Reveal:{CheatFlags.RevealAll}";
	}
	#endregion

	private static string OnOff(bool value) => value ? "ON" : "OFF";
}
#endif
