using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Vehicle : MonoBehaviour
{
	#region DATA
	public VehicleInfo Info;
	public Inventory Inventory;
	public Player Player;
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
	#endregion
	public void Initialize(Tilemap Ground, Tilemap Walls, VehicleInfo VehicleInfo, Player Player)
	{
		TilemapGround = Ground;
		TilemapWalls = Walls;
		Info = VehicleInfo;
		Inventory = new(Info.Storage);
		AStar = new(TilemapGround, TilemapWalls);
		this.Player = Player;
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
		MoveAlongPath();
	}
	/// <summary>
	/// Moves Vehicle along A* path
	/// </summary>
	private async void MoveAlongPath()
	{
		// While path has remaining tiles
		while (Path != null && Path.Count >= 0)
		{
			SoundManager.Instance.PlaySound(Move);
			Vector3 ShiftedDistance = new(Destination.x + 0.5f, Destination.y + 0.5f, Destination.z);
			float distance = Vector3.Distance(transform.position, ShiftedDistance);
			float speed = distance / Info.Time;
			// Move vehicle smoothly to next tile
			while (distance > 0f)
			{
				transform.position = Vector3.MoveTowards(
					transform.position, 
					ShiftedDistance, 
					speed * Time.deltaTime);
				await Task.Yield();
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
	}
	/// <summary>
	/// Calculates area Vehicle can move to in a turn based on tile type
	/// </summary>
	public Dictionary<Vector3Int, Node> CalculateArea()
	{
		// Return all road tiles
		return null;
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
		if (Info.CurrentFuel - amount < 0)
		{
			return false;
		}
		Info.DecreaseHealthBy(amount);
		return true;
	}
	#endregion
}