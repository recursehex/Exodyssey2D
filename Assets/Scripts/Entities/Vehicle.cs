using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Vehicle : MonoBehaviour
{
	#region DATA
	[NonSerialized] public VehicleInfo Info;
	[NonSerialized] public Inventory Inventory;
	#endregion
	[Header("Debug")]
	[SerializeField] private VehicleInfo.Tags VehicleTag = VehicleInfo.Tags.Unknown;
	[SerializeField] private VehicleInfo.Types VehicleType = VehicleInfo.Types.Unknown;
	[SerializeField] private string VehicleName = string.Empty;
	[SerializeField] private int currentHealth = 0;
	[SerializeField] private int currentCharge = 0;
	[SerializeField] private bool isOn = false;
	[SerializeField] private int movementRange = 0;
	#region EVENTS
	public System.Action OnVehicleMovementComplete;
	#endregion
	#region AUDIO
	public AudioClip Move;
	public AudioClip Select;
	public AudioClip Hurt;
	#endregion
	#region PATHFINDING
	public bool IsInMovement { get; set; } = false;
	public Tilemap TilemapGround;
	public Tilemap TilemapWalls;
	private Stack<Vector3Int> Path;
	private Vector3Int Destination;
	private AStar AStar;
	private Coroutine MoveRoutine;
	// Enemy to ram once the vehicle reaches the tile adjacent to it (killed on arrival, costs 1 HP)
	private Enemy RamTarget;
	private Enemy PendingRamTarget;
	private Stack<Vector3Int> PendingPath;
	#endregion
	public void Initialize(Tilemap Ground, Tilemap Walls, VehicleInfo VehicleInfo)
	{
		TilemapGround = Ground;
		TilemapWalls = Walls;
		Info = VehicleInfo;
		Inventory = new(Info.Storage);
		AStar = new(TilemapGround, TilemapWalls);
		// Let pathfinding treat only enemies this vehicle can run over as non-obstacles
		AStar.SetEnemyPassabilityCheck(CanRunOverEnemyAt);
	}
	/// <summary>
	/// Returns true if there is an enemy at the given world position that this vehicle can run over
	/// </summary>
	private bool CanRunOverEnemyAt(Vector3 Position)
	{
		Enemy Enemy = GameManager.Instance.GetEnemyAtPosition(Position);
		return Enemy != null && Info.CanRunOverType(Enemy.Info.Type);
	}
	/// <summary>
	/// Kills any run-over-able enemy on the tile just reached (free, no cost)
	/// </summary>
	private void RunOverEnemyAt(Vector3 Position)
	{
		Enemy Enemy = GameManager.Instance.GetEnemyAtPosition(Position);
		if (Enemy != null && Info.CanRunOverType(Enemy.Info.Type))
			GameManager.Instance.KillEnemy(Enemy);
	}
	/// <summary>
	/// Rams an enemy from an adjacent tile: deals damage equal to the vehicle's health and costs it 1 HP.
	/// Returns false if the ram destroyed this vehicle.
	/// </summary>
	private bool ExecuteRam(Enemy Enemy)
	{
		if (Enemy == null)
			return true;
		GameManager.Instance.DamageEnemy(Enemy, Info.MaxHealth);
		return !DecreaseHealthBy(1);
	}
#if UNITY_EDITOR
	private void LateUpdate()
	{
		SyncDebugFields();
	}
	private void SyncDebugFields()
	{
		if (Info == null)
		{
			VehicleTag = VehicleInfo.Tags.Unknown;
			VehicleType = VehicleInfo.Types.Unknown;
			VehicleName = string.Empty;
			currentHealth = 0;
			currentCharge = 0;
			isOn = false;
			movementRange = 0;
			return;
		}
		VehicleTag = Info.Tag;
		VehicleType = Info.Type;
		VehicleName = Info.Name;
		currentHealth = Info.CurrentHealth;
		currentCharge = Info.CurrentCharge;
		isOn = Info.IsOn;
		movementRange = Info.MovementRange;
	}
#endif
	#region MOVEMENT METHODS
	/// <summary>
	/// Vehicle can only move on roads unless canOffroad is true
	/// </summary>
	public void ComputePathAndStartMovement(Vector3 Goal)
	{
		AStar.Initialize();
		Path = AStar.ComputePath(transform.position, Goal);
		if (Path == null)
			return;
		RamTarget = null;
		Path.Pop();
		Destination = Path.Pop();
		IsInMovement = true;
		// Stop movement if game ends
		if (MoveRoutine != null)
			StopCoroutine(MoveRoutine);
		MoveRoutine = StartCoroutine(MoveAlongPath());
	}
	/// <summary>
	/// Computes a path that stops on the tile directly to the left of a rammable enemy, so the vehicle
	/// rams the enemy on its right. Returns false if that tile cannot be reached within this turn's
	/// movement range. Does not move the vehicle.
	/// </summary>
	public bool PrepareRam(Vector3 EnemyWorldPos, Func<Vector3Int, bool> IsWithinRange)
	{
		PendingRamTarget = null;
		PendingPath = null;
		Enemy Enemy = GameManager.Instance.GetEnemyAtPosition(EnemyWorldPos);
		if (Enemy == null)
			return false;
		Vector3Int StartCell = TilemapGround.WorldToCell(transform.position);
		Vector3Int EnemyCell = TilemapGround.WorldToCell(EnemyWorldPos);
		// The vehicle can only ram from the left: it must end on the tile directly left of the enemy
		Vector3Int ApproachCell = EnemyCell + Vector3Int.left;
		// Already positioned directly left of the enemy: ram in place without moving
		if (StartCell == ApproachCell)
		{
			PendingRamTarget = Enemy;
			return true;
		}
		// The approach tile must be reachable this turn
		if (!IsWithinRange(ApproachCell))
			return false;
		AStar.Initialize();
		Vector3 ApproachWorldPos = ApproachCell + new Vector3(0.5f, 0.5f);
		Stack<Vector3Int> FullPath = AStar.ComputePath(transform.position, ApproachWorldPos);
		if (FullPath == null || FullPath.Count < 2)
			return false;
		PendingRamTarget = Enemy;
		PendingPath = FullPath;
		return true;
	}
	/// <summary>
	/// Begins the movement prepared by PrepareRam, ramming the target once the tile to its left is reached
	/// </summary>
	public void StartPreparedRam()
	{
		RamTarget = PendingRamTarget;
		Path = PendingPath;
		PendingRamTarget = null;
		PendingPath = null;
		IsInMovement = true;
		// If there is no path the vehicle is already left of the enemy and rams in place
		if (Path != null)
		{
			Path.Pop();
			if (Path.Count > 0)
				Destination = Path.Pop();
			else
				Path = null;
		}
		if (MoveRoutine != null)
			StopCoroutine(MoveRoutine);
		MoveRoutine = StartCoroutine(MoveAlongPath());
	}
	/// <summary>
	/// Moves Vehicle along A* path
	/// </summary>
	private IEnumerator MoveAlongPath()
	{
		// While path has remaining tiles
		while (Path != null && Path.Count >= 0)
		{
			SoundManager.Instance.PlaySound(Move);
			Vector3 ShiftedDistance = Destination + new Vector3(0.5f, 0.5f);
			// Move vehicle smoothly to next tile
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(transform.position,
														 ShiftedDistance,
														 Info.Speed * Time.deltaTime);
				yield return null;
			}
			// Crush any run-over-able enemy on the tile just reached
			RunOverEnemyAt(ShiftedDistance);
			// Pop next tile in path
			if (Path != null && Path.Count > 0)
				Destination = Path.Pop();
			else break;
		}
		// Ram the adjacent target, if any, now that the vehicle has reached the penultimate tile
		bool wasDestroyedByRam = false;
		if (RamTarget != null)
		{
			if (!ExecuteRam(RamTarget))
				wasDestroyedByRam = true;
			RamTarget = null;
		}
		// When Vehicle stops moving
		Path = null;
		IsInMovement = false;
		MoveRoutine = null;
		// Notify player that vehicle movement is complete
		OnVehicleMovementComplete?.Invoke();
		// Eject the player and destroy the vehicle if a ram depleted its health
		if (wasDestroyedByRam)
			GameManager.Instance.DestroyRammedVehicle(this);
	}
	/// <summary>
	/// Calculates area Vehicle can move to in a turn based on movement range
	/// </summary>
	public Dictionary<Vector3Int, Node> CalculateArea()
	{
		if (!Info.IsOn)
			return new();
		AStar.Initialize();
		Dictionary<Vector3Int, Node> ReachableArea = AStar.GetReachableAreaByDistance(transform.position, Info.MovementRange);
		Vector3Int StartCell = TilemapGround.WorldToCell(transform.position);
		ReachableArea.Remove(StartCell);
		return ReachableArea;
	}
	#endregion
	#region HEALTH METHODS
	public bool Repair()
	{
		if (Info.TryRestoreHealth())
		{
			SoundManager.Instance.PlaySound(Select);
			return true;
		}
		return false;
	}
	public bool RepairBy(int amount)
	{
		if (Info.TryRestoreHealthBy(amount))
		{
			SoundManager.Instance.PlaySound(Select);
			return true;
		}
		return false;
	}
	/// <summary>
	/// Returns true if vehicle's health reaches 0
	/// </summary>
	public bool DecreaseHealthBy(int damage)
	{
		SoundManager.Instance.PlaySound(Hurt);
		Info.DecreaseHealthBy(damage);
		return Info.CurrentHealth <= 0;
	}
	#endregion
	#region CHARGE METHODS
	/// <summary>
	/// Uses a Power Cell from inventory to recharge vehicle, returns false if Vehicle is already fully charged
	/// </summary>
	public bool ClickOnToRecharge(ItemInfo Item)
	{
		int amount = Item.CurrentUses;
		// Try to recharge vehicle
		if (!Info.TryRechargeBy(ref amount))
			return false;
		// Decrease item durability by amount used to recharge vehicle
		Item.DecreaseDurability(amount);
		SoundManager.Instance.PlaySound(Select);
		return true;
	}
	/// <summary>
	/// Decreases vehicle's CurrentCharge by amount, returns false if vehicle would have negative charge after decrease
	/// </summary>
	public bool DecreaseChargeBy(int amount) => Info.TryDecreaseChargeBy(amount);
	/// <summary>
	/// Toggles vehicle ignition
	/// </summary>
	public void SwitchIgnition()
	{
		SoundManager.Instance.PlaySound(Select);
		Info.SwitchIgnition();
	}
	/// <summary>
	/// Returns true if vehicle has charge remaining
	/// </summary>
	public bool HasCharge() => Info.CurrentCharge > 0;
	#endregion
}
