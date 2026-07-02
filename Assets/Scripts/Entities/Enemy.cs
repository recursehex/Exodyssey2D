using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
	#region DATA
	[NonSerialized] public EnemyInfo Info;
	public GameObject StunIcon;
	#endregion
	[Header("Debug")]
	[SerializeField] private EnemyInfo.Tags EnemyTag = EnemyInfo.Tags.Unknown;
	[SerializeField] private EnemyInfo.Types EnemyType = EnemyInfo.Types.Unknown;
	[SerializeField] private string EnemyName = string.Empty;
	[SerializeField] private int currentHealth = 0;
	[SerializeField] private int currentEnergy = 0;
	[SerializeField] private bool isStunned = false;
	[SerializeField] private int damagePoints = 0;
	[SerializeField] private int range = 0;
	#region AUDIO
	public AudioClip Move;
	#endregion
	#region PATHFINDING
	public bool IsInMovement { get; private set; } = false;
	public bool WasBlockedThisTurn { get; set; } = false;
	private Tilemap TilemapGround;
	private Tilemap TilemapWalls;
	private Stack<Vector3Int> Path;
	private Vector3Int Destination;
	private Player Player;
	private AStar AStar;
	private Coroutine MoveRoutine;
	private bool isUsingRandomPath = false;
	private const int AdjacentNodeCount = 2;
	private const int RandomPathSearchRadius = 5;
	#endregion
	public void Initialize(Tilemap Ground, Tilemap Walls, EnemyInfo EnemyInfo)
	{
		TilemapGround = Ground;
		TilemapWalls = Walls;
		Info = EnemyInfo;
		AStar = new(TilemapGround, TilemapWalls);
		Player = FindAnyObjectByType<Player>();
		StunIcon = Instantiate(StunIcon, transform.position, Quaternion.identity);
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
			EnemyTag = EnemyInfo.Tags.Unknown;
			EnemyType = EnemyInfo.Types.Unknown;
			EnemyName = string.Empty;
			currentHealth = 0;
			currentEnergy = 0;
			isStunned = false;
			damagePoints = 0;
			range = 0;
			return;
		}
		EnemyTag = Info.Tag;
		EnemyType = Info.Type;
		EnemyName = Info.Name;
		currentHealth = Info.CurrentHealth;
		currentEnergy = Info.CurrentEnergy;
		isStunned = Info.IsStunned;
		damagePoints = Info.DamagePoints;
		range = Info.Range;
	}
