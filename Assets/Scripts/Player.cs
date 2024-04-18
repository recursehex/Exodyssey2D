using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Contains functionality specific to the Player
/// </summary>
public class Player : MonoBehaviour
{
	public int MaxHealth { get; private set; } = 3;
	public int MaxEnergy { get; private set; } = 3;
	public int CurrentHealth { get; set; } = 3;
	public int CurrentEnergy { get; set; } = 3;
	public int DamagePoints { get; set; } = 0;
	public Profession profession;
	public AudioClip playerMove;
	public AudioClip heal;
	public AudioClip select;
	public AudioClip gameOver;
	public Animator animator;
	public Inventory inventory;
	public InventoryUI inventoryUI;
	public ItemInfo selectedItem = null;
	public StatsDisplayManager statsDisplayManager;
	public bool finishedInit = false;
	public bool isInMovement = false;
	GameManager gm;
	#region PATHFINDING
	public Tilemap tilemapGround;
	public Tilemap tilemapWalls;
	private Stack<Vector3Int> path;
	private Vector3Int destination;
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
		inventory = new Inventory();
		animator = GetComponent<Animator>();
		inventoryUI.SetInventory(inventory);
		finishedInit = true;
	}
	// Update is called once per frame 
	void Update()
	{
		MoveAlongThePath();
	}
	/// <summary>
	/// Calculates path for Player to travel to destination for point clicked on
	/// </summary>
	public void CalculatePathAndStartMovement(Vector3 goal)
	{
		astar.Initialize();
		path = astar.ComputePath(transform.position, goal, gm);
		if (path == null) return;
		ChangeEnergy(-(path.Count - 1));
		path.Pop();
		destination = path.Pop();
		SoundManager.instance.PlaySound(playerMove);
	}
	/// <summary>
	/// Calculates area Player can move to in a turn based on currentEnergy
	/// </summary>
	public Dictionary<Vector3Int, Node> CalculateArea()
	{
		astar.Initialize();
		return astar.GetReachableAreaByDistance(transform.position, CurrentEnergy);
	}
	/// <summary>
	/// Moves Player along A* path
	/// </summary>
	public void MoveAlongThePath()
	{
		if (path == null) return;
		isInMovement = true;
		Vector3 shiftedDistance = new(destination.x + 0.5f, destination.y + 0.5f, destination.z);
		transform.position = Vector3.MoveTowards(transform.position, shiftedDistance, 2 * Time.deltaTime);
		float distance = Vector3.Distance(shiftedDistance, transform.position);
		if (distance > 0f) return;
		if (path.Count > 0)
		{
			destination = path.Pop();
			SoundManager.instance.PlaySound(playerMove);
		}
		else // When Player stops moving
		{
			path = null;
			isInMovement = false;
			if (gm.PlayerIsOnExitTile()) return;
			gm.DrawTargetsAndTracers();
		}
	}
	/// <summary>
	/// Decreases CurrentHealth by damage and updates Health display
	/// </summary>
	public void DecreaseHealth(int damage)
	{
		CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaxHealth);
		// Player is killed
		if (CurrentHealth == 0)
		{
			SoundManager.instance.PlaySound(gameOver);
			SoundManager.instance.musicSource.Stop();
			gm.GameOver();
		}
		// Player is damaged
		if (damage > 0)
		{
			statsDisplayManager.DecreaseHealthDisplay(CurrentHealth, MaxHealth);
			animator.SetTrigger("playerHit");
			// Reduce max energy to simulate weakness
			if (CurrentHealth == 1)
			{
				CurrentEnergy = 1;
				statsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, MaxEnergy);
				MaxEnergy = 1;
			}
		}
	}
	/// <summary>
	/// Restores Player Health to MaxHealth
	/// </summary>
	public void RestoreHealth() 
	{
		CurrentHealth = Mathf.Clamp(CurrentHealth + 3, 0, MaxHealth);
		statsDisplayManager.RestoreHealthDisplay();
		SoundManager.instance.PlaySound(heal);
		MaxEnergy = 3;
	}
	/// <summary>
	/// Changes energy and updates energy display, use negative to decrease
	/// </summary>
	public void ChangeEnergy(int change)
	{
		CurrentEnergy = Mathf.Clamp(CurrentEnergy + change, 0, MaxEnergy);
		// End turn and stop timer
		if (CurrentEnergy == 0) gm.turnTimer.timeRemaining = 0;
		// Decreased by Player action
		if (change < 0) statsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, MaxEnergy);
		// Restore after end turn and new level
		else statsDisplayManager.RestoreEnergyDisplay(CurrentHealth);
	}
	/// <summary>
	/// Decreases weapon durability by 1, removes weapon if uses == 0
	/// </summary>
	public void DecreaseWeaponDurability()
	{
		selectedItem.DecreaseDurability();
		inventoryUI.SetCurrentSelected(inventoryUI.SelectedIndex);
		if (selectedItem.currentUses == 0)
		{
			inventoryUI.RemoveItem(inventoryUI.SelectedIndex);
			inventoryUI.SetCurrentSelected(-1);
			gm.ClearTargetsAndTracers();
			selectedItem = null;
			DamagePoints = 0;
		}
	}
	public int GetWeaponRange()
	{
		return selectedItem == null ? -1 : selectedItem.range;
	}
	/// <summary>
	/// Adds item to inventory when picked up
	/// </summary>
	public bool AddItem(ItemInfo itemInfo)
	{
		bool itemIsAdded = inventory.AddItem(new ItemInventory { itemInfo = itemInfo });
		if (itemIsAdded) inventoryUI.RefreshInventoryIcons();
		return itemIsAdded;
	}
	/// <summary>
	/// Clicks on item in inventory, called by InventoryIcons
	/// </summary>
	public void TryClickItem(int itemIndex)
	{
		// Ensures index is within bounds and inventory has an item
		if (itemIndex >= inventory.itemList.Count || inventory.itemList.Count == 0) return;
	
		ItemInfo clickedItem = inventory.itemList[itemIndex].itemInfo;
		bool wasSelected = InventoryUI.ProcessSelection(inventoryUI.SelectedIndex, itemIndex);
		
		// If item is selected, update selection, otherwise reset
		if (wasSelected)
		{
			inventoryUI.SetCurrentSelected(itemIndex);
			selectedItem = clickedItem;
			SoundManager.instance.PlaySound(select);
			DamagePoints = clickedItem.damagePoints;

			// Only draw targets if ranged weapon is selected
			if (clickedItem.range > 0)
			{
				gm.DrawTargetsAndTracers();
			}
			else	// Clear targets for all other items
			{
				gm.ClearTargetsAndTracers();
			}
		}
		else	// Item was unselected
		{
			inventoryUI.SetCurrentSelected(-1);
			selectedItem = null;
			DamagePoints = 0;
			gm.ClearTargetsAndTracers();
		}
	}
	/// <summary>
	/// Handles when ClickTarget() clicks on Player
	/// </summary>
	public bool ClickOnPlayerToUseItem()
	{
		if (selectedItem?.type is not ItemInfo.ItemType.Consumable) return false;
		bool wasUsed = MedKitWasUsed();
		if (selectedItem.currentUses == 0)
		{
			inventoryUI.RemoveItem(inventoryUI.SelectedIndex);
			selectedItem = null;
			wasUsed = true;
		}
		return wasUsed;
	}
	private bool MedKitWasUsed() 
	{
		if (selectedItem.tag == ItemInfo.ItemTag.MedKit && CurrentHealth < MaxHealth && CurrentEnergy > 0)
		{
			RestoreHealth();
			ChangeEnergy(-1);
			selectedItem.DecreaseDurability();
			return true;
		}
		return false;
	}
	/// <summary>
	/// Tries to drop item from inventory onto the ground
	/// </summary>
	public void TryDropItem(int itemIndex)
	{
		// Returns if called when inventory is empty
		if (inventory.itemList.Count == 0) return;
		// Drops item on the ground, returns if an item is occupying the tile
		if (!GameManager.MyInstance.DropItem(inventory.itemList[itemIndex].itemInfo)) return;
		// Resets damage points if weapon is dropped
		if (selectedItem.type == ItemInfo.ItemType.Weapon)
		{
			DamagePoints = 0;
			// Clears targeting if ranged weapon is dropped
			if (GetWeaponRange() > 0) gm.ClearTargetsAndTracers();
		}
		// Removes item from inventory and plays corresponding sound
		selectedItem = null;
		inventoryUI.RemoveItem(itemIndex);
		SoundManager.instance.PlaySound(playerMove);
	}
	public void ProcessHoverForInventory(Vector3 mousePosition)
	{
		inventoryUI.ProcessHoverForInventory(mousePosition);
	}
	public void SetAnimation(string trigger)
	{
		animator.SetTrigger(trigger);
	}
	public void SetGameManager(GameManager g)
	{
		gm = g;
	}
}