using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
	public EnemyInfo Info;
	public AudioClip Move;
	public AudioClip Attack;
	public bool IsInMovement { get; private set; } = false;
	public GameObject StunIcon;
	#region PATHFINDING
	private Tilemap TilemapGround;
	private Tilemap TilemapWalls;
	private Stack<Vector3Int> Path;
	private Vector3Int Destination;
	private AStar AStar;
	#endregion
	// Start is called before the first frame update
	protected virtual void Start()
	{
		StunIcon = Instantiate(StunIcon, transform.position, Quaternion.identity);
	}
	public void Initialize(Tilemap TilemapGround, Tilemap TilemapWalls, EnemyInfo Info)
	{
		this.TilemapGround = TilemapGround;
		this.TilemapWalls = TilemapWalls;
		this.Info = Info;
		AStar = new(this.TilemapGround, this.TilemapWalls);
	}
	/// <summary>
	/// Calculates path for Enemy to move to and handles first move of turn
	/// </summary>
	public void ComputePathAndStartMovement(Vector3 Goal)
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
		Path = AStar.ComputePath(transform.position, Goal);
		// Compute path to Player and prevent enemy from colliding into Player
		if (Path != null && Info.CurrentEnergy > 0 && Path.Count > 2)
		{
			Info.DecrementEnergy();
			IsInMovement = true;
			// Remove first tile in path
			Path.Pop();
			// Move one tile closer to Player
			Vector3Int TryDistance = Path.Pop();
			Vector3 ShiftedTryDistance = new(TryDistance.x + 0.5f, TryDistance.y + 0.5f, 0);
			if (!GameManager.Instance.HasEnemyAtPosition(ShiftedTryDistance))
			{
				Destination = TryDistance;
				MoveAlongPath();
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
				Info.DecrementEnergy();
				SoundManager.Instance.PlaySound(Attack);
				GameManager.Instance.HandleDamageToPlayer(Info.DamagePoints);
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
	private async void MoveAlongPath()
	{
		while (Path != null && Path.Count > 0)
		{
			SoundManager.Instance.PlaySound(Move);
			Vector3 ShiftedDistance = new(Destination.x + 0.5f, Destination.y + 0.5f, Destination.z);
			// Move one tile closer to Player
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(transform.position, ShiftedDistance, 2 * Time.deltaTime);
				await Task.Yield();
			}
			// Pop next tile in path
			if (Path.Count > 1 && Info.CurrentEnergy > 0)
			{
				Info.DecrementEnergy();
				Destination = Path.Pop();
			}
			// Enemy attacks Player if enemy moves to an adjacent tile
			else if (Path.Count == 1 && Info.CurrentEnergy > 0)
			{
				Info.DecrementEnergy();
				SoundManager.Instance.PlaySound(Attack);
				GameManager.Instance.HandleDamageToPlayer(Info.DamagePoints);
			}
			else
			{
				Path = null;
				IsInMovement = false;
				StunIcon.transform.position = transform.position;
			}
		}
	}
	public void DecreaseHealthBy(int damage)
	{
		// NOTE: Eventually add sprite change for enemy on this line using: spriteRenderer.sprite = damagedSprite;
		Info.DecreaseHealthBy(damage);
		if (Info.CurrentHealth <= 0)
		{
			gameObject.SetActive(false);
		}
	}
	public void RestoreEnergy()
	{
		Info.RestoreEnergy();
	}
}