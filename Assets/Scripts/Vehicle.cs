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
			float distance = Vector3.Distance(transform.position, ShiftedDistance);
			float speed = distance / Info.Time;
			// Move vehicle smoothly to next tile
			while (distance > 0f)
			{
				transform.position = Vector3.MoveTowards(
					transform.position, 
					ShiftedDistance, 
					speed * Time.deltaTime);
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
		// When Vehichle stops moving
		Path = null;
		IsInMovement = false;
		MoveRoutine = null;
	}
	/// <summary>
	/// Calculates area Vehicle can move to in a turn based on tile type
	/// </summary>
	public Dictionary<Vector3Int, Node> CalculateArea()
	{
		// Return all road tiles
		throw new System.NotImplementedException();
		// return null;
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
		SoundManager.Instance.PlaySound(Move);
		Info.SwitchIgnition();
	}
	public bool HasFuel()
	{
		return Info.CurrentFuel > 0;
	}
	#endregion
}