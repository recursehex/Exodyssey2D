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
    /// Changes HP and updates HP display, use negative to decrease
    /// </summary>
    public void ChangeHealth(int change)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + change, 0, MaxHealth);
        // Player is killed
        if (CurrentHealth == 0)
        {
            SoundManager.instance.PlaySound(gameOver);
            SoundManager.instance.musicSource.Stop();
            GameManager.instance.GameOver();
        }
        // Player is damaged
        if (change < 0)
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
        // Player is healed
        else
        {
            statsDisplayManager.RestoreHealthDisplay();
            SoundManager.instance.PlaySound(heal);
            MaxEnergy = 3;
        }
    }
    /// <summary>
    /// Changes energy and updates energy display, use negative to decrease
    /// </summary>
    public void ChangeEnergy(int change)
    {
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + change, 0, MaxEnergy);
        // End turn and stop timer
        if (CurrentEnergy == 0)
        {
            gm.turnTimer.timeRemaining = 0;
        }
        // Decreased by Player action
        if (change < 0)
        {
            statsDisplayManager.DecreaseEnergyDisplay(CurrentEnergy, MaxEnergy);
        }
        // Restore after end turn and new level
        else statsDisplayManager.RestoreEnergyDisplay(CurrentHealth);
    }
    /// <summary>
    /// Changes weapon uses after use, removes weapon if uses == 0
    /// </summary>
    public void UpdateWeaponUP()
    {
        if (inventoryUI.UpdateWeaponUP())
        {
            inventoryUI.RemoveItem(inventoryUI.GetCurrentSelected());
            inventoryUI.SetCurrentSelected(-1);
            DamagePoints = 0;
        }
    }
    public int GetWeaponRange()
    {
        return inventoryUI.GetWeaponRange();
    }
    /// <summary>
    /// Adds item to inventory when picked up
    /// </summary>
    public bool AddItem(ItemInfo itemInfo)
    {
        bool itemIsadded = inventory.AddItem(new ItemInventory { itemInfo = itemInfo });
        if (itemIsadded) inventoryUI.RefreshInventoryItems();
        return itemIsadded;
    }
    /// <summary>
    /// Clicks on item in inventory
    /// </summary>
    public void TryClickItem(int itemIndex)
    {
        // Ensures index is within bounds and inventory has an item
        if (itemIndex >= inventory.itemList.Count || inventory.itemList.Count == 0) return;
        ItemInfo clickedItem = inventory.itemList[itemIndex].itemInfo;
        AfterItemUse ret = clickedItem.ClickItem(this, itemIndex);
        // Item gets selected since it was unselected before
        if (ret.selectedIdx != -1)
        {
            selectedItem = clickedItem;
            SoundManager.instance.PlaySound(select);
        }
        // TurnTimer is started after Player uses a consumable on the first move of a turn
        if (ret.consumableWasUsed && !gm.turnTimer.timerIsRunning)
        {
            gm.turnTimer.timerIsRunning = true;
            gm.needToDrawReachableAreas = true;
        }
        // Item is removed and inventory is refreshed if UP = 0
        if (ret.needToRemoveItem)
        {
            inventoryUI.RemoveItem(itemIndex);
            gm.needToDrawReachableAreas = true;
        }
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
        // Clears targeting if ranged weapon is dropped
        if (inventoryUI.ProcessDamageAfterWeaponDrop(this, itemIndex) && GetWeaponRange() > 0) gm.ClearTargetsAndTracers();
        // Removes item from inventory and plays corresponding sound
        inventoryUI.RemoveItem(itemIndex);
        SoundManager.instance.PlaySound(playerMove);
    }
    public void ProcessHoverForInventory(Vector3 mousePosition)
    {
        inventoryUI.ProcessHoverForInventory(mousePosition);
    }
    // Called by ItemInfo
    public void ClearTargetsAndTracers()
    {
        gm.ClearTargetsAndTracers();
    }
    // Called by ItemInfo
    public void DrawTargetsAndTracers()
    {
        gm.DrawTargetsAndTracers();
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