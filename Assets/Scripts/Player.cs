using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Contains functionality specific to the Player
/// </summary>
public class Player : MonoBehaviour
{
    public int maxHealth = 3;
    public int maxEnergy = 3;
    public int currentHealth = 3;
    public int currentEnergy = 3;

    public int damage = 0;

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
        if (path != null)
        {
            ChangeEnergy(-(path.Count - 1));
            path.Pop();
            destination = path.Pop();
            SoundManager.instance.PlaySound(playerMove);
        }
    }

    /// <summary>
    /// Calculates area Player can move to in a turn based on currentEnergy
    /// </summary>
    public Dictionary<Vector3Int, Node> CalculateArea()
    {
        astar.Initialize();
        return astar.GetReachableAreaByDistance(transform.position, currentEnergy);
    }

    /// <summary>
    /// Moves Player along A* path
    /// </summary>
    public void MoveAlongThePath()
    {
        if (path != null)
        {
            isInMovement = true;
            Vector3 shiftedDistance = new(destination.x + 0.5f, destination.y + 0.5f, destination.z);
            transform.position = Vector3.MoveTowards(transform.position, shiftedDistance, 2 * Time.deltaTime);
            float distance = Vector3.Distance(shiftedDistance, transform.position);
            if (distance <= 0f)
            {
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
        }
    }

    /// <summary>
    /// Changes HP & updates HP display, use negative to decrease
    /// </summary>
    public void ChangeHealth(int change)
    {
        currentHealth = Mathf.Clamp(currentHealth + change, 0, maxHealth);
        // Player is killed
        if (currentHealth == 0)
        {
            SoundManager.instance.PlaySound(gameOver);
            SoundManager.instance.musicSource.Stop();
            GameManager.instance.GameOver();
        }
        // Player is damaged
        if (change < 0)
        {
            statsDisplayManager.DecreaseHealthDisplay(currentHealth, maxHealth);
            animator.SetTrigger("playerHit");
            // Reduce max energy to simulate weakness
            if (currentHealth == 1)
            {
                currentEnergy = 1;
                statsDisplayManager.DecreaseEnergyDisplay(currentEnergy, maxEnergy);
                maxEnergy = 1;
            }
        }
        // Player is healed
        else
        {
            statsDisplayManager.RestoreHealthDisplay();
            SoundManager.instance.PlaySound(heal);
            maxEnergy = 3;
        }
    }

    /// <summary>
    /// Changes energy & updates energy display, use negative to decrease
    /// </summary>
    public void ChangeEnergy(int change)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + change, 0, maxEnergy);
        // Decreased by Player action
        if (change < 0) {
            statsDisplayManager.DecreaseEnergyDisplay(currentEnergy, maxEnergy);
            if (currentEnergy == 0)
            {
                gm.tiledot.gameObject.SetActive(false);
                gm.turnTimer.timeRemaining = 0;
            }
        }
        // Restore after end turn and new level
        else
        {
            statsDisplayManager.RestoreEnergyDisplay(currentHealth);
        }
    }

    public void UpdateWeaponUP()
    {
        // Remove weapon if weapon UP == 0 after usage
        if (inventoryUI.UpdateWeaponUP())
        {
            inventoryUI.RemoveItem(inventoryUI.GetCurrentSelected());
            inventoryUI.SetCurrentSelected(-1);
            damage = 0;
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
    public void TryClickItem(int itemIdx)
    {
        // Ensures index is within bounds & inventory has an item
        if (itemIdx >= inventory.itemList.Count || inventory.itemList.Count == 0)
        {
            return;
        }
        ItemInfo clickedItem = inventory.itemList[itemIdx].itemInfo;
        AfterItemUse ret = clickedItem.ClickItem(this, itemIdx);
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
        // Item is removed & inventory is refreshed if UP = 0
        if (ret.needToRemoveItem)
        {
            inventoryUI.RemoveItem(itemIdx);
            gm.needToDrawReachableAreas = true;
        }
    }

    /// <summary>
    /// Tries to drop item from inventory onto the ground
    /// </summary>
    public void TryDropItem(int n)
    {
        // Returns if called when inventory is empty
        if (inventory.itemList.Count == 0)
        {
            return;
        }
        // Drops item on the ground, returns if an item is occupying the tile
        if (!GameManager.MyInstance.DropItem(inventory.itemList[n].itemInfo))
        {
            return;
        }
        // Clears targeting if ranged weapon is dropped
        if (inventoryUI.ProcessDamageAfterWeaponDrop(this, n))
        {
            if (GetWeaponRange() > 0)
            {
                gm.ClearTargetsAndTracers();
            }
        }
        inventoryUI.RemoveItem(n);
        SoundManager.instance.PlaySound(playerMove);
    }

    public void ProcessHoverForInventory(Vector3 mp)
    {
        inventoryUI.ProcessHoverForInventory(mp);
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

    public void SetGameManager(GameManager g)
    {
        gm = g;
    }
}