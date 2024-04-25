using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
	public EnemyInfo info;
	public AudioClip enemyMove;
	public AudioClip enemyAttack;
	public AudioClip playerAttack;
	public bool isInMovement = false;
	#region PATHFINDING
	public Tilemap tilemapGround;
	public Tilemap tilemapWalls;
	private Stack<Vector3Int> path;
	private Vector3Int destination;
	GameManager gameManager;
	SoundManager soundManager;
	[SerializeField]
	private AStar astar;
	#endregion
	// Start is called before the first frame update
	protected virtual void Start()
	{
		astar = new AStar
		{
			tilemapGround = tilemapGround,
			tilemapWalls = tilemapWalls
		};
	}
	// Player attacks enemy
	public void DamageEnemy(int loss)
	{
		soundManager.PlaySound(playerAttack);
		// NOTE: Eventually add sprite change for enemy on this line using: spriteRenderer.sprite = dmgSprite;
		info.currentHealth -= loss;
		if (info.currentHealth <= 0)
		{
			gameObject.SetActive(false);
		}
	}
	void Update()
	{
		if (gameManager.playersTurn)
		{
			return;
		}
		MoveAlongThePath();
	}
	public void MoveAlongThePath()
	{
		if (path == null)
		{
			return;
		}
		Vector3 shiftedDistance = new(destination.x + 0.5f, destination.y + 0.5f, destination.z);
		transform.position = Vector3.MoveTowards(transform.position, shiftedDistance, 2 * Time.deltaTime);
		float distance = Vector3.Distance(shiftedDistance, transform.position);
		if (distance > 0f)
		{
			return;
		}
		// Move one tile closer to Player
		if (path.Count > 1 && info.currentEnergy > 0)
		{
			soundManager.PlaySound(enemyMove);
			destination = path.Pop();
			info.currentEnergy--;
		}
		// Enemy attacks Player if enemy moves to an adjacent tile
		else if (path.Count == 1 && info.currentEnergy > 0)
		{
			soundManager.PlaySound(enemyAttack);
			gameManager.HandleDamageToPlayer(info.damagePoints);
			info.currentEnergy--;
		}
		else
		{
			path = null;
			isInMovement = false;
		}
	}
	public void CalculatePathAndStartMovement(Vector3 goal)
	{
		isInMovement = true;
		astar.Initialize();
		astar.SetAllowDiagonal(false);
		path = astar.ComputePath(transform.position, goal, gameManager);
		// Compute path to Player
		if (path != null && info.currentEnergy > 0 && path.Count > 2) // To stop enemy from colliding into Player
		{
			info.currentEnergy--;
			// Remove first tile in path
			path.Pop();
			// Move one tile closer to Player
			Vector3Int tryDistance = path.Pop();
			if (!HasEnemyAtPosition(tryDistance))
			{
				destination = tryDistance;
			}
			else
			{
				path = null;
				isInMovement = false;
			}
			soundManager.PlaySound(enemyMove);
		}
		else // If enemy is adjacent to Player, attack
		{
			if (path != null && path.Count == 2)
			{
				for (int i = 0; i < info.currentEnergy; i++)
				{
					soundManager.PlaySound(enemyAttack);
					gameManager.HandleDamageToPlayer(info.damagePoints);
				}
			}
			path = null;
			isInMovement = false;
		}
	}
	private bool HasEnemyAtPosition(Vector3 position)
	{
		Vector3 shiftedDistance = new(position.x + 0.5f, position.y + 0.5f, 0);
		foreach (GameObject obj in gameManager.enemies)
		{
			Enemy e = obj.GetComponent<Enemy>();
			if (e.transform.position == shiftedDistance)
			{
				return true;
			}
		}
		return false;
	}
	public void RestoreEnergy()
	{
		info.currentEnergy = info.maxEnergy;
	}
	public void SetGameManager(GameManager g)
	{
		gameManager = g;
	}
	public void SetSoundManager(SoundManager s)
	{
		soundManager = s;
	}
}
