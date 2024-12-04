using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Vehicle : MonoBehaviour
{
	public VehicleInfo Info;
	public AudioClip VehicleMove;
	private GameManager GameManager;
	private SoundManager SoundManager;
	#region PATHFINDING
	public Tilemap TilemapGround;
	public Tilemap TilemapWalls;
	private Stack<Vector3Int> Path;
	private Vector3Int Destination;
	private AStar AStar;
	#endregion
	public bool isInMovement = false;
	// Start is called before the first frame update
	void Start()
	{
		GameManager = GameManager.Instance;
		SoundManager = SoundManager.Instance;
	}
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
}
