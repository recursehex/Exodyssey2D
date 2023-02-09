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

    // HP and AP text
    public Text healthText;
    public Text actionPointText;

    // Audio clips relating to the player
    public AudioClip moveSound1;
    public AudioClip moveSound2;
    public AudioClip eatSound1;
    public AudioClip eatSound2;
    public AudioClip drinkSound1;
    public AudioClip drinkSound2;
    public AudioClip gameOverSound;

    private Animator animator;
    public Inventory inventory;

    [SerializeField]
    public InventoryUI inventoryUI;

    public bool finishedInit = false;
    public bool isInMovement = false;


    public ItemInfo selectedItem = null;

    GameManager gm;

    #region PATHFINDING

    [SerializeField]
    public Tilemap tilemapGround;

    [SerializeField]
    public Tilemap tilemapWalls;

    private Stack<Vector3Int> path;

    private Vector3Int destination;

    [SerializeField]
    private AStar astar;
    #endregion

    Player()
    {
        astar = new AStar();
        inventory = new Inventory();
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
        inventoryUI.SetInventory(inventory);

        currentHP = maxHP;
        currentAP = maxAP;

        astar.tilemapGround = tilemapGround;
        astar.tilemapWalls = tilemapWalls;

        // Sets text of healthText and actionPointText
        healthText.text = "HP:" + currentHP;
        actionPointText.text = "AP:" + currentAP;

        finishedInit = true;
    }

    // Update is called once per frame 
    void Update()
    {
        //if (!GameManager.instance.playersTurn) return;
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
            ChangeActionPoints(-(path.Count - 1));
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

            Vector3 shiftedDistance = new Vector3(destination.x + 0.5f, destination.y + 0.5f, destination.z);
            transform.position = Vector3.MoveTowards(transform.position, shiftedDistance, 2 * Time.deltaTime);

            float distance = Vector3.Distance(shiftedDistance, transform.position);
            if (distance <= 0f)
            {
                if (path.Count > 0)
                {
                    destination = path.Pop();
                    SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
                }
                else
                {
                    path = null;
                    isInMovement = false;
                }
            }
        }
    }

    /// <summary>
    /// Ends player's turn and switches to enemy turn
    /// </summary>
    public void EndTurn()
    {
        CheckIfGameOver();

        GameManager.instance.playersTurn = false;
        ChangeActionPoints(maxAP);
    }

    /// <summary>
    /// Changes player's HP and updates HP text display, use negative to decrease
    /// </summary>
    public void ChangeHealth(int change)
    {
        // If new HP is greater than max
        if (currentHP + change > maxHP)
        {
            currentHP = maxHP;
            healthText.text = "HP:" + currentHP;
            maxAP = 3;
        }
        else
        {
            currentHP += change;
            // If change is negative, only used when player takes damage
            if (change < 0)
            {
                animator.SetTrigger("playerHit");
                healthText.text = "HP:" + currentHP;
                // If Player reaches 1 HP, maxAP drops to 1 to simulate weakness
                if (currentHP == 1)
                {
                    maxAP = 1;
                    RestoreAP();
                }
                CheckIfGameOver();
                return;
            }
            // If change is positive but not over max
            else
            {
                healthText.text = "HP:" + currentHP;
                maxAP = 3;
            }
        }
        // NOTE: Will be replaced with a different healing sound
        SoundManager.instance.RandomizeSfx(eatSound1, eatSound2);
    }

    /// <summary>
    /// Changes player's AP and updates AP text display, use negative to decrease
    /// </summary>
    public void ChangeActionPoints(int change)
    {
        // If new AP is greater than max
        if (currentAP + change > maxAP)
        {
            currentAP = maxAP;
            actionPointText.text = "AP:" + currentAP;
        }
        else
        {
            currentAP += change;
            // If change is negative
            if (change < 0)
            {
                if (currentAP <= 0)
                {
                    currentAP = 0;
                    gm.tiledot.gameObject.SetActive(false);
                    gm.turnTimer.timeRemaining = 0;
                }
                actionPointText.text = "AP:" + currentAP;
            }
            // If change is postive but not over max, wont really be used
            else
            {
                actionPointText.text = "AP:" + currentAP;
            }
        }
    }

    /// <summary>
    /// Resets player's HP to maxHP
    /// </summary>
    public void RestoreHP()
    {
        currentHP = maxHP;
        healthText.text = "HP:" + currentHP;
    }

    /// <summary>
    /// Resets player's AP to maxAP
    /// </summary>
    public void RestoreAP()
    {
        currentAP = maxAP;
        actionPointText.text = "AP:" + currentAP;
    }

    public void ProcessWeaponUse()
    {
        // if weapon UP goes to 0 after use, remove weapon
        if (inventoryUI.ProcessWeaponUse())
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

    public void AnimateAttack()
    {
        animator.SetTrigger("playerAttack");
    }

    /// <summary>
    /// Plays game over sound and ends the game
    /// </summary>
    private void CheckIfGameOver()
    {
        if (currentHP <= 0)
        {
            SoundManager.instance.PlaySingle(gameOverSound);
            SoundManager.instance.musicSource.Stop();
            GameManager.instance.GameOver();
        }
    }

    /// <summary>
    /// Adds item to inventory when picked up
    /// </summary>
    public bool AddItem(ItemInfo itemInfo)
    {
        ItemInventory itemInventory = new ItemInventory
        {
            itemInfo = itemInfo,
            amount = 1
        };

        bool ret = inventory.AddItem(itemInventory);
        if (ret)
            inventoryUI.RefreshInventoryItems();

        return ret;
    }

    /// <summary>
    /// Uses item in player's inventory
    /// </summary>
    public void TryUseItem(int n)
    {
        if (n < inventory.itemList.Count)
        {
            ItemInfo anItem = inventory.itemList[n].itemInfo;
            gm.needToDrawReachableAreas = true;
            AfterItemUse ret = anItem.UseItem(this, n);
            // Item gets selected since it was unselected before
            if (ret.selectedIdx != -1)
            {
                selectedItem = anItem;
            }
            // Item is removed and inventory is refreshed since it was used up
            if (ret.fRemove)
            {
                inventoryUI.RemoveItem(n);
                inventoryUI.RefreshInventoryItems();
            }
        }
    }

    /// <summary>
    /// Tries to drop item in player's inventory onto the ground
    /// </summary>
    public void TryDropItem(int n)
    {
        if (n < inventory.itemList.Count)
        {
            // If drop returns false, then we can't remove it
            if (GameManager.MyInstance.DropItem(inventory.itemList[n].itemInfo))
            {
                if (inventoryUI.ProcessDamageAfterWeaponDrop(this, n))
                {
                    if (IsRangedWeaponSelected() > 0)
                    {
                        gm.ClearTargetsAndTracers();
                    }
                }
                inventoryUI.RemoveItem(n);
                inventoryUI.RefreshInventoryItems();
            }
        }
    }

    public void ProcessHoverForInventory(Vector3 mp)
    {
        inventoryUI.ProcessHoverForInventory(mp);
    }

    public void SetGameManager(GameManager g)
    {
        gm = g;
    }
}