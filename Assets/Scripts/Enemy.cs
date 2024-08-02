using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
	public EnemyInfo Info;
	public AudioClip EnemyMove;
	public AudioClip EnemyAttack;
	public AudioClip PlayerAttack;
	public bool isInMovement = false;
	#region PATHFINDING
	public Tilemap TilemapGround;
	public Tilemap TilemapWalls;
	private Stack<Vector3Int> Path;
	private Vector3Int Destination;
	GameManager GameManager;
	SoundManager SoundManager;
	[SerializeField]
	private AStar AStar;
	#endregion
	// Start is called before the first frame update
	protected virtual void Start()
	{
		AStar = new(TilemapGround, TilemapWalls);
	}
	// Player attacks enemy
	public void DamageEnemy(int loss)
	{
		SoundManager.PlaySound(PlayerAttack);
		// NOTE: Eventually add sprite change for enemy on this line using: spriteRenderer.sprite = damagedSprite;
		Info.currentHealth -= loss;
		if (Info.currentHealth <= 0)
		{
			gameObject.SetActive(false);
		}
	}
	void Update()
	{
		if (GameManager.playersTurn)
		{
			return;
		}
		MoveAlongThePath();
	}
	public void MoveAlongThePath()
	{
		if (Path == null)
		{
			return;
		}
		Vector3 ShiftedDistance = new(Destination.x + 0.5f, Destination.y + 0.5f, Destination.z);
		transform.position = Vector3.MoveTowards(transform.position, ShiftedDistance, 2 * Time.deltaTime);
		float distance = Vector3.Distance(ShiftedDistance, transform.position);
		if (distance > 0f)
		{
			return;
		}
		// Move one tile closer to Player
		if (Path.Count > 1 && Info.currentEnergy > 0)
		{
			SoundManager.PlaySound(EnemyMove);
			Destination = Path.Pop();
			Info.currentEnergy--;
		}
		// Enemy attacks Player if enemy moves to an adjacent tile
		else if (Path.Count == 1 && Info.currentEnergy > 0)
		{
			SoundManager.PlaySound(EnemyAttack);
			GameManager.HandleDamageToPlayer(Info.damagePoints);
			Info.currentEnergy--;
		}
		else
		{
			Path = null;
			isInMovement = false;
		}
	}
	public void CalculatePathAndStartMovement(Vector3 Goal)
	{
		isInMovement = true;
		AStar.Initialize();
		AStar.SetAllowDiagonal(false);
		Path = AStar.ComputePath(transform.position, Goal, GameManager);
		// Compute path to Player
		if (Path != null && Info.currentEnergy > 0 && Path.Count > 2) // To stop enemy from colliding into Player
		{
			Info.currentEnergy--;
			// Remove first tile in path
			Path.Pop();
			// Move one tile closer to Player
			Vector3Int TryDistance = Path.Pop();
			Vector3 ShiftedTryDistance = new(TryDistance.x + 0.5f, TryDistance.y + 0.5f, 0);
			if (!GameManager.HasEnemyAtPosition(ShiftedTryDistance))
			{
				Destination = TryDistance;
			}
			else
			{
				Path = null;
				isInMovement = false;
			}
			SoundManager.PlaySound(EnemyMove);
		}
		// If enemy is adjacent to Player, attack
		else
		{
			if (Path != null && Path.Count == 2)
			{
				for (int i = 0; i < Info.currentEnergy; i++)
				{
					SoundManager.PlaySound(EnemyAttack);
					GameManager.HandleDamageToPlayer(Info.damagePoints);
				}
			}
			Path = null;
			isInMovement = false;
		}
	}
	public void RestoreEnergy()
	{
		Info.currentEnergy = Info.maxEnergy;
	}
	public void SetGameManager(GameManager G)
	{
		GameManager = G;
	}
	public void SetSoundManager(SoundManager S)
	{
		SoundManager = S;
	}
}
