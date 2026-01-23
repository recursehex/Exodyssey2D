using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Animator))]

/// <summary>
/// Contains functionality specific to the Player
/// </summary>
public class Player : MonoBehaviour
{
	[Header("Player Stats")]
	#region DATA
	private static readonly int fixedMaxEnergy = 3; // To restore energy to original value
	[SerializeField] private int maxHealth 	   	= 3;
	[SerializeField] private int maxEnergy	   	= fixedMaxEnergy;
	[SerializeField] private int currentHealth	= 3;
	public int CurrentEnergy { get; private set; } = 3;
	private readonly int walkSpeed 		= 2;
	private readonly int inventorySize 	= 2;
	public int DamagePoints => SelectedItemInfo?.DamagePoints ?? 0;
	[Header("Vehicle")]
	public Vehicle Vehicle;
	public bool IsInVehicle => Vehicle != null;
	[Header("Armor & Equipment")]
	private bool hasHelmet = false;
	private int helmetHealth = 0;
	private bool hasVest = false;
	private int vestHealth = 0;
	private bool hasNightVision = false;
	public Profession Profession;
	#endregion
	#region EVENTS
	public Action OnMovementComplete;
	#endregion
	#region AUDIO
	[SerializeField] private AudioClip Move;
	[SerializeField] private AudioClip Heal;
	[SerializeField] private AudioClip Select;
	[SerializeField] private AudioClip Attack;
	[SerializeField] private AudioClip Hurt;
	[SerializeField] private AudioClip GameOver;
	private Animator Animator;
	#endregion
	#region INVENTORY
	private Inventory Inventory;
	public InventoryUI InventoryUI;
	public ItemInfo SelectedItemInfo = null;
	public StatsDisplayManager StatsDisplayManager;
	#endregion
	#region DEBUG
	[Header("Debug")]
	[SerializeField] private ItemInfo.Tags SelectedItemTag = ItemInfo.Tags.Unknown;
	[SerializeField] private int selectedItemUses = 0;
	[SerializeField] private bool isInVehicle = false;
	[SerializeField] private List<ItemInfo.Tags> InventoryItemTags = new();
	[SerializeField] private List<string> InventoryItemNames = new();
	[SerializeField] private List<int> InventoryItemUses = new();
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
		Inventory = new(inventorySize);
		InventoryUI.Inventory = Inventory;
		Animator = GetComponent<Animator>();
		Profession = Profession.GetRandomProfession();
		FinishedInit = true;
	}
#if UNITY_EDITOR
	private void LateUpdate()
	{
		SyncDebugFields();
	}
	private void SyncDebugFields()
	{
		if (SelectedItemInfo != null)
		{
			SelectedItemTag = SelectedItemInfo.Tag;
			selectedItemUses = SelectedItemInfo.CurrentUses;
		}
		else
		{
			SelectedItemTag = ItemInfo.Tags.Unknown;
			selectedItemUses = 0;
		}
		isInVehicle = IsInVehicle;
		SyncInventoryDebugFields();
	}
	private void SyncInventoryDebugFields()
	{
		InventoryItemTags.Clear();
		InventoryItemNames.Clear();
		InventoryItemUses.Clear();
		if (Inventory == null)
			return;
		for (int i = 0; i < Inventory.Count; i++)
		{
			Item InventoryItem = Inventory[i];
			if (ReferenceEquals(InventoryItem, null))
			{
				InventoryItemTags.Add(ItemInfo.Tags.Unknown);
				InventoryItemNames.Add(string.Empty);
				InventoryItemUses.Add(0);
				continue;
			}
			ItemInfo InventoryItemInfo = InventoryItem.Info;
			if (InventoryItemInfo == null)
			{
				InventoryItemTags.Add(ItemInfo.Tags.Unknown);
				InventoryItemNames.Add(string.Empty);
				InventoryItemUses.Add(0);
				continue;
			}
			InventoryItemTags.Add(InventoryItemInfo.Tag);
			InventoryItemNames.Add(InventoryItemInfo.Name);
			InventoryItemUses.Add(InventoryItemInfo.CurrentUses);
		}
	}
