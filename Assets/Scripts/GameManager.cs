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

    public TileDot tiledot;

    [SerializeField]
    private HealthBar enemyHealthBar;

    public TurnTimer turnTimer;

    public GameObject[] areaMarking;

    public GameObject[] enemyTemplates;
    public List<GameObject> enemies = new();

    public GameObject[] itemTemplates;
    public List<GameObject> items = new();

    [SerializeField]
    private Tilemap tilemapGround;

    [SerializeField]
    private Tilemap tilemapWalls;

    [SerializeField]
    private Tilemap tilemapExit;

    public Button endTurnButton;

    public bool needToDrawReachableAreas = false;
    public List<GameObject> reachableArea = new();

    public GameObject[] target;

    public List<GameObject> targets = new();

    public List<Vector3> rangedTargetPositions = new();

    public GameObject[] tracer;

    public List<GameObject> tracers = new();

    private Dictionary<Vector3Int, Node> reachableAreasToDraw = null;

    [SerializeField]
    private Text dayText;

    [SerializeField]
    private Text levelText;

    [SerializeField]
    private GameObject levelImage;

    // How long the psuedo-loading screen lasts in seconds
    private readonly float levelStartDelay = 1.5f;

    // Number of items & enemies, with min & max range for enemies
    private int nStartItems;
    private int nStartEnemies;

    // Tile arrays are used for random generation
    public Tile[] groundTiles;
    public Tile[] wallTiles;

    // Level # is for each unit of the day, with 5 per day, e.g. lvl 5 means day 2
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
        public bool canTargetEnemy = true;
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
        DrawTargetsAndTracers();
        tiledot.gameObject.SetActive(true);
    }

    private void ResetForNextLevel()
    {
        turnTimer.timerIsRunning = false;
        turnTimer.ResetTimer();

        endTurnButton.interactable = true;

        // Clears tilemap tiles before generating new tiles
        tilemapGround.ClearAllTiles();
        tilemapWalls.ClearAllTiles();

        for (int i = 0; i < items.Count; i++)
        {
            Destroy(items[i]);
        }
        items.Clear();

        for (int i = 0; i < enemies.Count; i++)
        {
            Destroy(enemies[i]);
        }
        enemies.Clear();

        // Resets Player position & AP
        player.transform.position = new Vector3(-3.5f, 0.5f, 0f);
        player.RestoreAP();
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

        nStartItems = Random.Range(5, 10);
        nStartEnemies = Random.Range(1 + (int)(level * 0.5), 3 + (int)(level * 0.5));

        mapGenerator = new MapGen();

        // Do not change order
        GroundGeneration();
        WallGeneration();
        EnemyGeneration();
        ItemGeneration();
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
                Vector3Int tilePosition = new(x, y, 0);
                tilemapGround.SetTile(tilePosition, groundTiles[Random.Range(0, 8)]);
            }
        }
    }

    /// <summary>
    /// Procedurally generates walls once per level
    /// </summary>
    public void WallGeneration()
    {
        mapGenerator.GenerateMap(tilemapWalls, wallTiles);
    }

    /// <summary>
    /// Procedurally generates items once per level, first selecting a weighted rarity, then selecting equally from within that rarity
    /// </summary>
    public void ItemGeneration()
    {
        WeightedRarityGeneration.Generation(ItemInfo.RarityPercentMap(), ItemInfo.GenerateAllRarities(), nStartItems, itemTemplates, items, tilemapWalls, this, true);
    }

    /// <summary>
    /// Returns true if no item at Player position, allowing an item in the inventory to be dropped
    /// </summary>
    /// <param name="inf"></param>
    /// <returns></returns>
    public bool DropItem(ItemInfo inf)
    {
        if (HasItemAtPosition(player.transform.position) == null)
        {
            int idx = (int)inf.tag;
            GameObject instance = Instantiate(itemTemplates[idx], player.transform.position, Quaternion.identity);
            instance.GetComponent<Item>().info = inf;
            items.Add(instance);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns null if no item at position, returns item object if item is found
    /// </summary>
    public Item HasItemAtPosition(Vector3 position)
    {
        foreach (GameObject item in items)
        {
            Item i = item.GetComponent<Item>();
            if (i.transform.position == position)
            {
                return i;
            }
        }
        return null;
    }

    public bool HasElementAtPosition(Vector3 position)
    {
        foreach (GameObject item in items)
        {
            if (item.GetComponent<Item>().transform.position == position)
            {
                return true;
            }
        }
        foreach (GameObject enemy in enemies)
        {
            if (enemy.GetComponent<Enemy>().transform.position == position)
            {
                return true;
            }
        }
        return false;
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
        WeightedRarityGeneration.Generation(EnemyInfo.RarityPercentMap(), EnemyInfo.GenerateAllRarities(), nStartEnemies, enemyTemplates, enemies, tilemapWalls, this, false);
    }

    /// <summary>
    /// Checks if enemy is on a tile to avoid collisions
    /// </summary>
    public bool HasEnemyAtPosition(Vector3 position)
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy.GetComponent<Enemy>().transform.position == position)
            {
                return true;
            }
        }
        return false;
    }

    private void HideLevelLoadScreen()
    {
        levelImage.SetActive(false);
        levelText.gameObject.SetActive(false);
        dayText.gameObject.SetActive(false);
        doingSetup = false;
    }

    /// <summary>
    /// Called when Player dies
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
    /// Calculates each enemy's movement path & gives Player turn back at the end
    /// </summary>
    private void EnemyMovement()
    {
        // If all enemies are dead
        if (enemies.Count == 0)
        {
            playersTurn = true;
            endTurnButton.interactable = true;
            return;
        }
        // When enemies begin moving
        if (needToStartEnemyMovement)
        {
            endTurnButton.interactable = false;
            ClearTargetsAndTracers();
            needToStartEnemyMovement = false;
            idxEnemyMoving = 0;
            enemies[idxEnemyMoving].GetComponent<Enemy>().CalculatePathAndStartMovement(player.transform.position);
            enemiesInMovement = true;
            return;
        }
        // Enemy movement
        if (enemiesInMovement && !enemies[idxEnemyMoving].GetComponent<Enemy>().isInMovement)
        {
            if (idxEnemyMoving < enemies.Count - 1)
            {
                idxEnemyMoving++;
                enemies[idxEnemyMoving].GetComponent<Enemy>().CalculatePathAndStartMovement(player.transform.position);
                return;
            }
            // Once enemies have finished moving
            enemiesInMovement = false;
            UpdateEnemyAP();
            turnTimer.ResetTimer();
            tiledot.gameObject.SetActive(true);
            playersTurn = true;
            endTurnButton.interactable = true;
            needToDrawReachableAreas = true;
            DrawTileAreaIfNeeded();
            DrawTargetsAndTracers();
        }
    }

    /// <summary>
    /// Called by EndTurnButton, redraws tile areas, resets timer, & resets AP
    /// </summary>
    public void OnEndTurnPress()
    {
        tiledot.gameObject.SetActive(true);
        if (!player.isInMovement)
        {
            endTurnButton.interactable = false;
            turnTimer.timerIsRunning = false;
            turnTimer.ResetTimer();
            playersTurn = false;
            player.ChangeAP(player.maxAP);
            needToDrawReachableAreas = true;
            DrawTileAreaIfNeeded();
            needToStartEnemyMovement = enemies.Count > 0;
        }
    }

    /// <summary>
    /// Resets every enemy's AP when Player turn ends
    /// </summary>
    public void UpdateEnemyAP()
    {
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<Enemy>().RestoreAP();
        }
    }

    /// <summary>
    /// Checks what mouse button is clicked & acts
    /// </summary>
    private void ClickTarget()
    {
        // If RMB clicks
        if (Input.GetMouseButtonDown(1))
        {
            // This should be for the Player swapping to other characters or inspecting something
        }
        // If LMB clicks, begin Player movement system
        else if (Input.GetMouseButtonDown(0))
        {
            BoundsInt size = tilemapGround.cellBounds;
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int clickPoint = tilemapGround.WorldToCell(worldPoint);
            Vector3 shiftedClickPoint = new(clickPoint.x + 0.5f, clickPoint.y + 0.5f, 0);
            if (
            // If mouse clicks within the grid
            clickPoint.x >= size.min.x &&
            clickPoint.x < size.max.x &&
            clickPoint.y >= size.min.y &&
            clickPoint.y < size.max.y &&
            !tilemapWalls.HasTile(clickPoint) && !player.isInMovement)
            {
                // If click is on Player position
                if (shiftedClickPoint == player.transform.position)
                {
                    // If there is an item on the same tile as Player
                    Item itemAtPosition = HasItemAtPosition(shiftedClickPoint);
                    if (itemAtPosition != null)
                    {
                        // If an item is there, Player picks it up
                        if (player.AddItem(itemAtPosition.info))
                        {
                            DestroyItemAtPosition(shiftedClickPoint);
                        }
                    }
                }
                bool isInMovementRange = IsInMovementRange(clickPoint);
                bool isInMeleeRange = IsInMeleeRange(shiftedClickPoint);
                bool isInRangeForRangedWeapon = player.GetWeaponRange() > 0 && IsInRangeForRangedWeapon(shiftedClickPoint);
                // For actions that require AP, first check if it is Player turn
                if (!playersTurn) return;

                int idxOfEnemy = GetEnemyIndexAtPosition(clickPoint);
                // If there is no enemy on the clicked tile & if the mouse clicks on a tile Player is not on
                if (isInMovementRange && idxOfEnemy == -1 && shiftedClickPoint != player.transform.position)
                {
                    // Player movement
                    endTurnButton.interactable = false;
                    player.isInMovement = true;
                    needToDrawReachableAreas = true;
                    player.CalculatePathAndStartMovement(worldPoint);
                    turnTimer.StartTimer();
                }
                // For attacking an enemy, if there exists an enemy & if it is in melee or ranged weapon range & if Player has a weapon & has AP
                else if (idxOfEnemy >= 0 && (isInMeleeRange || isInRangeForRangedWeapon) && player.damagePoints > 0 && player.currentAP > 0)
                {
                    HandleDamageToEnemy(idxOfEnemy);
                    player.ChangeAP(-1);
                    player.animator.SetTrigger("playerAttack");
                    player.UpdateWeaponUP();
                    needToDrawReachableAreas = true;
                    turnTimer.StartTimer();
                }
            }
        }
        // If mouse is hovering over a tile, to move tiledot & enemyHealthBar
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
                // If mouse is within the grid
                tilePoint.x >= size.min.x &&
                tilePoint.x < size.max.x &&
                tilePoint.y >= size.min.y &&
                tilePoint.y < size.max.y)
            {
                // If mouse is hovering over a tileArea
                if (IsInMovementRange(tilePoint))
                {
                    // Moves tiledot to the tile the mouse is hovering over
                    tiledot.MoveToPlace(tilePoint);
                }
                // Checks which enemy is hovered over to display its health
                int idxOfEnemy = GetEnemyIndexAtPosition(tilePoint);
                if (idxOfEnemy >= 0)
                {
                    Enemy e = enemies[idxOfEnemy].GetComponent<Enemy>();
                    enemyHealthBar.SetHealth(e.info.currentHP);
                    enemyHealthBar.gameObject.SetActive(true);
                    enemyHealthBar.MoveToPlace(tilePoint);
                }
                else
                {
                    enemyHealthBar.gameObject.SetActive(false);
                }
            }
            // If mouse is hovering over UI since UI is outside the grid
            else
            {
                // If mouse is hovering over an inventory slot
                player.ProcessHoverForInventory(mousePoint);
            }
        }
    }

    /// <summary>
    /// Called when an enemy takes damage, assumes player.damagePoints > 0
    /// </summary>
    private void HandleDamageToEnemy(int idx)
    {
        GameObject enemy = enemies[idx];
        Enemy e = enemy.GetComponent<Enemy>();
        e.DamageEnemy(player.damagePoints);
        if (e.info.currentHP <= 0)
        {
            enemies.RemoveAt(idx);
            Destroy(enemy, 0f);
            DrawTargetsAndTracers();
        }
    }

    /// <summary>
    /// Called when Player takes damage to adjust health & redraw tile areas
    /// </summary>
    public void HandleDamageToPlayer(int dmg)
    {
        player.ChangeHP(-dmg);
        needToDrawReachableAreas = true;
        DrawTileAreaIfNeeded();
    }

    /// <summary>
    /// Called when the turn timer ends to stop Player from acting
    /// </summary>
    public void OnTurnTimerEnd()
    {
        tiledot.gameObject.SetActive(false);
        player.ChangeAP(-player.maxAP);
        ClearTileAreas();
        ClearTargetsAndTracers();
    }

    public bool PlayerIsOnExitTile()
    {
        if (tilemapExit.HasTile(Vector3Int.FloorToInt(player.transform.position)))
        {
            OnLevelWasLoaded(level);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns -1 if no enemy is present at selected position
    /// or index of enemy if enemy is present
    /// </summary>
    private int GetEnemyIndexAtPosition(Vector3Int position)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].GetComponent<Enemy>().transform.position == position + new Vector3(0.5f, 0.5f, 0))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Checks if tile is within range based on Player AP
    /// </summary>
    private bool IsInMovementRange(Vector3Int position)
    {
        return reachableAreasToDraw?.ContainsKey(position) == true;
    }

    /// <summary>
    /// Returns true if an enemy or object is adjacent to Player
    /// </summary>
    /// <param name="p1"></param>
    /// <returns></returns>
    private bool IsInMeleeRange(Vector3 objPosition)
    {
        return Vector3.Distance(player.transform.position, objPosition) <= 1.0f;
    }

    /// <summary>
    /// Return true if an enemy or object is within range of a ranged weapon
    /// </summary>
    /// <param name="objPosition"></param>
    /// <returns></returns>
    private bool IsInRangeForRangedWeapon(Vector3 objPosition)
    {
        return rangedTargetPositions.Contains(objPosition);
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
        foreach (GameObject target in targets)
        {
            Destroy(target);
        }
        targets.Clear();
    }

    private void ClearTracers()
    {
        foreach (GameObject tracer in tracers)
        {
            Destroy(tracer);
        }
        tracers.Clear();
    }

    /// <summary>
    /// Redraws tile areas when Player AP changes
    /// </summary>
    public void DrawTileAreaIfNeeded()
    {
        if (!player.isInMovement && needToDrawReachableAreas && playersTurn)
        {
            ClearTileAreas();
            reachableAreasToDraw = player.CalculateArea();
            if (reachableAreasToDraw.Count > 0)
            {
                foreach (KeyValuePair<Vector3Int, Node> node in reachableAreasToDraw)
                {
                    Vector3 shiftedDistance = new(node.Value.Position.x + 0.5f, node.Value.Position.y + 0.5f, node.Value.Position.z);
                    GameObject instance = Instantiate(areaMarking[0], shiftedDistance, Quaternion.identity) as GameObject;
                    reachableArea.Add(instance);
                }
            }
            needToDrawReachableAreas = false;
        }
    }

    public void DrawTargetsAndTracers()
    {
        ClearTargetsAndTracers();
        int weaponRange = player.GetWeaponRange();
        if (weaponRange > 0 && !enemiesInMovement && player.currentAP > 0)
        {
            rangedTargetPositions.Clear();
            foreach (GameObject enemy in enemies)
            {
                Enemy e = enemy.GetComponent<Enemy>();
                Vector3 enemyPoint = e.transform.position;
                RangedWeaponCalculation result = IsInLineOfSight(player.transform.position, enemyPoint, weaponRange);
                if (result.tracerPath.Count > 0)
                {
                    foreach (Vector3 tracerPosition in result.tracerPath)
                    {
                        GameObject tracerChoice = tracer[0];
                        Vector3 shiftedDistance = new(tracerPosition.x + 0.0f, tracerPosition.y + 0.0f, enemyPoint.z);
                        GameObject instance = Instantiate(tracerChoice, shiftedDistance, Quaternion.identity) as GameObject;
                        tracers.Add(instance);
                    }
                }
                if (result.canTargetEnemy)
                {
                    Vector3 targetPosition = e.transform.position;
                    GameObject targetChoice = target[0];
                    Vector3 shiftedDistance = new(targetPosition.x + 0.0f, targetPosition.y + 0.0f, enemyPoint.z);
                    GameObject instance = Instantiate(targetChoice, shiftedDistance, Quaternion.identity) as GameObject;
                    targets.Add(instance);
                    rangedTargetPositions.Add(shiftedDistance);
                }
            }
        }
    }

    public RangedWeaponCalculation IsInLineOfSight(Vector3 playerPosition, Vector3 objPosition, int weaponRange)
    {
        RangedWeaponCalculation ret = new();
        float distanceFromPlayerToEnemy = Mathf.Sqrt(Mathf.Pow(objPosition.x - playerPosition.x, 2) + Mathf.Pow(objPosition.y - playerPosition.y, 2));

        if (distanceFromPlayerToEnemy > weaponRange)
        {
            ret.canTargetEnemy = false;
        }

        ret.tracerPath = BresenhamsAlgorithm((int)(playerPosition.x - 0.5f), (int)(playerPosition.y - 0.5f), (int)(objPosition.x - 0.5f), (int)(objPosition.y - 0.5f));

        foreach (Vector3 tracerPosition in ret.tracerPath)
        {
            Vector3Int tracerPositionInt = new((int)tracerPosition.x, (int)tracerPosition.y, 0);
            if (tilemapWalls.HasTile(tracerPositionInt)) // NOTE: ignore if weapon's isMortar = true
            {
                ret.canTargetEnemy = false;
                break;
            }
        }
        return ret;
    }

    public List<Vector3> BresenhamsAlgorithm(int x0, int y0, int x1, int y1)
    {
        List<Vector3> ret = new();
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            ret.Add(new Vector3(x0, y0, 0));
            if ((x0 == x1) && (y0 == y1)) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        return ret;
    }
}