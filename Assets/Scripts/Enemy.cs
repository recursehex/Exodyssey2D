using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
	#region DATA
	public EnemyInfo Info;
	public GameObject StunIcon;
	#endregion
	#region AUDIO
	public AudioClip Move;
	public AudioClip Attack;
	#endregion
	#region PATHFINDING
	public bool IsInMovement { get; private set; } = false;
	private Tilemap TilemapGround;
	private Tilemap TilemapWalls;
	private Stack<Vector3Int> Path;
	private Vector3Int Destination;
	private Player Player;
	private AStar AStar;
	private Coroutine MoveRoutine;
	#endregion
	public void Initialize(Tilemap Ground, Tilemap Walls, EnemyInfo EnemyInfo, Player Player)
	{
		TilemapGround = Ground;
		TilemapWalls = Walls;
		Info = EnemyInfo;
		AStar = new(TilemapGround, TilemapWalls);
		this.Player = Player;
		StunIcon = Instantiate(StunIcon, transform.position, Quaternion.identity);
	}
	#region MOVEMENT
	/// <summary>
	/// Calculates path for Enemy to move to and handles first move of turn
	/// </summary>
	public void ComputePathAndStartMovement()
	{
		// Stuns enemy for one turn
		if (Info.IsStunned)
		{
			Info.IsStunned = false;
			return;
		}
		StunIcon.SetActive(false);
		AStar.Initialize();
		AStar.SetAllowDiagonal(false);
		// First try to find a complete path to the player
		Path = AStar.ComputePath(transform.position, Player.transform.position);
		// If no complete path is available, try partial pathfinding
		Path ??= AStar.ComputePath(transform.position, Player.transform.position, true);
		// Compute path to Player and prevent enemy from colliding into Player
		if (Path != null && Info.CurrentEnergy > 0 && Path.Count > 2)
		{
			Info.DecrementEnergy();
			IsInMovement = true;
			// Remove first tile in path
			Path.Pop();
			// Move one tile closer to Player
			Vector3Int TryDistance = Path.Pop();
			Vector3 ShiftedTryDistance = TryDistance + new Vector3(0.5f, 0.5f, 0);
			if (!GameManager.Instance.HasEnemyAtPosition(ShiftedTryDistance) 
				&& !GameManager.Instance.HasVehicleAtPosition(ShiftedTryDistance))
			{
				Destination = TryDistance;
				// Stop movement if game ends
				if (MoveRoutine != null)
				{
                    StopCoroutine(MoveRoutine);
				}
                MoveRoutine = StartCoroutine(MoveAlongPath());
			}
			else
			{
				Path = null;
				IsInMovement = false;
			}
		}
		// If enemy is adjacent to Player, attack
		else if (Path != null && Path.Count == 2)
		{
			while (Info.CurrentEnergy > 0)
			{
				AttackPlayer();
			}
		}
		// If no path to Player, do not start moving
		else
		{
			Path = null;
			IsInMovement = false;
		}
	}
	/// <summary>
	/// Moves Enemy along A* path
	/// </summary>
	private IEnumerator MoveAlongPath()
	{
		while (Path != null && Path.Count > 0)
		{
			SoundManager.Instance.PlaySound(Move);
			Vector3 ShiftedDistance = Destination + new Vector3(0.5f, 0.5f, 0);
			// Move one tile closer to Player
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(
					transform.position, 
					ShiftedDistance, 
					Info.Speed * Time.deltaTime);
				yield return null;
			}
			// Pop next tile in path
			if (Path.Count > 1 && Info.CurrentEnergy > 0)
			{
				Vector3Int NextDestination = Path.Peek();
				Vector3 NextShiftedDestination = new(NextDestination.x + 0.5f, NextDestination.y + 0.5f, 0);
				
				// Check if next destination is still free before moving
				if (!GameManager.Instance.HasEnemyAtPosition(NextShiftedDestination)
					&& !GameManager.Instance.HasVehicleAtPosition(NextShiftedDestination))
				{
					Info.DecrementEnergy();
					Destination = Path.Pop();
				}
				else
				{
					// Stop moving if next position is now occupied
					Path = null;
					IsInMovement = false;
					StunIcon.transform.position = transform.position;
				}
			}
			// Enemy attacks Player if enemy moves to an adjacent tile
			else if (Path.Count == 1 && Info.CurrentEnergy > 0)
			{
				AttackPlayer();
			}
			else
			{
				Path = null;
				IsInMovement = false;
				StunIcon.transform.position = transform.position;
			}
		}
		MoveRoutine = null;
	}
	#endregion
	#region HEALTH METHODS
	public void DecreaseHealthBy(int damage)
	{
		Info.DecreaseHealthBy(damage);
		if (Info.CurrentHealth <= 0)
		{
			gameObject.SetActive(false);
		}
	}
	#endregion
	#region ENERGY METHODS
	public void RestoreEnergy()
	{
		Info.RestoreEnergy();
	}
	#endregion
	#region ATTACK METHODS
	private void AttackPlayer()
	{
		Info.DecrementEnergy();
		SoundManager.Instance.PlaySound(Attack);
		// If Player is in Vehicle, damage Vehicle
		if (Player.IsInVehicle)
		{
			// If Vehicle is destroyed, Player is ejected
			if (Player.Vehicle.DecreaseHealthBy(Info.DamagePoints))
			{
				Player.ExitVehicle();
			}
		}
		else
		{
			// If Player is not in Vehicle, damage Player
			Player.DecreaseHealthBy(Info.DamagePoints, Info.Range == 0);
		}
	}
	#endregion
}