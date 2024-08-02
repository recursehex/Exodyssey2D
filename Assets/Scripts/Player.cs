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
	public Profession Profession = new(Profession.Tags.Unknown, 0);
	public AudioClip PlayerMove;
	public AudioClip Heal;
	public AudioClip Select;
	public AudioClip GameOver;
	public Animator Animator;
	public Inventory Inventory;
	public InventoryUI InventoryUI;
	public ItemInfo SelectedItem = null;
	public StatsDisplayManager StatsDisplayManager;
	public bool finishedInit = false;
	public bool isInMovement = false;
	public GameManager GameManager;
	public SoundManager SoundManager;
	#region PATHFINDING
	public Tilemap TilemapGround;
	public Tilemap TilemapWalls;
	private Stack<Vector3Int> Path;
	private Vector3Int Destination;
	[SerializeField]
	private AStar AStar;
	#endregion
	// Start is called before the first frame update
	protected virtual void Start()
	{
		AStar = new(TilemapGround, TilemapWalls);
		Inventory = new();
		Animator = GetComponent<Animator>();
		InventoryUI.SetInventory(Inventory);
		finishedInit = true;
	}
	// Update is called once per frame 
	void Update()
	{
		if (!GameManager.playersTurn)
		{
			return;
		}
		MoveAlongThePath();
	}
	/// <summary>
	/// Calculates path for Player to travel to destination for point clicked on
	/// </summary>
	public void CalculatePathAndStartMovement(Vector3 Goal)
	{
		AStar.Initialize();
		Path = AStar.ComputePath(transform.position, Goal, GameManager);
		if (Path == null)
		{
			return;
		}
		DecreaseEnergy(Path.Count - 1);
		Path.Pop();
		Destination = Path.Pop();
		SoundManager.PlaySound(PlayerMove);
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
	/// Moves Player along A* path
	/// </summary>
	public void MoveAlongThePath()
	{
		if (Path == null)
		{
			return;
		}
		isInMovement = true;
		Vector3 ShiftedDistance = new(Destination.x + 0.5f, Destination.y + 0.5f, Destination.z);
		transform.position = Vector3.MoveTowards(transform.position, ShiftedDistance, 2 * Time.deltaTime);
		float distance = Vector3.Distance(ShiftedDistance, transform.position);
		if (distance > 0f)
		{
			return;
		}
		if (Path.Count > 0)
		{
			Destination = Path.Pop();
			SoundManager.PlaySound(PlayerMove);
		}
		else // When Player stops moving
		{
			Path = null;
			isInMovement = false;
			if (GameManager.PlayerIsOnExitTile())
			{
				return;
			}
			GameManager.DrawTargetsAndTracers();
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
			SoundManager.PlaySound(GameOver);
			SoundManager.MusicSource.Stop();
			GameManager.GameOver();
		}
		// Player is damaged
		StatsDisplayManager.DecreaseHealthDisplay(CurrentHealth, MaxHealth);
		Animator.SetTrigger("playerHit");
		// Reduce max energy to simulate weakness
		if (CurrentHealth == 1)
		{
			CurrentEnergy = 1;
			StatsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, MaxEnergy);
			MaxEnergy = 1;
		}
	}
	/// <summary>
	/// Restores Player Health to MaxHealth
	/// </summary>
	public void RestoreHealth() 
	{
		CurrentHealth = MaxHealth;
		StatsDisplayManager.RestoreHealthDisplay();
		SoundManager.PlaySound(Heal);
		MaxEnergy = 3;
	}
	/// <summary>
	/// Decreases CurrentEnergy and updates energy display
	/// </summary>
	public void DecreaseEnergy(int decrement)
	{
		CurrentEnergy = Mathf.Clamp(CurrentEnergy - decrement, 0, MaxEnergy);
		StatsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, MaxEnergy);
		// End turn and stop timer if CurrentEnergy reaches 0
		if (CurrentEnergy == 0)
		{
			GameManager.TurnTimer.timeRemaining = 0;
		}
	}
	/// <summary>
	/// Restores Player Energy to MaxEnergy
	/// </summary>
	public void RestoreEnergy() 
	{
		CurrentEnergy = MaxEnergy;
		StatsDisplayManager.RestoreEnergyDisplay(CurrentHealth);
	}
	/// <summary>
	/// Decreases weapon durability by 1, removes weapon if uses == 0
	/// </summary>
	public void DecreaseWeaponDurability()
	{
		SelectedItem.DecreaseDurability();
		InventoryUI.SetCurrentSelected(InventoryUI.SelectedIndex);
		// When weapon durability reaches 0
		if (SelectedItem.currentUses == 0)
		{
			InventoryUI.RemoveItem(InventoryUI.SelectedIndex);
			InventoryUI.SetCurrentSelected(-1);
			GameManager.ClearTargetsAndTracers();
			SelectedItem = null;
			DamagePoints = 0;
		}
	}
	public int GetWeaponRange()
	{
		return SelectedItem == null ? 0 : SelectedItem.range;
	}
	/// <summary>
	/// Adds item to inventory when picked up
	/// </summary>
	public bool AddItem(ItemInfo ItemInfo)
	{
		bool itemIsAdded = Inventory.AddItem(new(ItemInfo));
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
		if (itemIndex >= Inventory.InventoryList.Count
			|| Inventory.InventoryList.Count == 0)
		{
			return;
		}
		ItemInfo ClickedItem = Inventory.InventoryList[itemIndex].ItemInfo;
		bool wasSelected = InventoryUI.ProcessSelection(InventoryUI.SelectedIndex, itemIndex);
		// If item is selected, update selection, otherwise reset
		if (wasSelected)
		{
			InventoryUI.SetCurrentSelected(itemIndex);
			SelectedItem = ClickedItem;
			SoundManager.PlaySound(Select);
			DamagePoints = ClickedItem.damagePoints;

			// Only draw targets if ranged weapon is selected
			if (ClickedItem.range > 0)
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
			SelectedItem = null;
			DamagePoints = 0;
			GameManager.ClearTargetsAndTracers();
		}
	}
	/// <summary>
	/// Handles when ClickTarget() clicks on Player
	/// </summary>
	public bool ClickOnPlayerToUseItem()
	{
		if (SelectedItem?.Type is not ItemInfo.Types.Consumable)
		{
			return false;
		}
		bool wasUsed = MedKitWasUsed();
		if (SelectedItem.currentUses == 0)
		{
			InventoryUI.RemoveItem(InventoryUI.SelectedIndex);
			SelectedItem = null;
			wasUsed = true;
		}
		return wasUsed;
	}
	private bool MedKitWasUsed() 
	{
		if (SelectedItem.Tag is ItemInfo.Tags.MedKit
			&& CurrentHealth < MaxHealth
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
				SelectedItem.DecreaseDurability();
			}
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
		if (Inventory.InventoryList.Count == 0)
		{
			return;
		}
		// Drops item on the ground, returns if an item is occupying the tile
		if (!GameManager.DropItem(Inventory.InventoryList[itemIndex].ItemInfo))
		{
			return;
		}
		// Resets damage points if weapon is dropped
		if (SelectedItem.Type is ItemInfo.Types.Weapon)
		{
			DamagePoints = 0;
			// Clears targeting if ranged weapon is dropped
			if (GetWeaponRange() > 0)
			{
				GameManager.ClearTargetsAndTracers();
			}
		}
		// Removes item from inventory and plays corresponding sound
		SelectedItem = null;
		InventoryUI.RemoveItem(itemIndex);
		SoundManager.PlaySound(PlayerMove);
	}
	public void ProcessHoverForInventory(Vector3 MousePosition)
	{
		InventoryUI.ProcessHoverForInventory(MousePosition);
	}
	public void SetAnimation(string trigger)
	{
		Animator.SetTrigger(trigger);
	}
}
