using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

/// <summary>
/// Contains functionality specific to the Player
/// </summary>
public class Player : MonoBehaviour
{
    public int maxHP = 3;
    public int maxAP = 3;
    public int currentHP;
    public int currentAP;

    public int damageToEnemy = 0;

    public Text healthText;
    public Text actionPointText;

    public AudioClip moveSound1;
    public AudioClip moveSound2;
    public AudioClip eatSound1;
    public AudioClip eatSound2;
    public AudioClip pressSound1;
    public AudioClip pressSound2;
    public AudioClip gameOverSound;

    public Animator animator;
    public Inventory inventory;
    public InventoryUI inventoryUI;
    public ItemInfo selectedItem = null;

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
        astar = new AStar();
        astar.tilemapGround = tilemapGround;
        astar.tilemapWalls = tilemapWalls;

        inventory = new Inventory();

        animator = GetComponent<Animator>();
        inventoryUI.SetInventory(inventory);

        currentHP = maxHP;
        currentAP = maxAP;

        healthText.text = "HP:" + currentHP;
        actionPointText.text = "AP:" + currentAP;

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
            ChangeAP(-(path.Count - 1));
            path.Pop();
            destination = path.Pop();
            SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
        }
    }

    /// <summary>
    /// Calculates area that the player can move to in a turn based on currentAP
    /// </summary>
    public Dictionary<Vector3Int, Node> CalculateArea()
    {
        astar.Initialize();
        return astar.GetReachableAreaByDistance(transform.position, currentAP);
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
                    SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
                }
                else // When Player stops moving
                {
                    path = null;
                    isInMovement = false;
                    if (gm.PlayerIsOnExitTile())
                    {
                        return;
                    }
                    gm.DrawTargetsAndTracers();
                }
            }
        }
    }

    /// <summary>
    /// Changes player's HP and updates HP text display, use negative to decrease
    /// </summary>
    public void ChangeHP(int change)
    {
        currentHP = Mathf.Clamp(currentHP + change, 0, maxHP);
        healthText.text = "HP:" + currentHP;

        if (currentHP == 0)
        {
            SoundManager.instance.PlaySingle(gameOverSound);
            SoundManager.instance.musicSource.Stop();
            GameManager.instance.GameOver();
        }

        if (change < 0)
        {
            animator.SetTrigger("playerHit");
            if (currentHP == 1)
            {
                maxAP = 1;
                RestoreAP();
            }
        }
        else
        {
            SoundManager.instance.RandomizeSfx(eatSound1, eatSound2);
            maxAP = 3;
        }
    }

    /// <summary>
    /// Changes player's AP and updates AP text display, use negative to decrease
    /// </summary>
    public void ChangeAP(int change)
    {
        currentAP = Mathf.Clamp(currentAP + change, 0, maxAP);
        actionPointText.text = "AP:" + currentAP;
        if (currentAP == 0)
        {
            gm.tiledot.gameObject.SetActive(false);
            gm.turnTimer.timeRemaining = 0;
        }
    }

    /// <summary>
    /// Resets player's AP to maxAP
    /// </summary>
    public void RestoreAP()
    {
        currentAP = maxAP;
        actionPointText.text = "AP:" + currentAP;
    }

    public void UpdateWeaponUP()
    {
        // If weapon UP == 0 after use, remove weapon
        if (inventoryUI.UpdateWeaponUP())
        {
            inventoryUI.RemoveItem(inventoryUI.GetCurrentSelected());
            inventoryUI.RefreshInventoryItems();
            damageToEnemy = 0;
        }
    }

    public int IsRangedWeaponSelected()
    {
        return inventoryUI.IsRangedWeaponSelected();
    }

    /// <summary>
    /// Adds item to inventory when picked up
    /// </summary>
    public bool AddItem(ItemInfo itemInfo)
    {
        bool itemIsadded = inventory.AddItem(new ItemInventory { itemInfo = itemInfo, amount = 1 });
        if (itemIsadded)
            inventoryUI.RefreshInventoryItems();

        return itemIsadded;
    }

    /// <summary>
    /// Uses item in player's inventory
    /// </summary>
    public void TryUseItem(int n)
    {
        ItemInfo anItem = inventory.itemList[n].itemInfo;
        AfterItemUse ret = anItem.UseItem(this, n);
        // Item gets selected since it was unselected before
        if (ret.selectedIdx != -1)
        {
            selectedItem = anItem;
            SoundManager.instance.RandomizeSfx(pressSound1, pressSound2);
        }
        // TurnTimer is started after player uses a consumable on the first move of a turn
        if (ret.consumableWasUsed && !gm.turnTimer.timerIsRunning)
        {
            gm.turnTimer.timerIsRunning = true;
            gm.needToDrawReachableAreas = true;
        }
        // Item is removed and inventory is refreshed since it was used up
        if (ret.needToRemoveItem)
        {
            inventoryUI.RemoveItem(n);
            inventoryUI.RefreshInventoryItems();
            gm.needToDrawReachableAreas = true;
        }
    }

    /// <summary>
    /// Tries to drop item in player's inventory onto the ground
    /// </summary>
    public void TryDropItem(int n)
    {
        if (!GameManager.MyInstance.DropItem(inventory.itemList[n].itemInfo))
        {
            return;
        }

        if (inventoryUI.ProcessDamageAfterWeaponDrop(this, n))
        {
            if (IsRangedWeaponSelected() > 0)
            {
                gm.ClearTargetsAndTracers();
            }
        }
        inventoryUI.RemoveItem(n);
        inventoryUI.RefreshInventoryItems();
        SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
    }

    public void ProcessHoverForInventory(Vector3 mp)
    {
        inventoryUI.ProcessHoverForInventory(mp);
    }

    public void ClearTargetsAndTracers()
    {
        gm.ClearTargetsAndTracers();
    }

    public void DrawTargetsAndTracers()
    {
        gm.DrawTargetsAndTracers();
    }

    public void SetGameManager(GameManager g)
    {
        gm = g;
    }
}