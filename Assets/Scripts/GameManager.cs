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

    public GameObject[] target;

    public List<GameObject> targets = new List<GameObject>();

    public List<Vector3> rangedTargetPositions = new List<Vector3>();

    public GameObject[] tracer;

    public List<GameObject> tracers = new List<GameObject>();

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

    public class RangedWeaponCalculation
    {
        public RangedWeaponCalculation()
        {
            tracerPath = new List<Vector3>();
        }
        public List<Vector3> tracerPath;
        public bool fHitTarget = true;
    }

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
        DrawTargetAndTracers();
    }

    private void ResetForNextLevel()
    {
        turnTimer.timerIsRunning = false;
        turnTimer.ResetTimer();

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

        // do not change order
        GroundGeneration();
        WallGeneration();
        ItemGeneration();
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
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                tilemapGround.SetTile(tilePosition, groundTiles[(Random.Range(0, 8))]);
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

    // NEED TO MAKE THIS A SEPARATE CLASS
    /// <summary>
    /// Procedurally generates items once per level
    /// </summary>
    public void ItemGeneration()
    {
        Dictionary<ItemRarity, int> percentMap = ItemInfo.FillRarityNamestoPercentageMap();
        List<ItemInfo> allItems = ItemInfo.GenerateAllPossibleItems();

        int sumPercent = 0;
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
                int p = percentMap[r];
                rarityPercentages.Add(p);
                ItemIndexDoubleList.Add(itemIndices);
                sumPercent += p;
            }
        }

        for (int i = 0; i < nStartItems * 4; i++)
        {
            int rSum = 0;
            int randomPercent = Random.Range(0, 100);
            int j;
            for (j = 0; j < rarityPercentages.Count; j++)
            {
                if (randomPercent <= ((rSum + rarityPercentages[j]) * 100) / sumPercent)
                {
                    break;
                }
                rSum += rarityPercentages[j];
            }

            int nItemsInGroup = ItemIndexDoubleList[j].Count;
            int randomItemInGroupIndex = Random.Range(0, nItemsInGroup);
            int randomItemIndex = ItemIndexDoubleList[j][randomItemInGroupIndex];

            GameObject item = itemTemplates[randomItemIndex];

            while (true)
            {
                int x = (Random.Range(-4, 4));
                int y = (Random.Range(-4, 4));
                Vector3Int p = new Vector3Int(x, y, 0);

                if (!tilemapWalls.HasTile(p) && !(x == -4 && y == 0))
                {
                    Vector3 shiftedDistance = new Vector3(x + 0.5f, y + 0.5f, 0);

                    Item itemAtPosition = HasItemAtPosition(shiftedDistance);

                    if (itemAtPosition == null)
                    {
                        GameObject instance = Instantiate(item, shiftedDistance, Quaternion.identity) as GameObject;

                        Item e = instance.GetComponent<Item>();

                        e.info = ItemInfo.ItemFactoryFromNumber(randomItemIndex);
                        items.Add(instance);

                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns true if there is no item at the player's position, allowing an item in the inventory to be dropped
    /// </summary>
    /// <param name="inf"></param>
    /// <returns></returns>
    public bool DropItem(ItemInfo inf)
    {
        bool ret = false;
        Vector3 playerPosition = player.transform.position;
        Item itemAtPosition = HasItemAtPosition(playerPosition);

        if (itemAtPosition == null)
        {
            int idx = (int)inf.tag;

            GameObject item = itemTemplates[idx];
            GameObject instance = Instantiate(item, playerPosition, Quaternion.identity) as GameObject;
            Item e = instance.GetComponent<Item>();
            e.info = inf;
            items.Add(instance);

            ret = true;
        }
        return ret;
    }

    /// <summary>
    /// Returns null if no item at position, returns item object if item is found
    /// </summary>
    private Item HasItemAtPosition(Vector3 position)
    {
        Item ret = null;
        foreach (GameObject obj in items)
        {
            Item e = obj.GetComponent<Item>();
            if (e.transform.position == position)
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
    private void DestroyItemAtPosition(Vector3 position)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Item e = items[i].GetComponent<Item>();
            if (e.transform.position == position)
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
                Vector3Int enemyPosition = new Vector3Int(x, y, 0);

                if (!tilemapWalls.HasTile(enemyPosition) && !(x < -1 && (y > -2 && y < 2)))
                {
                    Vector3 shiftedDistance = new Vector3(x + 0.5f, y + 0.5f, 0);

                    if (!HasEnemyAtLoc(shiftedDistance))
                    {
                        GameObject instance = Instantiate(enemy, shiftedDistance, Quaternion.identity) as GameObject;

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
    /// Checks if enemy is on a tile to avoid collisions
    /// </summary>
    public bool HasEnemyAtLoc(Vector3 position)
    {
        bool ret = false;
        foreach (GameObject obj in enemies)
        {
            Enemy e = obj.GetComponent<Enemy>();
            if (e.transform.position == position)
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
        Vector3 playerPosition = player.transform.position;

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
                enemies[idxEnemyMoving].GetComponent<Enemy>().CalculatePathAndStartMovement(playerPosition);
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
                        enemies[idxEnemyMoving].GetComponent<Enemy>().CalculatePathAndStartMovement(playerPosition);
                    }
                    else
                    {
                        enemiesInMovement = false;
                        DrawTargetAndTracers();
                        UpdateEnemyAP();
                        turnTimer.ResetTimer();
                        tiledot.gameObject.SetActive(true);
                        playersTurn = true;
                        endTurnButton.interactable = true;

                        needToDrawReachableAreas = true;
                        DrawTileAreaIfNeeded();
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
        // if left click, begin Player movement system
        else if (Input.GetMouseButtonDown(0))
        {
            BoundsInt size = tilemapGround.cellBounds;

            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int clickPoint = tilemapGround.WorldToCell(worldPoint);
            Vector3 shiftedClickPoint = new Vector3(clickPoint.x + 0.5f, clickPoint.y + 0.5f, 0);

            if (
            // check if in bounds
            clickPoint.x >= size.min.x &&
            clickPoint.x < size.max.x &&
            clickPoint.y >= size.min.y &&
            clickPoint.y < size.max.y &&

            // check if not a wall tile
            (!tilemapWalls.HasTile(clickPoint)) &&

            // check if not moving
            !player.isInMovement)
            {
                // if click is on player position, ITEM CHECK
                if (shiftedClickPoint == player.transform.position)
                {
                    // checks for item on tile and if so, player picks up item
                    Item itemAtPosition = HasItemAtPosition(shiftedClickPoint);

                    if (itemAtPosition != null)
                    {
                        if (player.AddItem(itemAtPosition.info))
                        {
                            DestroyItemAtPosition(shiftedClickPoint);
                        }
                    }
                }

                bool isInMovementRange = IsInMovementRange(clickPoint);
                bool isInMeleeRange = IsInMeleeRange(shiftedClickPoint);

                bool isInRangeForRangedWeapon = false;
                if (player.IsRangedWeaponSelected() > 0)
                {
                    isInRangeForRangedWeapon = IsInRangeForRangedWeapon(shiftedClickPoint);
                }

                // for actions that require AP
                if (
                    // check if in reachable area

                    // MAYBE UNCOMMENT BELOW LINE IF FOUND PROBLEMS REGARDING MOVEMENT
                    //(isInRange || isInRangeForRangedWeapon) &&
                    // check if it is players turn
                    playersTurn)
                {
                    // if click on exit tile, new level
                    if (isInMovementRange && tilemapExit.HasTile(clickPoint))
                    {
                        OnLevelWasLoaded(level);
                    }
                    else
                    {
                        int idxOfEnemy = GetEnemyIdxIfPresent(clickPoint);
                        bool startTimer = false;

                        // if there is no enemy on clicked tile
                        if (isInMovementRange && idxOfEnemy == -1)
                        {
                            // if click on tile that player is not occupying
                            if (shiftedClickPoint != player.transform.position)
                            {
                                // movement code
                                endTurnButton.interactable = false;
                                player.isInMovement = true;
                                needToDrawReachableAreas = true;
                                player.CalculatePathAndStartMovement(worldPoint);
                                startTimer = true;
                            }
                        }
                        // attack enemy
                        else if (idxOfEnemy != -1 && (isInMeleeRange || isInRangeForRangedWeapon))
                        {
                            // if player has a weapon and AP > 0
                            if (player.enemyDamage > 0 && player.currentAP > 0)
                            {
                                HandleEnemyDamage(idxOfEnemy);
                                player.ChangeActionPoints(-1);
                                player.AnimateAttack();
                                player.ProcessWeaponUse();
                                needToDrawReachableAreas = true;
                                startTimer = true;
                            }
                        }

                        if (startTimer)
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
            Vector3 mousePoint = Input.mousePosition;
            // mouse point in world
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePoint);
            // mouse point on tilemap
            Vector3Int tilePoint = tilemapGround.WorldToCell(worldPoint);

            if (
                // check if in bounds
                tilePoint.x >= size.min.x &&
                tilePoint.x < size.max.x &&
                tilePoint.y >= size.min.y &&
                tilePoint.y < size.max.y)
            {
                // check is if in reachable area to update tiledot
                if (IsInMovementRange(tilePoint))
                {
                    tiledot.MoveToPlace(tilePoint);
                }

                // checks which enemy is hovered over to display its health
                int idxOfEnemy = GetEnemyIdxIfPresent(tilePoint);
                if (idxOfEnemy >= 0)
                {
                    GameObject obj = enemies[idxOfEnemy];
                    Enemy e = obj.GetComponent<Enemy>();
                    enemyHealthBar.SetHealth(e.currentHP);

                    enemyHealthBar.gameObject.SetActive(true);
                    enemyHealthBar.MoveToPlace(tilePoint);
                }
                else
                {
                    enemyHealthBar.gameObject.SetActive(false);
                }
            }
            // if hovering over UI
            {
                // if hovering over inventory slot
                player.ProcessHoverForInventory(mousePoint);
            }
        }
    }

    /// <summary>
    /// Called when an enemy takes damage, assumes player.enemyDamage > 0
    /// </summary>
    private void HandleEnemyDamage(int idx)
    {
        GameObject obj = enemies[idx];
        Enemy e = obj.GetComponent<Enemy>();
        e.DamageEnemy(player.enemyDamage);

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
    /// Returns -1 if no enemy is present at selected position
    /// or index of enemy if enemy is present
    /// </summary>
    private int GetEnemyIdxIfPresent(Vector3Int position)
    {
        int ret = -1;
        Vector3 shiftedDistance = new Vector3(position.x + 0.5f, position.y + 0.5f, 0);

        for (int i = 0; i < enemies.Count; i++)
        {
            GameObject obj = enemies[i];
            Enemy e = obj.GetComponent<Enemy>();
            if (e.transform.position == shiftedDistance)
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
    private bool IsInMovementRange(Vector3Int position)
    {
        if (reachableAreasToDraw != null)
        {
            foreach (KeyValuePair<Vector3Int, Node> node in reachableAreasToDraw)
            {
                if (node.Value.Position.x == position.x && node.Value.Position.y == position.y)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if an enemy is adjacent to player
    /// </summary>
    /// <param name="p1"></param>
    /// <returns></returns>
    private bool IsInMeleeRange(Vector3 objPosition)
    {
        Vector3 playerPosition = player.transform.position;

        return (playerPosition.x == objPosition.x && playerPosition.y + 1 == objPosition.y) ||
               (playerPosition.x == objPosition.x && playerPosition.y - 1 == objPosition.y) ||
               (playerPosition.x + 1 == objPosition.x && playerPosition.y == objPosition.y) ||
               (playerPosition.x - 1 == objPosition.x && playerPosition.y == objPosition.y);
    }

    /// <summary>
    /// Return true if an enemy is within range of a ranged weapon
    /// </summary>
    /// <param name="objPosition"></param>
    /// <returns></returns>
    private bool IsInRangeForRangedWeapon(Vector3 objPosition)
    {

        foreach (Vector3 targetPosition in rangedTargetPositions)
        {
            if (targetPosition.x == objPosition.x && targetPosition.y == objPosition.y)
            {
                return true;
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
            foreach (GameObject tileArea in reachableArea)
            {
                Destroy(tileArea);
            }
            reachableArea.Clear();
            reachableAreasToDraw = null;
        }
    }

    public void ClearTargetsAndTracers()
    {
        ClearTargets();
        ClearTracers();
    }

    private void ClearTargets()
    {
        if (targets.Count > 0)
        {
            foreach (GameObject target in targets)
            {
                Destroy(target);
            }
            targets.Clear();
        }
    }

    private void ClearTracers()
    {
        if (tracers.Count > 0)
        {
            foreach (GameObject tracer in tracers)
            {
                Destroy(tracer);
            }
            tracers.Clear();
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
                    Vector3 shiftedDistance = new Vector3(node.Value.Position.x + 0.5f, node.Value.Position.y + 0.5f, node.Value.Position.z);
                    GameObject instance = Instantiate(tileChoice, shiftedDistance, Quaternion.identity) as GameObject;
                    reachableArea.Add(instance);
                }
            }
            needToDrawReachableAreas = false;

            DrawTargetAndTracers();
        }
    }

    public void DrawTargetAndTracers()
    {
        ClearTargetsAndTracers();

        int weaponRange = player.IsRangedWeaponSelected();
        if (weaponRange > 0 && (!enemiesInMovement))
        {
            rangedTargetPositions.Clear();

            foreach (GameObject obj in enemies)
            {
                Enemy e = obj.GetComponent<Enemy>();
                Vector3 enemyPoint = e.transform.position;

                // NOTE: add actual range
                RangedWeaponCalculation result = IsInLineOfSight(player.transform.position, enemyPoint, weaponRange);

                if (result.tracerPath.Count > 0)
                {
                    foreach (Vector3 tracerPosition in result.tracerPath)
                    {
                        GameObject tracerChoice = tracer[0];
                        Vector3 shiftedDistance = new Vector3(tracerPosition.x + 0.0f, tracerPosition.y + 0.0f, enemyPoint.z);
                        GameObject instance = Instantiate(tracerChoice, shiftedDistance, Quaternion.identity) as GameObject;

                        tracers.Add(instance);
                    }
                }

                if (result.fHitTarget)
                {
                    Vector3 targetPosition = e.transform.position;
                    GameObject targetChoice = target[0];
                    Vector3 shiftedDistance = new Vector3(targetPosition.x + 0.0f, targetPosition.y + 0.0f, enemyPoint.z);
                    GameObject instance = Instantiate(targetChoice, shiftedDistance, Quaternion.identity) as GameObject;

                    targets.Add(instance);

                    rangedTargetPositions.Add(shiftedDistance);
                }
            }
        }
    }

    public RangedWeaponCalculation IsInLineOfSight(Vector3 playerPosition, Vector3 objPosition, int weaponRange)
    {
        RangedWeaponCalculation ret = new RangedWeaponCalculation();

        // distance between player and enemy
        float distance = Mathf.Sqrt(Mathf.Pow(objPosition.x - playerPosition.x, 2) + Mathf.Pow(objPosition.y - playerPosition.y, 2));

        if (distance > weaponRange)
        {
            ret.fHitTarget = false;

        }

        ret.tracerPath = GetPointsOnLine((int)(playerPosition.x - 0.5f), (int)(playerPosition.y - 0.5f), (int)(objPosition.x - 0.5f), (int)(objPosition.y - 0.5f));

        foreach (Vector3 tracerPosition in ret.tracerPath)
        {
            Vector3Int tracerPositionInt = new Vector3Int((int)tracerPosition.x, (int)tracerPosition.y, 0);

            if (tilemapWalls.HasTile(tracerPositionInt)) // NOTE: if weapon's isMortar = true, ignore tilemapWalls check
            {
                ret.fHitTarget = false;
                break;
            }
        }
        return ret;
    }

    public List<Vector3> GetPointsOnLine(int x0, int y0, int x1, int y1)
    {
        List<Vector3> ret = new List<Vector3>();

        int i = 0;

        bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
        if (steep)
        {
            int t;
            t = x0; // swap x0 and y0
            x0 = y0;
            y0 = t;
            t = x1; // swap x1 and y1
            x1 = y1;
            y1 = t;
        }
        if (x0 > x1)
        {
            int t;
            t = x0; // swap x0 and x1
            x0 = x1;
            x1 = t;
            t = y0; // swap y0 and y1
            y0 = y1;
            y1 = t;
        }
        int dx = x1 - x0;
        int dy = Mathf.Abs(y1 - y0);
        int error = dx / 2;
        int ystep = (y0 < y1) ? 1 : -1;
        int y = y0;
        for (int x = x0; x <= x1; x++)
        {
            Vector3 point = new Vector3((steep ? y : x) + 0.5f, (steep ? x : y) + 0.5f, 0);
            i++;
            ret.Add(point);

            error -= dy;
            if (error < 0)
            {
                y += ystep;
                error += dx;
            }
        }
        return ret;
    }
}