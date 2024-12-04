using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Contains functionality specific to the Player
/// </summary>
public class Player : MonoBehaviour
{
	private readonly int maxHealth = 3;
	private int maxEnergy = 3;
	private int currentHealth = 3;
	public int CurrentEnergy { get; private set; } = 3;
	public int DamagePoints { get; private set; } = 0;
	public Profession Profession = new(Profession.Tags.Unknown, 0);
	public AudioClip PlayerMove;
	public AudioClip Heal;
	public AudioClip Select;
	public AudioClip Attack;
	public AudioClip GameOver;
	private Animator Animator;
	public Inventory Inventory;
	public InventoryUI InventoryUI;
	public ItemInfo SelectedItemInfo = null;
	public StatsDisplayManager StatsDisplayManager;
	public bool FinishedInit { get; private set; } = false;
	public bool IsInMovement { get; set; } = false;
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
		AStar = new(TilemapGround, TilemapWalls);
		Inventory = new();
		InventoryUI.Inventory = Inventory;
		Animator = GetComponent<Animator>();
		FinishedInit = true;
	}
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
		DecreaseEnergy(Path.Count - 1);
		Path.Pop();
		Destination = Path.Pop();
		IsInMovement = true;
		MoveAlongPath();
	}
	/// <summary>
	/// Moves Player along A* path
	/// </summary>
	private async void MoveAlongPath()
	{
		while (Path != null && Path.Count >= 0)
		{
			SoundManager.PlaySound(PlayerMove);
			Vector3 ShiftedDistance = new(Destination.x + 0.5f, Destination.y + 0.5f, Destination.z);
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(transform.position, ShiftedDistance, 2 * Time.deltaTime);
				await Task.Yield();
			}
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
		if (GameManager.PlayerIsOnExitTile())
		{
			return;
		}
		GameManager.DrawTargetsAndTracers();
	}
	/// <summary>
	/// Calculates area Player can move to in a turn based on currentEnergy
	/// </summary>
	public Dictionary<Vector3Int, Node> CalculateArea()
	{
		AStar.Initialize();
		return AStar.GetReachableAreaByDistance(transform.position, CurrentEnergy);
	}
	/// <summary>
	/// Decreases currentHealth by damage and updates Health display
	/// </summary>
	public void DecreaseHealth(int damage)
	{
		currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
		// Player is killed
		if (currentHealth == 0)
		{
			SoundManager.PlaySound(GameOver);
			GameManager.GameOver();
			SoundManager.Instance.FadeOutMusic(2.0f);
			return;
		}
		// Player is damaged
		StatsDisplayManager.DecreaseHealthDisplay(currentHealth, maxHealth);
		Animator.SetTrigger("playerHit");
		// Reduce max energy to simulate weakness
		if (currentHealth == 1)
		{
			CurrentEnergy = 1;
			StatsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, maxEnergy);
			maxEnergy = 1;
		}
	}
	/// <summary>
	/// Restores Player Health to maxHealth
	/// </summary>
	public void RestoreHealth() 
	{
		currentHealth = maxHealth;
		StatsDisplayManager.RestoreHealthDisplay();
		SoundManager.PlaySound(Heal);
		maxEnergy = 3;
	}
	/// <summary>
	/// Decreases CurrentEnergy and updates energy display
	/// </summary>
	public void DecreaseEnergy(int decrement)
	{
		CurrentEnergy = Mathf.Clamp(CurrentEnergy - decrement, 0, maxEnergy);
		StatsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, maxEnergy);
		// End turn and stop timer if CurrentEnergy reaches 0
		if (CurrentEnergy == 0)
		{
			GameManager.EndTurnTimer();
		}
	}
	/// <summary>
	/// Decreases Player CurrentEnergy to 0
	/// </summary>
	public void DecreaseEnergyToZero()
	{
		CurrentEnergy = 0;
	}
	/// <summary>
	/// Restores Player Energy to maxEnergy
	/// </summary>
	public void RestoreEnergy() 
	{
		CurrentEnergy = maxEnergy;
		StatsDisplayManager.RestoreEnergyDisplay(currentHealth);
	}
	/// <summary>
	/// Handles Player variables when attacking an enemy
	/// </summary>
	public void AttackEnemy()
	{
		SoundManager.PlaySound(Attack);
		DecreaseEnergy(1);
		DecreaseWeaponDurability();
		Animator.SetTrigger("playerAttack");
	}
	/// <summary>
	/// Decreases weapon durability by 1, removes weapon if uses == 0
	/// </summary>
	public void DecreaseWeaponDurability()
	{
		SelectedItemInfo.DecreaseDurability();
		InventoryUI.SetCurrentSelected(InventoryUI.SelectedIndex);
		// When weapon durability reaches 0
		if (SelectedItemInfo.CurrentUses == 0)
		{
			InventoryUI.RemoveItem(InventoryUI.SelectedIndex);
			InventoryUI.SetCurrentSelected(-1);
			GameManager.ClearTargetsAndTracers();
			SelectedItemInfo = null;
			DamagePoints = 0;
		}
	}
	public int GetWeaponRange()
	{
		return SelectedItemInfo == null ? 0 : SelectedItemInfo.Range;
	}
	/// <summary>
	/// Adds item to inventory when picked up, returns false if inventory is full
	/// </summary>
	public bool TryAddItem(ItemInfo ItemInfo)
	{
		bool itemIsAdded = Inventory.TryAddItem(new(ItemInfo));
		if (itemIsAdded)
		{
			InventoryUI.RefreshInventoryIcons();
		}
		return itemIsAdded;
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
		bool wasSelected = InventoryUI.ProcessSelection(InventoryUI.SelectedIndex, itemIndex);
		// If item is selected, update selection, otherwise reset
		if (wasSelected)
		{
			InventoryUI.SetCurrentSelected(itemIndex);
			SelectedItemInfo = ClickedItem;
			SoundManager.PlaySound(Select);
			DamagePoints = ClickedItem.DamagePoints;
			// Only draw targets if ranged weapon is selected
			if (ClickedItem.Range > 0)
			{
				GameManager.DrawTargetsAndTracers();
			}
			// Clear targets for all other items
			else
			{
				GameManager.ClearTargetsAndTracers();
			}
		}
		// Item was deselected
		else
		{
			InventoryUI.SetCurrentSelected(-1);
			SelectedItemInfo = null;
			DamagePoints = 0;
			GameManager.ClearTargetsAndTracers();
		}
	}
	/// <summary>
	/// Handles when ClickTarget() clicks on Player, returns false if item is not consumable or was not used
	/// </summary>
	public bool ClickOnPlayerToUseItem()
	{
		if (SelectedItemInfo?.Type is not ItemInfo.Types.Consumable)
		{
			return false;
		}
		bool wasUsed = MedKitWasUsed();
		if (SelectedItemInfo.CurrentUses == 0)
		{
			InventoryUI.RemoveItem(InventoryUI.SelectedIndex);
			SelectedItemInfo = null;
			wasUsed = true;
		}
		return wasUsed;
	}
	/// <summary>
	/// Returns true if MedKit was used to heal Player
	/// </summary>
	private bool MedKitWasUsed() 
	{
		if (SelectedItemInfo.Tag is ItemInfo.Tags.MedKit
			&& currentHealth < maxHealth
			&& CurrentEnergy > 0)
		{
			RestoreHealth();
			// Uses energy if profession is not medic
			if (Profession.Tag is not Profession.Tags.Medic)
			{
				DecreaseEnergy(1);
			}
			// Uses MedKit if profession is not medic level 2
			if (!(Profession.Level >= 2 && Profession.Tag is Profession.Tags.Medic))
			{
				SelectedItemInfo.DecreaseDurability();
			}
			return true;
		}
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
				GameManager.ClearTargetsAndTracers();
			}
		}
		// Put dropped item in temp slot out of inventory
		ItemInfo DroppedItemInfo = Inventory[itemIndex].Info;
		if (DroppedItemInfo == SelectedItemInfo)
		{
			SelectedItemInfo = null;
			InventoryUI.DeselectItem(itemIndex);
		}
		Item ItemAtPosition = GameManager.GetItemAtPosition(transform.position);
		// If there is item at Player's position
		if (ItemAtPosition != null)
		{
			// Swap dropped item with ground item
			Inventory[itemIndex].Info = ItemAtPosition.Info;
			GameManager.RemoveItemAtPosition(ItemAtPosition);
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
		GameManager.InstantiateNewItem(DroppedItemInfo, transform.position);
		// Removes item from inventory and plays corresponding sound
		SoundManager.PlaySound(PlayerMove);
	}
}