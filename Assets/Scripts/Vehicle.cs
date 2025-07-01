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
	#endregion
	#region CHARGE METHODS
	/// <summary>
	/// Uses a Power Cell from inventory to recharge vehicle, returns false if Vehicle is already fully charged
	/// </summary>
	/// <param name="Item"></param>
	/// <returns></returns>
	public bool ClickOnToRecharge(ItemInfo Item)
	{
		int currentUses = Item.CurrentUses;
		// Try to recharge vehicle
		if (!Info.RechargeBy(ref currentUses))
		{
			return false;
		}
		// Update item durability
		Item.DecreaseDurability(currentUses);
		SoundManager.Instance.PlaySound(Select);
		return true;
	}
	/// <summary>
	/// Decreases vehicle's CurrentCharge by amount, returns false if vehicle would have negative charge after decrease
	/// </summary>
	/// <param name="amount"></param>
	/// <returns></returns>
	public bool DecreaseChargeBy(int amount) => Info.DecreaseChargeBy(amount);
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
	/// <returns></returns>
	public bool HasCharge()
	{
		return Info.CurrentCharge > 0;
	}
	#endregion
}