#endif
	private void OnDisable() =>	StopMoveRoutineIfRunning();
	private void OnDestroy() => StopMoveRoutineIfRunning();
	private void StopMoveRoutineIfRunning()
	{
		if (MoveRoutine == null)
			return;
		StopCoroutine(MoveRoutine);
		MoveRoutine = null;
	}
	#region MOVEMENT
	/// <summary>
	/// Calculates path for Enemy to move to and handles first move of turn
	/// </summary>
	public void ComputePathAndStartMovement()
	{
		WasBlockedThisTurn = false;
		// Stuns enemy for one turn
		if (Info.IsStunned)
		{
			Info.IsStunned = false;
			return;
		}
		StunIcon.SetActive(false);
		AStar.Initialize();
		AStar.SetAllowDiagonal(false);
		// Track if using a random path (not targeting the player)
		isUsingRandomPath = false;
		// First try to find a complete path to the player
		Path = AStar.ComputePath(transform.position, Player.transform.position);
		// If no complete path is available (other enemy is in the way), try partial pathfinding
		Path ??= AStar.ComputePath(transform.position, Player.transform.position, true);
        // If still no path (enemy is completely blocked from player), try to find a random position within a limited range to move to
        if (Path == null)
        {
            Path = AStar.ComputeRandomPath(transform.position, RandomPathSearchRadius);
            isUsingRandomPath = true;
        }
        // Handle movement for paths with more than 2 nodes OR random paths with exactly 2 nodes
        if (Path != null
			&& HasEnergy
			&& (Path.Count > AdjacentNodeCount || (Path.Count == AdjacentNodeCount && isUsingRandomPath)))
		{
			Info.DecrementEnergy();
			IsInMovement = true;
			// Remove first tile in path
			Path.Pop();
			// Move one tile closer to destination
			Vector3Int TryDistance = Path.Pop();
			Vector3 ShiftedTryDistance = TryDistance + new Vector3(0.5f, 0.5f);
			if (!GameManager.Instance.HasEnemyAtPosition(ShiftedTryDistance) 
				&& !GameManager.Instance.HasVehicleAtPosition(ShiftedTryDistance))
			{
				Destination = TryDistance;
				// Stop movement if game ends
				if (MoveRoutine != null)
                    StopCoroutine(MoveRoutine);
                MoveRoutine = StartCoroutine(MoveAlongPath());
			}
			else
			{
				Path = null;
				IsInMovement = false;
				isUsingRandomPath = false;
				WasBlockedThisTurn = true;
			}
		}
		// If enemy is adjacent to Player, attack (but only if not using a random path)
		else if (Path != null
				&& Path.Count == AdjacentNodeCount
				&& !isUsingRandomPath)
		{
			// A burning enemy prioritizes survival: flee to an unoccupied tile if it can,
			// and only attacks the Player when it has nowhere to escape to
			if (IsOnFire && TryStartFleePath())
				return;
			// Stop attacking if the vehicle is destroyed, so leftover attacks do not hit the ejected player
			while (HasEnergy && AttackPlayer()) { }
		}
		// If no valid movement available
		else
		{
			Path = null;
			IsInMovement = false;
			isUsingRandomPath = false;
			// Mark as blocked if enemy has energy but couldn't move
			if (HasEnergy && Path == null)
				WasBlockedThisTurn = true;
		}
	}
	/// <summary>
	/// True when the enemy is standing on a fire tile
	/// </summary>
	private bool IsOnFire => GameManager.Instance.HasFireAtWorld(transform.position);
	/// <summary>
	/// Starts a flee: the enemy steps off the fire onto the safe tile nearest the player, then re-targets
	/// the player with any remaining energy. Returns false (so the caller can fall back to attacking) if
	/// the enemy has no energy or no safe adjacent tile to escape to.
	/// </summary>
	private bool TryStartFleePath()
	{
		if (!HasEnergy || !TryFindSafeFleeCell(out Vector3Int FleeCell))
			return false;
		IsInMovement = true;
		isUsingRandomPath = false;
		if (MoveRoutine != null)
			StopCoroutine(MoveRoutine);
		MoveRoutine = StartCoroutine(FleeStepThenPursue(FleeCell));
		return true;
	}
	/// <summary>
	/// Finds the adjacent tile nearest the player that the enemy can safely step onto: in bounds and free
	/// of walls, fire, structures, other entities, and the player. Returns false if no such tile exists.
	/// </summary>
	private bool TryFindSafeFleeCell(out Vector3Int Best)
	{
		Best = default;
		Vector3Int EnemyCell = TilemapGround.WorldToCell(transform.position);
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		BoundsInt Size = TilemapGround.cellBounds;
		int bestDistance = int.MaxValue;
		bool found = false;
		foreach (Vector3Int Direction in new[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right })
		{
			Vector3Int Cell = EnemyCell + Direction;
			Vector3 World = Cell + new Vector3(0.5f, 0.5f);
			if (Cell.x < Size.min.x || Cell.x >= Size.max.x || Cell.y < Size.min.y || Cell.y >= Size.max.y)
				continue;
			if (TilemapWalls.HasTile(Cell)
				|| Cell == PlayerCell
				|| GameManager.Instance.HasFireAtPosition(Cell)
				|| GameManager.Instance.HasStructureAtCell(Cell)
				|| GameManager.Instance.HasEnemyAtPosition(World)
				|| GameManager.Instance.HasVehicleAtPosition(World))
				continue;
			// Prefer the safe tile closest to the player so the enemy stays engaged after escaping the fire
			int distance = Mathf.Max(Mathf.Abs(Cell.x - PlayerCell.x), Mathf.Abs(Cell.y - PlayerCell.y));
			if (distance < bestDistance)
			{
				bestDistance = distance;
				Best = Cell;
				found = true;
			}
		}
		return found;
	}
	/// <summary>
	/// Steps off the fire onto the chosen safe tile, then spends any remaining energy pursuing the player
	/// </summary>
	private IEnumerator FleeStepThenPursue(Vector3Int FleeCell)
	{
		Info.DecrementEnergy();
		yield return MoveToCell(FleeCell);
		// The enemy is off the fire now (fire tiles are already obstacles in A*), so re-target the player
		if (HasEnergy)
			yield return PursuePlayer();
		Path = null;
		IsInMovement = false;
		StunIcon.transform.position = transform.position;
		MoveRoutine = null;
		isUsingRandomPath = false;
	}
	/// <summary>
	/// Moves toward the player one tile at a time while energy remains, attacking once adjacent
	/// </summary>
	private IEnumerator PursuePlayer()
	{
		AStar.Initialize();
		AStar.SetAllowDiagonal(false);
		Stack<Vector3Int> PursuitPath = AStar.ComputePath(transform.position, Player.transform.position);
		PursuitPath ??= AStar.ComputePath(transform.position, Player.transform.position, true);
		if (PursuitPath == null || PursuitPath.Count < AdjacentNodeCount)
			yield break;
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		// Remove the tile the enemy is currently on
		PursuitPath.Pop();
		while (PursuitPath.Count > 0 && HasEnergy)
		{
			// Only the player's tile is left: the enemy is adjacent, so attack instead of stepping onto it
			if (PursuitPath.Count == 1)
			{
				if (PursuitPath.Peek() == PlayerCell)
					while (HasEnergy && AttackPlayer()) { }
				yield break;
			}
			Vector3Int Next = PursuitPath.Peek();
			Vector3 NextShifted = Next + new Vector3(0.5f, 0.5f);
			// Stop if the next tile is now occupied
			if (GameManager.Instance.HasEnemyAtPosition(NextShifted)
				|| GameManager.Instance.HasVehicleAtPosition(NextShifted))
				yield break;
			Info.DecrementEnergy();
			PursuitPath.Pop();
			yield return MoveToCell(Next);
		}
	}
	/// <summary>
	/// Smoothly moves the enemy onto the center of the given cell
	/// </summary>
	private IEnumerator MoveToCell(Vector3Int Cell)
	{
		SoundManager.Instance.PlaySound(Move);
		Vector3 ShiftedDistance = Cell + new Vector3(0.5f, 0.5f);
		while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
		{
			transform.position = Vector3.MoveTowards(transform.position,
													 ShiftedDistance,
													 Info.Speed * Time.deltaTime);
			yield return null;
		}
	}
	/// <summary>
	/// Moves Enemy along A* path
	/// </summary>
	private IEnumerator MoveAlongPath()
	{
		// First, move to the initial destination that was already popped
		if (Destination != null)
		{
			SoundManager.Instance.PlaySound(Move);
			Vector3 ShiftedDistance = Destination + new Vector3(0.5f, 0.5f);
			// Move to the destination
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(transform.position, 
														 ShiftedDistance, 
														 Info.Speed * Time.deltaTime);
				yield return null;
			}
		}
		// Then continue with remaining path if any
		while (Path != null && Path.Count > 0)
		{
			// Check for next tile in path
			if (Path.Count > 1 && HasEnergy)
			{
				Vector3Int NextDestination = Path.Peek();
				Vector3 NextShiftedDestination = new(NextDestination.x + 0.5f, NextDestination.y + 0.5f);
				// Check if next destination is still free before moving
				if (!GameManager.Instance.HasEnemyAtPosition(NextShiftedDestination)
					&& !GameManager.Instance.HasVehicleAtPosition(NextShiftedDestination))
				{
					Info.DecrementEnergy();
					Destination = Path.Pop();
					SoundManager.Instance.PlaySound(Move);
					Vector3 ShiftedDistance = Destination + new Vector3(0.5f, 0.5f);
					// Move to next tile
					while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
					{
						transform.position = Vector3.MoveTowards(
							transform.position, 
							ShiftedDistance, 
							Info.Speed * Time.deltaTime);
						yield return null;
					}
				}
				// Stop moving if next position is now occupied
				else break;
			}
			// Enemy attacks Player if enemy moves to an adjacent tile (but not when using random path)
			else if (Path.Count == 1
					&& HasEnergy
					&& !isUsingRandomPath)
			{
				AttackPlayer();
				break;
			}
			// No more moves available
			else break;
		}
		// Clean up after movement ends
		Path = null;
		IsInMovement = false;
		StunIcon.transform.position = transform.position;
		MoveRoutine = null;
		isUsingRandomPath = false;
	}
	#endregion
	#region HEALTH METHODS
	public void DecreaseHealthBy(int damage)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		if (CheatFlags.InvincibleEnemies) return;
#endif
		Info.DecreaseHealthBy(damage);
		if (Info.CurrentHealth <= 0)
			gameObject.SetActive(false);
	}
	#endregion
	#region ENERGY METHODS
	public void RestoreEnergy() => Info.RestoreEnergy();
	public bool HasEnergy => Info.CurrentEnergy > 0;
	#endregion
	#region ATTACK METHODS
	/// <summary>
	/// Attacks the Player, or their Vehicle if they are in one. Returns false if the attack destroyed the
	/// vehicle, signalling the enemy to stop attacking rather than carry damage over to the ejected player.
	/// </summary>
	private bool AttackPlayer()
	{
		Info.DecrementEnergy();
		// If Player is in Vehicle, damage Vehicle and stop if it was destroyed
		if (Player.IsInVehicle)
		{
			bool vehicleDestroyed = GameManager.Instance.DamageVehicle(Player.Vehicle, Info.DamagePoints);
			return !vehicleDestroyed;
		}
		// If Player is not in Vehicle, damage Player
		Player.DecreaseHealthBy(Info.DamagePoints, Info.Range == 0);
		return true;
	}
	#endregion
}
