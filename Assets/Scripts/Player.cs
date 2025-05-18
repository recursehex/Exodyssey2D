using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Contains functionality specific to the Player
/// </summary>
public class Player : MonoBehaviour
{
	#region DATA
	private readonly int maxHealth = 3;
	private int maxEnergy = 3;
	private int currentHealth = 3;
	public int currentEnergy = 3;
	public int DamagePoints { get; private set; } = 0;
	private readonly int walkSpeed = 2;
	public Vehicle Vehicle;
	public bool IsInVehicle => Vehicle != null;
	private bool hasHelmet = false;
	private int helmetHealth;
	private bool hasVest = false;
	private int vestHealth;
	private bool hasNightVision = false;
	public Profession Job;
	#endregion
	#region AUDIO
	public AudioClip PlayerMove;
	public AudioClip Heal;
	public AudioClip Select;
	public AudioClip Attack;
	public AudioClip GameOver;
	private Animator Animator;
	#endregion
	#region INVENTORY
	public Inventory Inventory;
	public InventoryUI InventoryUI;
	public ItemInfo SelectedItemInfo = null;
	public StatsDisplayManager StatsDisplayManager;
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
	public bool FinishedInit { get; private set; } = false;
	protected virtual void Start()
	{
		AStar = new(TilemapGround, TilemapWalls);
		Inventory = new(2);
		InventoryUI.Inventory = Inventory;
		Animator = GetComponent<Animator>();
		Job = Profession.GetRandomProfession();
		FinishedInit = true;
	}
	#region MOVEMENT METHODS
	/// <summary>
	/// Calculates path for Player to travel to destination for point clicked on
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
	/// Moves Player along A* path
	/// </summary>
	private IEnumerator MoveAlongPath()
	{
		// While path has remaining tiles
		while (Path != null && Path.Count >= 0)
		{
			DecrementEnergy();
			SoundManager.Instance.PlaySound(PlayerMove);
			Vector3 ShiftedDistance = Destination + new Vector3(0.5f, 0.5f, 0);
			// Move Player smoothly to next tile
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(
					transform.position, 
					ShiftedDistance, 
					walkSpeed * Time.deltaTime);
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
		// When Player stops moving
		Path = null;
		IsInMovement = false;
		if (!GameManager.Instance.PlayerIsOnExitTile())
		{
			GameManager.Instance.DrawTargetsAndTracers();
		}
	}
	/// <summary>
	/// Calculates area Player can move to in a turn based on currentEnergy
	/// </summary>
	public Dictionary<Vector3Int, Node> CalculateArea()
	{
		AStar.Initialize();
		return AStar.GetReachableAreaByDistance(transform.position, currentEnergy);
	}
	public void EnterVehicle(Vehicle EnteredVehicle)
    {
		Vehicle = EnteredVehicle;
		transform.position = Vehicle.transform.position;
		DecrementEnergy();
    }
	#endregion
	#region HEALTH METHODS
	/// <summary>
	/// Decreases currentHealth by damage and updates Health display
	/// </summary>
	public void DecreaseHealthBy(int damage, bool isMeleeDamage)
	{
		// Handle armor
		if (isMeleeDamage && hasHelmet)
		{
			int absorbed = Mathf.Min(damage, helmetHealth);
			helmetHealth -= absorbed;
			damage       -= absorbed;
			if (helmetHealth <= 0) hasHelmet = false;
		}
		else if (!isMeleeDamage && hasVest)
		{
			int absorbed = Mathf.Min(damage, vestHealth);
			vestHealth -= absorbed;
			damage     -= absorbed;
			if (vestHealth <= 0) hasVest = false;
		}
		if (damage <= 0) return;
		// Damage Player
		currentHealth -= damage;
		// If Player is killed
		if (currentHealth <= 0)
		{
			SoundManager.Instance.PlaySound(GameOver);
			GameManager.Instance.GameOver();
			SoundManager.Instance.FadeOutMusic(2.0f);
			return;
		}
		// Update health display and animation state
		StatsDisplayManager.DecreaseHealthDisplay(currentHealth, maxHealth);
		Animator.SetTrigger("playerHit");
		// Reduce max energy to simulate weakness at 1 health
		if (currentHealth == 1)
		{
			currentEnergy = 1;
			StatsDisplayManager.DecreaseEnergyDisplay(currentEnergy, maxEnergy);
			maxEnergy = 1;
		}
	}
	/// <summary>
	/// Restores Player Health to maxHealth
	/// </summary>
	private void RestoreHealth() 
	{
		currentHealth = maxHealth;
		StatsDisplayManager.RestoreHealthDisplay();
		SoundManager.Instance.PlaySound(Heal);
		maxEnergy = 3;
	}
	#endregion
	#region ENERGY METHODS
	/// <summary>
	/// Decreases CurrentEnergy by 1 and updates energy display
	/// </summary>
	private void DecrementEnergy()
	{
		currentEnergy = Mathf.Clamp(--currentEnergy, 0, maxEnergy);
		StatsDisplayManager.DecreaseEnergyDisplay(currentEnergy, maxEnergy);
		// End turn and stop timer if CurrentEnergy reaches 0
		if (currentEnergy == 0)
		{
			GameManager.Instance.StopTurnTimer();
		}
	}
	/// <summary>
	/// Sets Player CurrentEnergy to 0
	/// </summary>
	public void SetEnergyToZero()
	{
		currentEnergy = 0;
		StatsDisplayManager.DecreaseEnergyDisplay(currentEnergy, maxEnergy);
	}
	/// <summary>
	/// Restores Player Energy to maxEnergy
	/// </summary>
	public void RestoreEnergy() 
	{
		currentEnergy = maxEnergy;
		StatsDisplayManager.RestoreEnergyDisplay(currentHealth);
	}
	public bool HasEnergy()
	{
		return currentEnergy > 0;
	}
	#endregion
	#region ATTACK METHODS
	/// <summary>
	/// Handles Player variables when attacking an enemy
	/// </summary>
	public void AttackEnemy()
	{
		SoundManager.Instance.PlaySound(Attack);
		Animator.SetTrigger("playerAttack");
		DecrementEnergy();
		DecrementWeaponDurability();
	}
	/// <summary>
	/// Decreases weapon durability by 1, removes weapon if uses == 0
	/// </summary>
	public void DecrementWeaponDurability()
	{
		SelectedItemInfo.DecreaseDurability();
		InventoryUI.SetCurrentSelected(InventoryUI.SelectedIndex);
		// When weapon durability reaches 0
		if (SelectedItemInfo.CurrentUses == 0)
		{
			InventoryUI.RemoveItem(InventoryUI.SelectedIndex);
			InventoryUI.SetCurrentSelected(-1);
			GameManager.Instance.ClearTargetsAndTracers();
			SelectedItemInfo = null;
			DamagePoints = 0;
		}
	}
	public int GetWeaponRange()
	{
		return SelectedItemInfo == null ? 0 : SelectedItemInfo.Range;
	}
	#endregion
	#region ITEM METHODS
	/// <summary>
	/// Adds item to inventory when picked up, returns false if inventory is full
	/// </summary>
	public bool TryAddItem(Item Item)
	{
		if (Inventory.TryAddItem(Item))
		{
			InventoryUI.RefreshInventoryIcons();
			return true;
		}
		return false;
	}
	/// <summary>
	/// Clicks on item in inventory, called by InventoryIcons
	/// </summary>
	public void TryClickItem(int itemIndex)
	{
		// Ensures index is within bounds and inventory has an item
		if (itemIndex >= Inventory.Count
			|| Inventory.Count == 0)
		{
			return;
		}
		ItemInfo ClickedItem = Inventory[itemIndex].Info;
		// If item is selected, update selection, otherwise reset
		if (InventoryUI.ProcessSelection(InventoryUI.SelectedIndex, itemIndex))
		{
			InventoryUI.SetCurrentSelected(itemIndex);
			SelectedItemInfo = ClickedItem;
			SoundManager.Instance.PlaySound(Select);
			DamagePoints = ClickedItem.DamagePoints;
			// Only draw targets if ranged weapon is selected
			if (ClickedItem.Range > 0)
			{
				GameManager.Instance.DrawTargetsAndTracers();
			}
			// Clear targets for all other items
			else
			{
				GameManager.Instance.ClearTargetsAndTracers();
			}
		}
		// Item was deselected
		else
		{
			InventoryUI.SetCurrentSelected(-1);
			SelectedItemInfo = null;
			DamagePoints = 0;
			GameManager.Instance.ClearTargetsAndTracers();
		}
	}
	/// <summary>
	/// Handles when ClickTarget() clicks on Player, returns false if item is not valid or was not used
	/// </summary>
	public bool ClickOnPlayerToUseItem()
	{
		if (SelectedItemInfo == null)
		{
			return false;
		}
		bool wasUsed = WasItemUsed();
		if (SelectedItemInfo.CurrentUses == 0)
		{
			InventoryUI.RemoveItem(InventoryUI.SelectedIndex);
			SelectedItemInfo = null;
			wasUsed = true;
		}
		return wasUsed;
	}
	/// <summary>
	/// Returns true if item was used on Player
	/// </summary>
	private bool WasItemUsed()
	{
		if (SelectedItemInfo.Tag is ItemInfo.Tags.MedKit
			&& currentHealth < maxHealth
			&& currentEnergy > 0)
		{
			RestoreHealth();
			// Uses energy if profession is not medic
			if (Job.Tag is not Profession.Tags.Medic)
			{
				DecrementEnergy();
			}
			// Uses MedKit if profession is not master medic
			if (!(Job.IsMaster && Job.Tag is Profession.Tags.Medic))
			{
				SelectedItemInfo.DecreaseDurability();
			}
			return true;
		}
		// else if (SelectedItemInfo.Tag is ItemInfo.Tags.Helmet
		// 	&& !hasHelmet)
		// {
		// 	hasHelmet = true;
		// 	helmetHealth = SelectedItemInfo.CurrentUses;
		// 	SelectedItemInfo = null;
		// 	return true;
		// }
		// else if (SelectedItemInfo.Tag is ItemInfo.Tags.Vest
		// 	&& !hasVest)
		// {
		// 	hasVest = true;
		// 	vestHealth = SelectedItemInfo.CurrentUses;
		// 	SelectedItemInfo = null;
		// 	return true;
		// }
		// else if (SelectedItemInfo.Tag is ItemInfo.Tags.NightVision
		// 	&& !hasNightVision)
		// {
		// 	hasNightVision = true;
		// 	SelectedItemInfo = null;
		// 	return true;
		// }
		return false;
	}
	/// <summary>
	/// Tries to drop item from inventory onto the ground, called by InventoryIcons
	/// </summary>
	public void TryDropItem(int itemIndex)
	{
		// Returns if called when inventory is empty
		if (Inventory.Count == 0)
		{
			return;
		}
		// Resets damage points if weapon is dropped
		if (SelectedItemInfo == Inventory[itemIndex].Info
			&& Inventory[itemIndex].Info?.Type is ItemInfo.Types.Weapon)
		{
			DamagePoints = 0;
			// Clears targeting if ranged weapon is dropped
			if (GetWeaponRange() > 0)
			{
				GameManager.Instance.ClearTargetsAndTracers();
			}
		}
		// Put dropped item in temp slot out of inventory
		ItemInfo DroppedItemInfo = Inventory[itemIndex].Info;
		if (DroppedItemInfo == SelectedItemInfo)
		{
			SelectedItemInfo = null;
			InventoryUI.DeselectItem(itemIndex);
		}
		Item ItemAtPosition = GameManager.Instance.GetItemAtPosition(transform.position);
		// If there is item at Player's position
		if (ItemAtPosition != null)
		{
			// Swap dropped item with ground item
			Inventory[itemIndex].Info = ItemAtPosition.Info;
			GameManager.Instance.RemoveItemAtPosition(ItemAtPosition);
			Destroy(ItemAtPosition.gameObject);
			InventoryUI.RefreshInventoryIcons();
		}
		// Remove dropped item from inventory
		else
		{
			InventoryUI.RemoveItem(itemIndex);
		}
		InventoryUI.RefreshText();
		// Drop item onto ground from temp slot
		GameManager.Instance.SpawnItem((int)DroppedItemInfo.Tag, transform.position);
		// Removes item from inventory and plays corresponding sound
		SoundManager.Instance.PlaySound(PlayerMove);
	}
    #endregion
}