using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
	public EnemyInfo Info;
	public AudioClip EnemyMove;
	public AudioClip EnemyAttack;
	public AudioClip PlayerAttack;
	public bool isInMovement = false;
	public GameManager GameManager;
	public SoundManager SoundManager;
	#region PATHFINDING
	public Tilemap TilemapGround;
	public Tilemap TilemapWalls;
	private Stack<Vector3Int> Path;
	private Vector3Int Destination;
	private AStar AStar;
	#endregion
	// Start is called before the first frame update
	protected virtual void Start()
	{
		GameManager = GameManager.Instance;
		SoundManager = SoundManager.Instance;
		TilemapGround = GameManager.Instance.TilemapGround;
		TilemapWalls = GameManager.Instance.TilemapWalls;
		AStar = new(TilemapGround, TilemapWalls);
	}
	/// <summary>
	/// Calculates path for Enemy to move to and handles first move of turn
	/// </summary>
	public void ComputePathAndStartMovement(Vector3 Goal)
	{
		// Stuns enemy for one turn
		if (Info.isStunned)
		{
			Info.isStunned = false;
			return;
		}
		AStar.Initialize();
		AStar.SetAllowDiagonal(false);
		Path = AStar.ComputePath(transform.position, Goal, GameManager);
		// Compute path to Player and prevent enemy from colliding into Player
		if (Path != null && Info.currentEnergy > 0 && Path.Count > 2)
		{
			Info.currentEnergy--;
			isInMovement = true;
			// Remove first tile in path
			Path.Pop();
			// Move one tile closer to Player
			Vector3Int TryDistance = Path.Pop();
			Vector3 ShiftedTryDistance = new(TryDistance.x + 0.5f, TryDistance.y + 0.5f, 0);
			if (!GameManager.HasEnemyAtPosition(ShiftedTryDistance))
			{
				Destination = TryDistance;
				MoveAlongPath();
			}
			else
			{
				Path = null;
				isInMovement = false;
			}
		}
		// If enemy is adjacent to Player, attack
		else if (Path != null && Path.Count == 2)
		{
			while (Info.currentEnergy > 0)
			{
				Info.currentEnergy--;
				SoundManager.PlaySound(EnemyAttack);
				GameManager.HandleDamageToPlayer(Info.damagePoints);
			}
		}
		// If no path to Player, do not start moving
		else
		{
			Path = null;
			isInMovement = false;
		}
	}
	/// <summary>
	/// Moves Enemy along A* path
	/// </summary>
	private async void MoveAlongPath()
	{
		while (Path != null && Path.Count > 0)
		{
			SoundManager.PlaySound(EnemyMove);
			Vector3 ShiftedDistance = new(Destination.x + 0.5f, Destination.y + 0.5f, Destination.z);
			// Move one tile closer to Player
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(transform.position, ShiftedDistance, 2 * Time.deltaTime);
				await Task.Yield();
			}
			// Pop next tile in path
			if (Path.Count > 1 && Info.currentEnergy > 0)
			{
				Info.currentEnergy--;
				Destination = Path.Pop();
			}
			// Enemy attacks Player if enemy moves to an adjacent tile
			else if (Path.Count == 1 && Info.currentEnergy > 0)
			{
				Info.currentEnergy--;
				SoundManager.PlaySound(EnemyAttack);
				GameManager.HandleDamageToPlayer(Info.damagePoints);
			}
			else
			{
				Path = null;
				isInMovement = false;
			}
		}
	}
	public void DecreaseHealth(int damage)
	{
		SoundManager.PlaySound(PlayerAttack);
		// NOTE: Eventually add sprite change for enemy on this line using: spriteRenderer.sprite = damagedSprite;
		Info.currentHealth -= damage;
		if (Info.currentHealth <= 0)
		{
			gameObject.SetActive(false);
		}
	}
	public void RestoreEnergy()
	{
		Info.currentEnergy = Info.maxEnergy;
	}
}