#endif
	private void OnDisable() => StopMoveRoutineIfRunning();
	private void OnDestroy() => StopMoveRoutineIfRunning();
	private void StopMoveRoutineIfRunning()
	{
		if (MoveRoutine == null)
			return;
		StopCoroutine(MoveRoutine);
		MoveRoutine = null;
	}
	public void ResetForNewGame(Vector3 StartPosition)
	{
		StopMoveRoutineIfRunning();
		Path = null;
		Destination = Vector3Int.zero;
		IsInMovement = false;
		transform.position = StartPosition;
		SetPlayerVisibility(true);
		if (Vehicle != null)
			ExitVehicle();
		hasHelmet = false;
		helmetHealth = 0;
		hasVest = false;
		vestHealth = 0;
		hasNightVision = false;
		SelectedItemInfo = null;
		Inventory = new(inventorySize);
		InventoryUI.Inventory = Inventory;
		InventoryUI.SetNoneSelected();
		InventoryUI.RefreshInventoryIcons();
		InventoryUI.RefreshText();
		Profession = Profession.GetRandomProfession();
		maxEnergy = fixedMaxEnergy;
		currentHealth = maxHealth;
		CurrentEnergy = maxEnergy;
		StatsDisplayManager.RestoreHealthDisplay();
		StatsDisplayManager.RestoreEnergyDisplay(currentHealth);
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
			return;
		Path.Pop();
		Destination = Path.Pop();
		IsInMovement = true;
		// Stop movement if game ends
		if (MoveRoutine != null)
			StopCoroutine(MoveRoutine);
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
			SoundManager.Instance.PlaySound(Move);
			Vector3 ShiftedDistance = Destination + new Vector3(0.5f, 0.5f);
			// Move Player smoothly to next tile
			while (Vector3.Distance(transform.position, ShiftedDistance) > 0f)
			{
				transform.position = Vector3.MoveTowards(transform.position, 
														 ShiftedDistance, 
														 walkSpeed * Time.deltaTime);
				yield return null;
			}
			// Pop next tile in path
			if (Path != null && Path.Count > 0)
				Destination = Path.Pop();
			else break;
		}
		// When Player stops moving
		Path = null;
		IsInMovement = false;
		// Update targets if a ranged weapon is selected
		if (HasRange && !IsInVehicle)
			GameManager.Instance.UpdateTargets();
		// Notify that movement is complete
		OnMovementComplete?.Invoke();
	}
	/// <summary>
	/// Calculates area Player can move to in a turn based on CurrentEnergy
	/// </summary>
	public Dictionary<Vector3Int, Node> CalculateArea()
	{
		AStar.Initialize();
		Dictionary<Vector3Int, Node> ReachableArea = AStar.GetReachableAreaByDistance(transform.position, CurrentEnergy);
		Vector3Int StartCell = TilemapGround.WorldToCell(transform.position);
		ReachableArea.Remove(StartCell);
		return ReachableArea;
	}
	#endregion
	#region VEHICLE METHODS
	public void EnterVehicle(Vehicle EnteredVehicle)
	{
		SoundManager.Instance.PlaySound(Move);
		Vehicle = EnteredVehicle;
		transform.position = Vehicle.transform.position;
		// Hide player when entering vehicle
		SetPlayerVisibility(false);
		DecrementEnergy();
	}
	public void ExitVehicle()
    {
		// Show player when exiting vehicle
		SetPlayerVisibility(true);
		Vehicle = null;
    }
	public void VehicleMovement(Vector3 WorldPoint)
	{
		if (Vehicle == null)
			return;
		// Subscribe to vehicle movement complete event
		Vehicle.OnVehicleMovementComplete = () => {
			IsInMovement = false;
			// Trigger player movement complete event
			OnMovementComplete?.Invoke();
			// Unsubscribe to prevent memory leaks
			Vehicle.OnVehicleMovementComplete = null;
		};
		Vehicle.ComputePathAndStartMovement(WorldPoint);
		DecrementEnergy();
	}
	/// <summary>
	/// Sets Player visibility when entering and exiting vehicles
	/// </summary>
	private void SetPlayerVisibility(bool isVisible)
	{
		if (TryGetComponent(out SpriteRenderer SpriteRenderer))
			SpriteRenderer.enabled = isVisible;
		// Also hide or show the animator component
		if (Animator != null)
			Animator.enabled = isVisible;
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
			damage 		 -= absorbed;
			if (helmetHealth <= 0) hasHelmet = false;
		}
		else if (!isMeleeDamage && hasVest)
		{
			int absorbed = Mathf.Min(damage, vestHealth);
			vestHealth 	-= absorbed;
			damage 	   	-= absorbed;
			if (vestHealth <= 0) hasVest = false;
		}
		// Return if there is no more remaining damage
		if (damage <= 0) return;
		// Damage Player
		currentHealth -= damage;
		SoundManager.Instance.PlaySound(Hurt);
		// Game over if Player is killed
		if (currentHealth <= 0)
		{
			GameManager.Instance.GameOver();
			return;
		}
		// Update health display and animation state
		StatsDisplayManager.DecreaseHealthDisplay(currentHealth, maxHealth);
		Animator.SetTrigger("playerHit");
		// If 1 health left, reduce max energy to simulate weakness
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
	private void RestoreHealth() 
	{
		currentHealth = maxHealth;
		StatsDisplayManager.RestoreHealthDisplay();
		SoundManager.Instance.PlaySound(Heal);
		maxEnergy = fixedMaxEnergy;
	}
	#endregion
	#region ENERGY METHODS
	public bool HasEnergy => CurrentEnergy > 0;
	/// <summary>
	/// Decreases CurrentEnergy by 1 and updates energy display
	/// </summary>
	private void DecrementEnergy()
	{
		CurrentEnergy = Mathf.Clamp(--CurrentEnergy, 0, maxEnergy);
		StatsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, maxEnergy);
		// End turn and stop timer if no more energy
		if (!HasEnergy)
			GameManager.Instance.StopTurnTimer();
	}
	/// <summary>
	/// Sets Player CurrentEnergy to 0
	/// </summary>
	public void SetEnergyToZero()
	{
		CurrentEnergy = 0;
		StatsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, maxEnergy);
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
	/// Spends energy and durability when using a utility-style item (e.g. extinguisher).
	/// </summary>
	public void UseItem()
	{
		DecrementEnergy();
		DecrementItemDurability();
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
		UseItem();
	}
	/// <summary>
	/// Decreases item durability by 1, removes item if uses run out
	/// </summary>
	private void DecrementItemDurability()
	{
		SelectedItemInfo.DecreaseDurability();
		InventoryUI.SetCurrentSelected(InventoryUI.SelectedIndex);
		TryRemoveSelectedItem();
	}
	/// <summary>
	/// Removes selected item from inventory and resets related variables
	/// If forceRemove is true, item is removed regardless of durability
	/// </summary>
	private void TryRemoveSelectedItem(bool forceRemove = false)
	{
		if (!forceRemove && HasUses)
			return;
		InventoryUI.RemoveItem(InventoryUI.SelectedIndex);
		InventoryUI.SetNoneSelected();
		SelectedItemInfo = null;
		GameManager.Instance.ClearTargets();
		GameManager.Instance.UpdateTileAreas();
	}
	public bool HasUses => SelectedItemInfo?.CurrentUses > 0;
	/// <summary>
	/// Returns true if Player has a selected item in inventory
	/// </summary>
	public bool HasRange => SelectedItemInfo?.HasRange ?? false;
	/// <summary>
	/// Returns weapon range of selected item, 0 if no item is selected or item is not a weapon
	/// </summary>
	public int WeaponRange => SelectedItemInfo?.Range ?? 0;
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
		if (Inventory.IsEmpty || itemIndex >= Inventory.Count)
			return;
		ItemInfo ClickedItem = Inventory[itemIndex].Info;
		// If item is selected, update selection, otherwise reset
		if (InventoryUI.ProcessSelection(InventoryUI.SelectedIndex, itemIndex))
		{
			InventoryUI.SetCurrentSelected(itemIndex);
			SelectedItemInfo = ClickedItem;
			SoundManager.Instance.PlaySound(Select);
			// Only update targets if ranged weapon is selected
			if (ClickedItem.HasRange && !IsInVehicle)
				GameManager.Instance.UpdateTargets();
			// Clear targets for non-ranged weapons
			else
				GameManager.Instance.ClearTargets();
			GameManager.Instance.UpdateTileAreas();
		}
		// Item was deselected
		else
		{
			InventoryUI.SetNoneSelected();
			SelectedItemInfo = null;
			GameManager.Instance.ClearTargets();
			GameManager.Instance.UpdateTileAreas();
		}
	}
	/// <summary>
	/// Handles when Player is clicked on, returns false if item is not valid or was not used
	/// </summary>
	public bool ClickOnToUseItem()
	{
		if (SelectedItemInfo == null)
			return false;
		bool wasUsed = WasItemUsedOnPlayer();
		return wasUsed;
	}
	/// <summary>
	/// Returns true if item was used on Player
	/// </summary>
	private bool WasItemUsedOnPlayer()
	{
		if (SelectedItemInfo.Tag is ItemInfo.Tags.MedKit
			&& currentHealth < maxHealth
			&& HasEnergy)
		{
			RestoreHealth();
			// Uses energy if profession is not medic
			if (Profession.Tag is not Profession.Tags.Medic)
				DecrementEnergy();
			// Uses MedKit if profession is not master medic
			if (!(Profession.IsMaster && Profession.Tag is Profession.Tags.Medic))
				DecrementItemDurability();
			return true;
		}
		else if (SelectedItemInfo.Tag is ItemInfo.Tags.Helmet
			&& !hasHelmet)
		{
			hasHelmet = true;
			helmetHealth = SelectedItemInfo.CurrentUses;
			TryRemoveSelectedItem(true);
			DecrementEnergy();
			return true;
		}
		else if (SelectedItemInfo.Tag is ItemInfo.Tags.Vest
			&& !hasVest)
		{
			hasVest = true;
			vestHealth = SelectedItemInfo.CurrentUses;
			TryRemoveSelectedItem(true);
			DecrementEnergy();
			return true;
		}
		else if (SelectedItemInfo.Tag is ItemInfo.Tags.NightVision
			&& !hasNightVision)
		{
			hasNightVision = true;
			TryRemoveSelectedItem(true);
			DecrementEnergy();
			return true;
		}
		return false;
	}
	/// <summary>
	/// Uses an item from inventory on vehicle, returns false if item cannot be used on vehicle
	/// </summary>
	public bool ClickOnVehicleToUseItem(Vehicle Vehicle)
	{
		// Returns if no item is selected
		if (SelectedItemInfo == null)
			return false;
		// Try to use ToolKit on vehicle
		if (SelectedItemInfo.Tag is ItemInfo.Tags.ToolKit
			&& Vehicle.Repair())
		{
			DecrementItemDurability();
			return true;
		}
		// Try to use PowerCell on vehicle
		if (SelectedItemInfo.Tag is ItemInfo.Tags.PowerCell
			&& Vehicle.ClickOnToRecharge(SelectedItemInfo))
		{
			InventoryUI.SetCurrentSelected(InventoryUI.SelectedIndex);
			TryRemoveSelectedItem();
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
		if (Inventory.IsEmpty)
			return;
		// Put dropped item in temp slot out of inventory
		ItemInfo DroppedItemInfo = Inventory[itemIndex].Info;
		if (DroppedItemInfo == SelectedItemInfo)
		{
			// Clears targeting if ranged weapon is dropped
			if (HasRange)
				GameManager.Instance.ClearTargets();
			SelectedItemInfo = null;
			InventoryUI.DeselectItem(itemIndex);
			GameManager.Instance.UpdateTileAreas();
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
			InventoryUI.RemoveItem(itemIndex);
		InventoryUI.RefreshText();
		// Drop item onto ground from temp slot with preserved state
		GameManager.Instance.SpawnItem(DroppedItemInfo, transform.position);
		// Removes item from inventory and plays corresponding sound
		SoundManager.Instance.PlaySound(Move);
	}
    #endregion
}
