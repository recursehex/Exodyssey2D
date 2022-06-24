using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    private Camera mainCamera;
    public static GameManager instance;
    [HideInInspector] public bool playersTurn = true;

    [SerializeField]
    private Player player;

    [SerializeField]
    public TileDot tiledot;

    [SerializeField]
    private HealthBar enemyHealthBar;

    [SerializeField]
    public TurnTimer turnTimer;

    public GameObject[] areaMarking;

    public GameObject[] enemyTemplates;
    public List<GameObject> enemies = new List<GameObject>();

    public GameObject[] itemTemplates;
    public List<GameObject> items = new List<GameObject>();

    [SerializeField]
    private Tilemap tilemapGround;

    [SerializeField]
    private Tilemap tilemapWalls;

    [SerializeField]
    private Tilemap tilemapExit;

    [SerializeField]
    public Button endTurnButton;

    public bool needToDrawReachableAreas = false;
    public List<GameObject> reachableArea = new List<GameObject>();

    public GameObject[] reticleMarking;

    public List<GameObject> targetReticles = new List<GameObject>();

    private Dictionary<Vector3Int, Node> reachableAreasToDraw = null;

    [SerializeField]
    private Text dayText;

    [SerializeField]
    private Text levelText;

    [SerializeField]
    private GameObject levelImage;

    // time that the level screen lasts
    public float levelStartDelay = 1.5f;

    // number of items and enemies
    public int nStartItems;
    public int nStartEnemies = 1;
    private int minEnemies = 1;
    private int maxEnemies = 3;

    // tile arrays used for random generation
    public Tile[] groundTiles;
    public Tile[] wallTiles;

    // level # is for each unit of the day, with 5 per day, i.e lvl 5 means day 2
    // the units of the day are Dawn, Midday, Afternoon, Dusk, and Night
    private readonly string[] timeOfDayNames = { "DAWN", "MIDDAY", "AFTERNOON", "DUSK", "NIGHT" };
    private int level = 0;
    private int day = 1;

    private bool needToStartEnemyMovement = false;
    private bool enemiesInMovement = false;
    private int idxEnemyMoving = -1;

    private bool doingSetup;

    private MapGen mapGenerator;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        InitGame();
    }

    public static GameManager MyInstance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
            }
            return instance;
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        player.tilemapGround = tilemapGround;
        player.tilemapWalls = tilemapWalls;
        player.SetGameManager(this);
    }

    private void OnLevelWasLoaded(int index)
    {
        level++;
        levelText.text = timeOfDayNames[level % timeOfDayNames.Length];
        if (level % 5 == 0)
        {
            day++;
            dayText.text = "DAY " + day;
        }

        ResetForNextLevel();

        InitGame();
    }

    private void ResetForNextLevel()
    {
        endTurnButton.interactable = true;

        // clear tilemap tiles before generating new tiles
        tilemapGround.ClearAllTiles();
        tilemapWalls.ClearAllTiles();

        for (int i = 0; i < items.Count; i++)
        {
            Destroy(items[i].gameObject);
        }

        items.Clear();

        for (int i = 0; i < enemies.Count; i++)
        {
            Destroy(enemies[i].gameObject);
        }

        enemies.Clear();

        // reset player data
        player.transform.position = new Vector3(-3.5f, 0.5f, 0f);
        player.RestoreAP();

        minEnemies += (int)(level * 0.5);
        maxEnemies += (int)(level * 0.5);
        nStartEnemies = Random.Range(minEnemies, maxEnemies);
    }

    void InitGame()
    {
        doingSetup = true;

        needToDrawReachableAreas = true;

        levelImage.SetActive(true);
        levelText.gameObject.SetActive(true);
        dayText.gameObject.SetActive(true);
        Invoke(nameof(HideLevelLoadScreen), levelStartDelay);
        enemyHealthBar.gameObject.SetActive(false);

        nStartItems = Random.Range(2, 5);

        mapGenerator = new MapGen();

        // tilemap generation
        GroundGeneration();
        WallGeneration();
        ItemGeneration();

        // enemy generation
        EnemyGeneration();
    }

    /// <summary>
    /// Generates random ground tiles
    /// </summary>
    public void GroundGeneration()
    {
        for (int x = -4; x < 5; x++)
        {
            for (int y = -4; y < 5; y++)
            {
                Vector3Int p = new Vector3Int(x, y, 0);
                tilemapGround.SetTile(p, groundTiles[(Random.Range(0, 8))]);
            }
        }
    }

    /// <summary>
    /// Procedurally generates walls once per level
    /// </summary>
    public void WallGeneration()
    {
        mapGenerator.generateMap(tilemapWalls, wallTiles);
    }

    /// <summary>
    /// Procedurally generates items once per level
    /// </summary>
    /// 
    public void ItemGeneration()
    {
        Dictionary<ItemRarity, int> pctMap = ItemInfo.FillRarityNamestoPercentageMap();
        List<ItemInfo> allItems = ItemInfo.GenerateAllPossibleItems();

        int sumPct = 0;
        List<int> rarityPercentages = new List<int>();
        List<List<int>> ItemIndexDoubleList = new List<List<int>>();
        for (ItemRarity r = ItemRarity.Common; r < ItemRarity.Unknown; r++)
        {
            int nItemsOfRarity = 0;
            List<int> itemIndices = new List<int>();
            for (int i = 0; i < allItems.Count; i++)
            {
                if (allItems[i].rarity == r)
                {
                    nItemsOfRarity++;
                    itemIndices.Add(i);
                }
            }
            if (nItemsOfRarity > 0)
            {
                int p = pctMap[r];
                rarityPercentages.Add(p);
                ItemIndexDoubleList.Add(itemIndices);
                sumPct += p;
            }
        }

        for (int i = 0; i < nStartItems; i++)
        {
            int rSum = 0;
            int rndPct = Random.Range(0, 100);
            int j = 0;
            for (j = 0; j < rarityPercentages.Count; j++)
            {
                if (rndPct <= ((rSum + rarityPercentages[j]) * 100) / sumPct)
                {
                    break;
                }
                rSum += rarityPercentages[j];
            }

            int nItemsInGroup = ItemIndexDoubleList[j].Count;
            int rndItemInGroupIndex = Random.Range(0, nItemsInGroup);
            int rndItemIndex = ItemIndexDoubleList[j][rndItemInGroupIndex];

            GameObject item = itemTemplates[rndItemIndex];

            while (true)
            {
                int x = (Random.Range(-4, 4));
                int y = (Random.Range(-4, 4));
                Vector3Int p = new Vector3Int(x, y, 0);

                if (!tilemapWalls.HasTile(p) && !(x == -4 && y == 0))
                {
                    Vector3 shiftedDst = new Vector3(x + 0.5f, y + 0.5f, 0);

                    Item itemInPt = HasItemAtLoc(shiftedDst);

                    if (itemInPt == null)
                    {
                        GameObject instance = Instantiate(item, shiftedDst, Quaternion.identity) as GameObject;

                        Item e = instance.GetComponent<Item>();

                        e.info = ItemInfo.ItemFactoryFromNumber(rndItemIndex);
                        items.Add(instance);

                        break;
                    }
                }
            }
        }
    }

    public bool DropItem(ItemInfo inf)
    {
        bool ret = false;
        Vector3 playerLoc = player.transform.position;
        Item itemInPt = HasItemAtLoc(playerLoc);

        // check if space is empty
        if (itemInPt == null)
        {
            int idx = (int)inf.type;

            GameObject item = itemTemplates[idx];
            GameObject instance = Instantiate(item, playerLoc, Quaternion.identity) as GameObject;
            Item e = instance.GetComponent<Item>();
            e.info = inf;
            items.Add(instance);

            ret = true;
        }
        return ret;
    }

    /// <summary>
    /// Returns null if no item at loc, returns item object if item is found
    /// </summary>
    private Item HasItemAtLoc(Vector3 p)
    {
        Item ret = null;
        foreach (GameObject obj in items)
        {
            Item e = obj.GetComponent<Item>();
            if (e.transform.position == p)
            {
                ret = e;
                break;
            }
        }
        return ret;
    }

    /// <summary>
    /// Removes item when picked up or when a new level starts
    /// </summary>
    private void DestroyItemAtLoc(Vector3 p)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Item e = items[i].GetComponent<Item>();
            if (e.transform.position == p)
            {
                items.RemoveAt(i);
                Destroy(e.gameObject);
                break;
            }
        }
    }

    /// <summary>
    /// Spawns enemies for each level
    /// </summary>
    public void EnemyGeneration()
    {
        for (int i = 0; i < nStartEnemies; i++)
        {
            GameObject enemy = enemyTemplates[0];

            while (true)
            {
                int x = (int)(Random.Range(-4.0f, 4.0f));
                int y = (int)(Random.Range(-4.0f, 4.0f));
                Vector3Int p = new Vector3Int(x, y, 0);

                if (!tilemapWalls.HasTile(p) && !(x < -1 && (y > -2 && y < 2)))
                {
                    Vector3 shiftedDst = new Vector3(x + 0.5f, y + 0.5f, 0);

                    if (!HasEnemyAtLoc(shiftedDst))
                    {
                        GameObject instance = Instantiate(enemy, shiftedDst, Quaternion.identity) as GameObject;

                        GameObject obj = instance;
                        Enemy e = obj.GetComponent<Enemy>();
                        e.ExposedStart();

                        enemies.Add(instance);

                        enemies[enemies.Count - 1].GetComponent<Enemy>().SetAllEnemyList(enemies);
                        enemies[enemies.Count - 1].GetComponent<Enemy>().SetGameManager(this);

                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if enemy is on a tile so that another enemy is not spawned on the same location
    /// </summary>
    private bool HasEnemyAtLoc(Vector3 p)
    {
        bool ret = false;
        foreach (GameObject obj in enemies)
        {
            Enemy e = obj.GetComponent<Enemy>();
            if (e.transform.position == p)
            {
                ret = true;
                break;
            }
        }
        return ret;
    }

    private void HideLevelLoadScreen()
    {
        levelImage.SetActive(false);
        levelText.gameObject.SetActive(false);
        dayText.gameObject.SetActive(false);
        doingSetup = false;
    }

    /// <summary>
    /// Called when player dies
    /// </summary>
    public void GameOver()
    {
        dayText.gameObject.SetActive(true);
        dayText.text = "You died after " + ((level / 5) + 1) + " days.";
        levelImage.SetActive(true);
        enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (doingSetup || !player.finishedInit)
            return;

        DrawTileAreaIfNeeded();

        if (!player.isInMovement && endTurnButton.interactable == false && playersTurn)
        {
            endTurnButton.interactable = true;
        }

        ClickTarget();

        EnemyMovement();
    }

    /// <summary>
    /// Calculates each enemy's movement path and gives back player's turn at the end
    /// </summary>
    private void EnemyMovement()
    {
        Vector3 playerLoc = player.transform.position;

        if (enemies.Count == 0)
        {
            playersTurn = true;
            endTurnButton.interactable = true;
        }
        else
        {
            if (needToStartEnemyMovement)
            {
                endTurnButton.interactable = false;

                needToStartEnemyMovement = false;

                idxEnemyMoving = 0;
                enemies[idxEnemyMoving].GetComponent<Enemy>().CalculatePathAndStartMovement(playerLoc);
                enemiesInMovement = true;
            }
            else if (enemiesInMovement)
            {
                Enemy e = enemies[idxEnemyMoving].GetComponent<Enemy>();
                if (!e.isInMovement)
                {
                    if (idxEnemyMoving < enemies.Count - 1)
                    {
                        idxEnemyMoving++;
                        enemies[idxEnemyMoving].GetComponent<Enemy>().CalculatePathAndStartMovement(playerLoc);
                    }
                    else
                    {
                        enemiesInMovement = false;
                        UpdateEnemyAP();
                        turnTimer.ResetTimer();
                        tiledot.gameObject.SetActive(true);
                        playersTurn = true;
                        endTurnButton.interactable = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Redraws tile areas for player since their AP was reset
    /// </summary>
    public void EndTurnAreaReset()
    {
        endTurnButton.interactable = false;

        turnTimer.timerIsRunning = false;
        turnTimer.ResetTimer();

        playersTurn = false;
        tiledot.gameObject.SetActive(true);
        needToDrawReachableAreas = true;
        DrawTileAreaIfNeeded();

        needToStartEnemyMovement = true;
    }

    /// <summary>
    /// Resets every enemy's AP when player's turn ends
    /// </summary>
    public void UpdateEnemyAP()
    {
        foreach (GameObject obj in enemies)
        {
            Enemy e = obj.GetComponent<Enemy>();
            e.RestoreAP();
        }
    }

    /// <summary>
    /// Checks what mouse button is clicked and acts
    /// </summary>
    private void ClickTarget()
    {
        if (Input.GetMouseButtonDown(1)
            //&& !EventSystem.current.IsPointerOverGameObject()
            )
        {
            // this should be for player swapping to other characters or inspecting something

        }
        // if left click, begin Player movement syste
        else if (Input.GetMouseButtonDown(0))
        {
            BoundsInt size = tilemapGround.cellBounds;

            Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int clkPt = tilemapGround.WorldToCell(wp);
            Vector3 shiftedClkPt = new Vector3(clkPt.x + 0.5f, clkPt.y + 0.5f, 0);

            if (
            // check if in bounds
            clkPt.x >= size.min.x &&
            clkPt.x < size.max.x &&
            clkPt.y >= size.min.y &&
            clkPt.y < size.max.y &&

            // check if not a wall tile
            (!tilemapWalls.HasTile(clkPt)) &&

            // check if not moving
            !player.isInMovement)
            {
                // if click is on player position, ITEM CHECK
                if (shiftedClkPt == player.transform.position)
                {
                    // checks for item on tile and if so, player picks up item
                    Item itmAtPt = HasItemAtLoc(shiftedClkPt);

                    if (itmAtPt != null)
                    {
                        if (player.AddItem(itmAtPt.info))
                            DestroyItemAtLoc(shiftedClkPt);
                    }
                }

                bool fRangedWeaponSelected = player.IsRangedWeaponSelected();
                bool fIsInRange = IsInRange(clkPt);
                // for actions that require AP
                if (
                    // check if in reachable area
                    (fIsInRange || fRangedWeaponSelected) &&
                    // check if it is players turn
                    playersTurn)
                {
                    // if click on exit tile, new level
                    if (fIsInRange && tilemapExit.HasTile(clkPt))
                    {
                        OnLevelWasLoaded(level);
                        // reached exit tile
                    }
                    else
                    {
                        int idxOfEnemy = GetEnemyIdxIfPresent(clkPt);
                        bool fStartTimer = false;

                        // if there is no enemy on clicked tile
                        if (fIsInRange && idxOfEnemy == -1)
                        {
                            // if click on tile that player is not occupying
                            if (shiftedClkPt != player.transform.position)
                            {
                                // movement code
                                endTurnButton.interactable = false;
                                player.isInMovement = true;
                                needToDrawReachableAreas = true;
                                player.CalculatePathAndStartMovement(wp);
                                fStartTimer = true;
                            }
                        }
                        // attack enemy
                        else if (idxOfEnemy != -1)
                        {
                            if (player.enemyDamage > 0)
                            {
                                HandleEnemyDamage(idxOfEnemy);
                                player.ChangeActionPoints(-1);
                                player.AnimateAttack();
                                player.ProcessWeaponUse();
                                needToDrawReachableAreas = true;
                                fStartTimer = true;
                            }
                        }
                        if (fStartTimer)
                        {
                            turnTimer.StartTimer();
                        }
                    }
                }
            }
        }
        // hovering over tile, right now this is for enemy HP display
        else
        {
            BoundsInt size = tilemapGround.cellBounds;

            // mouse point on screen
            Vector3 mp = Input.mousePosition;
            // mouse point in world
            Vector3 wp = mainCamera.ScreenToWorldPoint(mp);
            // mouse point on tilemap
            Vector3Int hovPt = tilemapGround.WorldToCell(wp);

            if (
                // check if in bounds
                hovPt.x >= size.min.x &&
                hovPt.x < size.max.x &&
                hovPt.y >= size.min.y &&
                hovPt.y < size.max.y)
            {
                // check is if in reachable area to update tiledot
                if (IsInRange(hovPt))
                {
                    tiledot.MoveToPlace(hovPt);
                }

                // checks which enemy is hovered over to display its health
                int idxOfEnemy = GetEnemyIdxIfPresent(hovPt);
                if (idxOfEnemy >= 0)
                {
                    GameObject obj = enemies[idxOfEnemy];
                    Enemy e = obj.GetComponent<Enemy>();
                    enemyHealthBar.SetHealth(e.currentHP);

                    enemyHealthBar.gameObject.SetActive(true);
                    enemyHealthBar.MoveToPlace(hovPt);
                }
                else
                {
                    enemyHealthBar.gameObject.SetActive(false);
                }
            }
            // if hovering over UI
            {
                // if hovering over inventory slot
                player.ProcessHoverForInventory(mp);
            }
        }
    }

    /// <summary>
    /// Called when an enemy takes damage, assumes player.enemydamage > 0
    /// </summary>
    private void HandleEnemyDamage(int idx)
    {
        GameObject obj = enemies[idx];
        Enemy e = obj.GetComponent<Enemy>();
        e.DamageEnemy(player.enemyDamage);
        //player.subtractUP();

        // removes and destroys enemy
        if (e.currentHP <= 0)
        {
            enemies.RemoveAt(idx);
            Destroy(obj, 0f);
        }
    }

    /// <summary>
    /// Called when player takes damage to adjust health and redraw tile areas
    /// </summary>
    public void HandlePlayerDamage(int dmg)
    {
        player.ChangeHealth(dmg);
        needToDrawReachableAreas = true;
        DrawTileAreaIfNeeded();
    }

    /// <summary>
    /// Called when the turn timer ends to stop player from acting
    /// </summary>
    public void OnEndTurnTimer()
    {
        tiledot.gameObject.SetActive(false);
        ClearTileAreas();
        player.ChangeActionPoints(-3);
    }

    /// <summary>
    /// returns -1 if no enemy is present in the location pointed by p
    /// or index of enemy if enemy is present
    /// </summary>
    private int GetEnemyIdxIfPresent(Vector3Int p)
    {
        int ret = -1;
        Vector3 shiftedDst = new Vector3(p.x + 0.5f, p.y + 0.5f, 0);

        for (int i = 0; i < enemies.Count; i++)
        {
            GameObject obj = enemies[i];
            Enemy e = obj.GetComponent<Enemy>();
            if (e.transform.position == shiftedDst)
            {
                ret = i;
                break;
            }
        }
        return ret;
    }

    /// <summary>
    /// Checks if tile is within range based on player's AP
    /// </summary>
    private bool IsInRange(Vector3Int p)
    {
        if (reachableAreasToDraw != null)
        {
            foreach (KeyValuePair<Vector3Int, Node> node in reachableAreasToDraw)
            {
                if (node.Value.Position.x == p.x && node.Value.Position.y == p.y)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Deletes all tile areas, used to reset tile areas
    /// </summary>
    private void ClearTileAreas()
    {
        if (reachableArea.Count > 0)
        {
            foreach (GameObject t in reachableArea)
            {
                Destroy(t);
            }
            reachableArea.Clear();

            reachableAreasToDraw = null;
        }
    }

    private void ClearReticles()
    {
        if (targetReticles.Count > 0)
        {
            foreach (GameObject t in targetReticles)
            {
                Destroy(t);
            }
            targetReticles.Clear();
        
        }
    }


    /// <summary>
    /// Redraws tile areas when player's AP changes
    /// </summary>
    public void DrawTileAreaIfNeeded()
    {
        if (!player.isInMovement && needToDrawReachableAreas)
        {
            ClearTileAreas();

            reachableAreasToDraw = player.CalculateArea();

            if (reachableAreasToDraw.Count > 0)
            {
                foreach (KeyValuePair<Vector3Int, Node> node in reachableAreasToDraw)
                {
                    GameObject tileChoice = areaMarking[0];
                    Vector3 shiftedDst = new Vector3(node.Value.Position.x + 0.5f, node.Value.Position.y + 0.5f, node.Value.Position.z);
                    GameObject instance = Instantiate(tileChoice, shiftedDst, Quaternion.identity) as GameObject;
                    reachableArea.Add(instance);
                }
            }
            needToDrawReachableAreas = false;


            ClearReticles();

            if (player.IsRangedWeaponSelected())
            {
                foreach (GameObject obj in enemies)
                {
                    Enemy e = obj.GetComponent<Enemy>();
                    Vector3 p = e.transform.position;

                    GameObject reticleChoice = reticleMarking[0];
//                    Vector3 shiftedDst = new Vector3(p.x + 0.5f, p.y + 0.5f, p.z);
                    Vector3 shiftedDst = new Vector3(p.x + 0.0f, p.y + 0.0f, p.z);
                    GameObject instance = Instantiate(reticleChoice, shiftedDst, Quaternion.identity) as GameObject;

                    targetReticles.Add(instance);
                }
            }
        }
    }
}