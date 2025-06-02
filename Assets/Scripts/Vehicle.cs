using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Vehicle : MonoBehaviour
{
	#region DATA
	public VehicleInfo Info;
	public Inventory Inventory;
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
	#endregion
	public void Initialize(Tilemap Ground, Tilemap Walls, VehicleInfo VehicleInfo)
	{
		TilemapGround = Ground;
		TilemapWalls = Walls;
		Info = VehicleInfo;
		Inventory = new(Info.Storage);
		AStar = new(TilemapGround, TilemapWalls);
	}
	#region MOVEMENT METHODS
	/// <summary>
	/// Vehicle can only move on roads unless canOffroad is true
	/// </summary>
	public void ComputePathAndStartMovement(Vector3 Goal)
	{
		AStar.Initialize();
		Path = AStar.ComputePath(transform.position, Goal);
		if (Path == null)
		{
			return;
		}
		Path.Pop();
		Destination = Path.Pop();
		IsInMovement = true;
		// Stop movement if game ends
		if (MoveRoutine != null)
		{
			StopCoroutine(MoveRoutine);
		}
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
			Vector3 ShiftedDistance = Destination + new Vector3(0.5f, 0.5f, 0);
			// Move vehicle smoothly to next tile
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(
					transform.position, 
					ShiftedDistance, 
					Info.Speed * Time.deltaTime);
				yield return null;
			}
			// Pop next tile in path
			if (Path != null && Path.Count > 0)
			{
				Destination = Path.Pop();
			}
			else
			{
				break;
			}
		}
		// When Vehicle stops moving
		Path = null;
		IsInMovement = false;
		MoveRoutine = null;
	}
	/// <summary>
	/// Calculates area Vehicle can move to in a turn based on movement range
	/// </summary>
	public Dictionary<Vector3Int, Node> CalculateArea()
	{
		if (!Info.IsOn)
		{
			return new();
		}
		AStar.Initialize();
		return AStar.GetReachableAreaByDistance(transform.position, Info.MovementRange);
	}
	#endregion
	#region HEALTH METHODS
	/// <summary>
	/// Returns true if vehicle's health reaches 0
	/// </summary>
	public bool DecreaseHealthBy(int damage)
	{
		SoundManager.Instance.PlaySound(Hurt);
		Info.DecreaseHealthBy(damage);
		if (Info.CurrentHealth <= 0)
		{
			GameManager.Instance.DestroyVehicle(this);
			return true;
		}
		return false;
	}
	public void RestoreHealth()
	{
		Info.RestoreHealth();
	}
	#endregion
	#region FUEL METHODS
	public void RefuelBy(int amount)
	{
		Info.RefuelBy(ref amount);
	}
	/// <summary>
	/// Decreases vehicle's CurrentFuel by amount, returns false if vehicle would have negative fuel after decrease
	/// </summary>
	public bool DecreaseFuelBy(int amount)
	{
		return Info.DecreaseFuelBy(amount);
	}
	public void SwitchIgnition()
	{
		SoundManager.Instance.PlaySound(Select);
		Info.SwitchIgnition();
	}
	public bool HasFuel()
	{
		return Info.CurrentFuel > 0;
	}
	#endregion
}