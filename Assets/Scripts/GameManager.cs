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
	public GameObject tileArea;
	public TurnTimer turnTimer;
	// Must update when adding new enemy
	public GameObject[] enemyTemplates;
	public List<GameObject> enemies = new();
	// Must update when adding new item
	public GameObject[] itemTemplates;
	public List<GameObject> items = new();
	[SerializeField]
	private Tilemap tilemapGround;
	[SerializeField]
	private Tilemap tilemapWalls;
	// Tile arrays are used for random generation
	public Tile[] groundTiles;
	public Tile[] wallTiles;
	[SerializeField]
	private Tilemap tilemapExit;
	public Button endTurnButton;
	public bool needToDrawTileAreas = false;
	public List<GameObject> tileAreas = new();
	private Dictionary<Vector3Int, Node> tileAreasToDraw = null;
	#region RANGED WEAPON SYSTEM
	public GameObject targetTemplate;
	public List<GameObject> targets = new();
	public List<Vector3> targetPositions = new();
	public GameObject tracerTemplate;
	public List<GameObject> tracers = new();
	private List<Vector3> tracerPath = new();
	#endregion
	#region LOADING SCREEN
	[SerializeField]
	private Text dayText;
	[SerializeField]
	private Text levelText;
	[SerializeField]
	private GameObject levelImage;
	// How long the psuedo-loading screen lasts in seconds
	private readonly float levelStartDelay = 1.5f;
	// Level # is for each unit of the day, with 5 per day, e.g. level 5 means day 2
	private readonly string[] timeOfDayNames = { "DAWN", "NOON", "AFTERNOON", "DUSK", "NIGHT" };
	private int level = 0;
	private int day = 1;
	#endregion
	// Number of items spawned in a level
	private int spawnItemCount;
	// Number of enemies spawned in a level
	private int spawnEnemyCount;
	private bool needToStartEnemyMovement = false;
	private bool enemiesInMovement = false;
	private int indexOfMovingEnemy = -1;
	private bool doingSetup;
	private MapGen mapGenerator;
	// Start is called before the first frame update
	void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}
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
	}
	/// <summary>
	/// Resets grid state after Player enters new level
	/// </summary>
	private void ResetForNextLevel()
	{
		// Increments level and day
		level++;
		levelText.text = timeOfDayNames[level % timeOfDayNames.Length];
		if (level % 5 == 0)
		{
			day++;
			dayText.text = "DAY " + day;
		}
		
		// Resets turn timer and end turn button
		turnTimer.timerIsRunning = false;
		turnTimer.ResetTimer();
		endTurnButton.interactable = true;
		
		// Clears tilemap tiles before generating new tiles
		tilemapGround.ClearAllTiles();
		tilemapWalls.ClearAllTiles();
		
		// Destroys all items on the ground
		for (int i = 0; i < items.Count; i++)
		{
			Destroy(items[i]);
		}
		items.Clear();
		
		// Destroys all enemies
		for (int i = 0; i < enemies.Count; i++)
		{
			Destroy(enemies[i]);
		}
		enemies.Clear();
		
		// Resets Player position and energy
		player.transform.position = new(-3.5f, 0.5f, 0f);
		player.ChangeEnergy(player.MaxEnergy);
		InitGame();
		DrawTargetsAndTracers();
		tiledot.gameObject.SetActive(true);
	}
	/// <summary>
	/// Begins grid state generation
	/// </summary>
	void InitGame()
	{
		doingSetup = true;
		needToDrawTileAreas = true;
		levelImage.SetActive(true);
		levelText.gameObject.SetActive(true);
		dayText.gameObject.SetActive(true);
		Invoke(nameof(HideLevelLoadScreen), levelStartDelay);
		spawnItemCount = Random.Range(5, 10);
		spawnEnemyCount = Random.Range(1 + (int)(level * 0.5), 3 + (int)(level * 0.5));
		mapGenerator = new();
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
				tilemapGround.SetTile(tilePosition, groundTiles[Random.Range(0, groundTiles.Length)]);
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
	/// Procedurally generates items once per level, first selecting weighted rarity, then selecting equally from within that rarity
	/// </summary>
	public void ItemGeneration()
	{
		while (spawnItemCount > 0)
		{
			if (WeightedRarityGeneration.GenerateItem())
			{
				spawnItemCount--;
			}
		}
	}
	/// <summary>
	/// Returns true if no item at Player position, allowing an item in the inventory to be dropped
	/// </summary>
	/// <param name="info"></param>
	/// <returns></returns>
	public bool DropItem(ItemInfo info)
	{
		if (HasItemAtPosition(player.transform.position))
		{
			return false;
		}
		int index = (int)info.tag;
		GameObject instance = Instantiate(itemTemplates[index], player.transform.position, Quaternion.identity);
		instance.GetComponent<Item>().info = info;
		items.Add(instance);
		return true;
	}
	/// <summary>
	/// Returns false if no item at position, returns true if found
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public bool HasItemAtPosition(Vector3 position)
	{
		foreach (GameObject item in items)
		{
			if (item.GetComponent<Item>().transform.position == position)
			{
				return true;
			}
		}
		return false;
	}
	/// <summary>
	/// Returns null if no item at position, returns Item if found
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Item GetItemAtPosition(Vector3 position)
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
	/// <summary>
	/// Removes item when picked up or when new level starts
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
	private void EnemyGeneration()
	{
		while (spawnEnemyCount > 0)
		{
			if (WeightedRarityGeneration.GenerateEnemy())
			{
				spawnEnemyCount--;
			}
		}
	}
	/// <summary>
	/// Returns false if no enemy at position, returns true if found
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
		levelText.gameObject.SetActive(true);
		dayText.text = "YOU DIED";
		levelText.text = day == 1 ? "AFTER 1 DAY" : "AFTER " + ((level / 5) + 1) + " DAYS";
		levelImage.SetActive(true);
		enabled = false;
	}
	// Update is called once per frame
	void Update()
	{
		if (doingSetup
			|| !player.finishedInit
			|| player.isInMovement)
		{
			return;
		}
		
		DrawTileAreaIfNeeded();
		
		if (endTurnButton.interactable == false
			&& playersTurn)
		{
			endTurnButton.interactable = true;
		}
		
		ClickTarget();
		EnemyMovement();
	}
	/// <summary>
	/// Calculates each enemy's movement path then sets Player turn
	/// </summary>
	private void EnemyMovement()
	{
		// If there are no enemies
		if (enemies.Count == 0)
		{
			playersTurn = true;
			endTurnButton.interactable = true;
			return;
		}
		// When enemies start moving
		if (needToStartEnemyMovement)
		{
			endTurnButton.interactable = false;
			tiledot.gameObject.SetActive(false);
			ClearTileAreas();
			ClearTargetsAndTracers();
			needToStartEnemyMovement = false;
			indexOfMovingEnemy = 0;
			enemies[indexOfMovingEnemy].GetComponent<Enemy>().CalculatePathAndStartMovement(player.transform.position);
			enemiesInMovement = true;
			return;
		}
		// Enemy movement
		if (enemiesInMovement
			&& !enemies[indexOfMovingEnemy].GetComponent<Enemy>().isInMovement)
		{
			if (indexOfMovingEnemy < enemies.Count - 1)
			{
				indexOfMovingEnemy++;
				enemies[indexOfMovingEnemy].GetComponent<Enemy>().CalculatePathAndStartMovement(player.transform.position);
				return;
			}
			SetPlayersTurn();
		}
	}
	/// <summary>
	/// Once enemies stopped moving
	/// </summary>
	private void SetPlayersTurn()
	{
		enemiesInMovement = false;
		UpdateEnemyEnergy();
		turnTimer.ResetTimer();
		playersTurn = true;
		endTurnButton.interactable = true;
		needToDrawTileAreas = true;
		tiledot.gameObject.SetActive(true);
		DrawTileAreaIfNeeded();
		DrawTargetsAndTracers();
	}
	/// <summary>
	/// Called by EndTurnButton, redraws tile areas, resets timer, resets energy
	/// </summary>
	public void OnEndTurnPress()
	{
		if (player.isInMovement)
		{
			return;
		}
		endTurnButton.interactable = false;
		turnTimer.timerIsRunning = false;
		turnTimer.ResetTimer();
		playersTurn = false;
		player.ChangeEnergy(player.MaxEnergy);
		needToDrawTileAreas = true;
		if (enemies.Count == 0)
		{
			tiledot.gameObject.SetActive(true);
		}
		needToStartEnemyMovement = enemies.Count > 0;
	}
	/// <summary>
	/// Resets every enemy's energy when Player turn ends
	/// </summary>
	private void UpdateEnemyEnergy()
	{
		foreach (GameObject enemy in enemies)
		{
			enemy.GetComponent<Enemy>().RestoreEnergy();
		}
	}
	/// <summary>
	/// Acts based on what mouse button is clicked
	/// </summary>
	private void ClickTarget()
	{
		BoundsInt size = tilemapGround.cellBounds;
		Vector3 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
		Vector3Int tilePoint = tilemapGround.WorldToCell(worldPoint);
		Vector3 shiftedClickPoint = new(tilePoint.x + 0.5f, tilePoint.y + 0.5f, 0);
		// If RMB clicks
		if (Input.GetMouseButtonDown(1))
		{
			// This should be for the Player inspecting something
		}
		// If LMB clicks within grid, begin Player movement system
		else if (Input.GetMouseButtonDown(0)
			&& !player.isInMovement
			&& IsWithinCellBounds(tilePoint, size)
			&& !tilemapWalls.HasTile(tilePoint))
		{
			// If Player clicks on its own tile but energy is not used
			TryAddItem(shiftedClickPoint);
			
			// For actions that require energy, first check if it is Player turn
			if (!playersTurn || player.CurrentEnergy == 0)
			{
				return;
			}
			
			TryUseItemOnPlayer(shiftedClickPoint);
			bool isInMovementRange = IsInMovementRange(tilePoint);
			int enemyIndex = GetEnemyIndexAtPosition(tilePoint);
			
			if (TryPlayerMovement(isInMovementRange, enemyIndex, shiftedClickPoint, worldPoint))
			{
				return;
			}
			
			bool isInMeleeRange = IsInMeleeRange(shiftedClickPoint);
			bool isInRangedWeaponRange = player.GetWeaponRange() > 0 && IsInRangeForRangedWeapon(shiftedClickPoint);
			TryPlayerAttack(enemyIndex, isInMeleeRange, isInRangedWeaponRange);
		}
		// If mouse is hovering over tile and within grid
		else if (IsWithinCellBounds(tilePoint, size))
		{
			// If mouse is hovering over tileArea
			if (IsInMovementRange(tilePoint))
			{
				tiledot.MoveToPlace(tilePoint);
			}
		}
		// If mouse is hovering over UI since UI is outside the grid
		else
		{
			// If mouse is hovering over an inventory slot
			player.ProcessHoverForInventory(worldPoint);
		}
	}
	/// <summary>
	/// Tries to add item to Player inventory after clicking on own tile with item on it
	/// </summary>
	/// <param name="shiftedClickPoint"></param>
	private void TryAddItem(Vector3 shiftedClickPoint)
	{
		// If click and item is on Player position
		if (shiftedClickPoint == player.transform.position
			&& GetItemAtPosition(shiftedClickPoint) is Item itemAtPosition)
		{
			// Player picks up item
			if (player.AddItem(itemAtPosition.info))
			{
				DestroyItemAtPosition(shiftedClickPoint);
			}
		}
	}
	/// <summary>
	/// Tries to use item on Player after clicking on own tile with selected item
	/// </summary>
	/// <param name="shiftedClickPoint"></param>
	private void TryUseItemOnPlayer(Vector3 shiftedClickPoint) 
	{
		if (shiftedClickPoint != player.transform.position
			|| !player.ClickOnPlayerToUseItem())
		{
			return;
		}
		needToDrawTileAreas = true;
		turnTimer.StartTimer();
	}
	/// <summary>
	/// Tries to move to clicked tile
	/// </summary>
	/// <param name="isInMovementRange"></param>
	/// <param name="enemyIndex"></param>
	/// <param name="shiftedClickPoint"></param>
	/// <param name="worldPoint"></param>
	/// <returns></returns>
	private bool TryPlayerMovement(bool isInMovementRange, int enemyIndex, Vector3 shiftedClickPoint, Vector3 worldPoint) 
	{
		if (isInMovementRange
			&& enemyIndex == -1
			&& shiftedClickPoint != player.transform.position)
		{
			endTurnButton.interactable = false;
			player.isInMovement = true;
			needToDrawTileAreas = true;
			player.CalculatePathAndStartMovement(worldPoint);
			ClearTileAreas();
			turnTimer.StartTimer();
			return true;
		}
		return false;
	}
	/// <summary>
	/// Tries to attack enemy at clicked tile
	/// </summary>
	/// <param name="enemyIndex"></param>
	/// <param name="isInMeleeRange"></param>
	/// <param name="isInRangedWeaponRange"></param>
	private void TryPlayerAttack(int enemyIndex, bool isInMeleeRange, bool isInRangedWeaponRange) 
	{
		if (enemyIndex >= 0
			&& (isInMeleeRange || isInRangedWeaponRange)
			&& player.DamagePoints > 0)
		{
			HandleDamageToEnemy(enemyIndex);
			player.ChangeEnergy(-1);
			player.SetAnimation("playerAttack");
			player.DecreaseWeaponDurability();
			needToDrawTileAreas = true;
			turnTimer.StartTimer();
			if (isInRangedWeaponRange
				&& player.selectedItem?.currentUses == 0)
			{
				ClearTargetsAndTracers();
			}
		}
	}
	/// <summary>
	/// Called by ClickTarget(), returns true if position is within area
	/// </summary>
	/// <param name="tilePoint"></param>
	/// <param name="size"></param>
	/// <returns></returns>
	private bool IsWithinCellBounds(Vector3Int tilePoint, BoundsInt size)
	{
		return tilePoint.x >= size.min.x
			&& tilePoint.x < size.max.x
			&& tilePoint.y >= size.min.y
			&& tilePoint.y < size.max.y;
	}
	/// <summary>
	/// Called when an enemy takes damage, assumes player.DamagePoints > 0
	/// </summary>
	private void HandleDamageToEnemy(int index)
	{
		GameObject enemy = enemies[index];
		Enemy e = enemy.GetComponent<Enemy>();
		e.DamageEnemy(player.DamagePoints);
		if (e.info.currentHealth <= 0)
		{
			enemies.RemoveAt(index);
			Destroy(enemy);
			DrawTargetsAndTracers();
		}
	}
	/// <summary>
	/// Called when Player takes damage to decrease health and redraw tile areas
	/// </summary>
	public void HandleDamageToPlayer(int damage)
	{
		player.DecreaseHealth(damage);
		needToDrawTileAreas = true;
		DrawTileAreaIfNeeded();
	}
	/// <summary>
	/// Called when the turn timer ends to stop Player from acting
	/// </summary>
	public void OnTurnTimerEnd()
	{
		tiledot.gameObject.SetActive(false);
		player.ChangeEnergy(-player.MaxEnergy);
		ClearTileAreas();
		ClearTargetsAndTracers();
	}
	/// <summary>
	/// Returns true if Player is on exit tile and resets grid for next level
	/// </summary>
	/// <returns></returns>
	public bool PlayerIsOnExitTile()
	{
		if (!tilemapExit.HasTile(Vector3Int.FloorToInt(player.transform.position)))
		{
			return false;
		}
		ResetForNextLevel();
		return true;
	}
	/// <summary>
	/// Returns -1 if no enemy is present at selected position
	/// or index of enemy if enemy is present
	/// </summary>
	private int GetEnemyIndexAtPosition(Vector3Int position)
	{
		Vector3 targetPosition = position + new Vector3(0.5f, 0.5f, 0);
		return enemies.FindIndex(enemy => enemy.transform.position == targetPosition);
	}
	/// <summary>
	/// Checks if tile is within range based on Player energy
	/// </summary>
	/// <param name="position"></param>
	/// </returns></returns>
	private bool IsInMovementRange(Vector3Int position)
	{
		return tileAreasToDraw?.ContainsKey(position) == true;
	}
	/// <summary>
	/// Returns true if an enemy or object is adjacent to Player
	/// </summary>
	/// <param name="objPosition"></param>
	/// <returns></returns>
	private bool IsInMeleeRange(Vector3 objPosition)
	{
		return Vector3.Distance(player.transform.position, objPosition) <= 1.0f;
	}
	/// <summary>
	/// Return true if an object is within range of ranged weapon
	/// </summary>
	/// <param name="objPosition"></param>
	/// <returns></returns>
	private bool IsInRangeForRangedWeapon(Vector3 objPosition)
	{
		return targetPositions.Contains(objPosition);
	}
	/// <summary>
	/// Retrurns true if tilemapWalls has tile at input position
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public bool TilemapWallsHasTile(Vector3Int position) 
	{
		return tilemapWalls.HasTile(position);
	}
	public Tilemap GetTilemapWalls() 
	{
		return tilemapWalls;
	}
	public Tilemap GetTilemapGround() 
	{
		return tilemapGround;
	}
	/// <summary>
	/// Deletes all tile areas, used to reset tile areas
	/// </summary>
	private void ClearTileAreas()
	{
		if (tileAreas.Count == 0)
		{
			return;
		}
		foreach (GameObject tileArea in tileAreas)
		{
			Destroy(tileArea);
		}
		tileAreas.Clear();
		tileAreasToDraw = null;
	}
	/// <summary>
	/// Redraws tile areas when Player energy changes
	/// </summary>
	private void DrawTileAreaIfNeeded()
	{
		if (!player.isInMovement
			&& needToDrawTileAreas
			&& playersTurn)
		{
			ClearTileAreas();
			tileAreasToDraw = player.CalculateArea();
			if (tileAreasToDraw.Count > 0)
			{
				foreach (KeyValuePair<Vector3Int, Node> node in tileAreasToDraw)
				{
					Vector3 shiftedDistance = new(node.Value.Position.x + 0.5f, node.Value.Position.y + 0.5f, node.Value.Position.z);
					GameObject instance = Instantiate(tileArea, shiftedDistance, Quaternion.identity);
					tileAreas.Add(instance);
				}
			}
			needToDrawTileAreas = false;
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
	/// Draws targets and tracers when ranged weapon is selected
	/// </summary>
	public void DrawTargetsAndTracers()
	{
		ClearTargetsAndTracers();
		int weaponRange = player.GetWeaponRange();
		if (weaponRange > 0
			&& !enemiesInMovement
			&& player.CurrentEnergy > 0)
		{
			targetPositions.Clear();
			foreach (GameObject enemy in enemies)
			{
				Enemy e = enemy.GetComponent<Enemy>();
				Vector3 enemyPoint = e.transform.position;
				bool canTargetEnemy = IsInLineOfSight(player.transform.position, enemyPoint, weaponRange);
				if (tracerPath.Count > 0)
				{
					foreach (Vector3 tracerPosition in tracerPath)
					{
						Vector3 shiftedDistance = new(tracerPosition.x + 0.0f, tracerPosition.y + 0.0f, enemyPoint.z);
						GameObject instance = Instantiate(tracerTemplate, shiftedDistance, Quaternion.identity);
						tracers.Add(instance);
					}
				}
				if (canTargetEnemy)
				{
					Vector3 targetPosition = e.transform.position;
					Vector3 shiftedDistance = new(targetPosition.x + 0.0f, targetPosition.y + 0.0f, enemyPoint.z);
					GameObject instance = Instantiate(targetTemplate, shiftedDistance, Quaternion.identity);
					targets.Add(instance);
					targetPositions.Add(shiftedDistance);
				}
			}
			tracerPath.Clear();
		}
	}
	/// <summary>
	/// Returns true if player can target enemy with ranged weapon
	/// </summary>
	/// <param name="playerPosition"></param>
	/// <param name="enemyPosition"></param>
	/// <param name="weaponRange"></param>
	/// <returns></returns>
	private bool IsInLineOfSight(Vector3 playerPosition, Vector3 enemyPosition, int weaponRange)
	{
		float distanceFromPlayerToEnemy = Mathf.Sqrt(Mathf.Pow(enemyPosition.x - playerPosition.x, 2) + Mathf.Pow(enemyPosition.y - playerPosition.y, 2));
		if (distanceFromPlayerToEnemy > weaponRange)
		{
			return false;
		}
		Vector3Int playerPositionInt = new((int)(playerPosition.x - 0.5f), (int)(playerPosition.y - 0.5f), 0);
		Vector3Int enemyPositionInt = new((int)(enemyPosition.x - 0.5f), (int)(enemyPosition.y - 0.5f), 0);
		tracerPath = BresenhamsAlgorithm(playerPositionInt.x, playerPositionInt.y, enemyPositionInt.x, enemyPositionInt.y);
		foreach (Vector3 tracerPosition in tracerPath)
		{
			Vector3Int tracerPositionInt = new((int)tracerPosition.x, (int)tracerPosition.y, 0);
			if (tilemapWalls.HasTile(tracerPositionInt))
			{
				return false;
			}
		}
		return true;
	}
	private static List<Vector3> BresenhamsAlgorithm(int x0, int y0, int x1, int y1)
	{
		List<Vector3> pointsOnLine = new();
		int dx = Mathf.Abs(x1 - x0);
		int dy = Mathf.Abs(y1 - y0);
		int sx = (x0 < x1) ? 1 : -1;
		int sy = (y0 < y1) ? 1 : -1;
		int err = dx - dy;
		while (true)
		{
			pointsOnLine.Add(new Vector3(x0, y0, 0));
			if ((x0 == x1) && (y0 == y1))
			{
				break;
			}
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
		return pointsOnLine;
	}
}