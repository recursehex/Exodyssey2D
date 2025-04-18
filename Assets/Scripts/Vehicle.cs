using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Vehicle : MonoBehaviour
{
	#region DATA
	public VehicleInfo Info;
	public Inventory Inventory;
	public Player Player;
	private bool hasBattery = false;
	private bool hasSpotlight = false;
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