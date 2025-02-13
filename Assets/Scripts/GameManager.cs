using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	[SerializeField] private Camera MainCamera;
	[SerializeField] private bool doingSetup;
	[SerializeField] private bool playersTurn = true;
	[SerializeField] private Player Player;
	[SerializeField] private TileDot Tiledot;
	[SerializeField] private GameObject TileArea;
	[SerializeField] private TurnTimer TurnTimer;
	[SerializeField] private Button EndTurnButton;
	private MapGen MapGenerator;
	#region ENEMIES
	// Must update when adding new enemy
	[SerializeField] private GameObject[] EnemyTemplates;
	[SerializeField] private List<Enemy> Enemies = new();
	// Number of enemies spawned in a level
	[SerializeField] private int spawnEnemyCount;
	[SerializeField] private bool needToStartEnemyMovement = false;
	[SerializeField] private bool enemiesInMovement = false;
	[SerializeField] private int indexOfMovingEnemy = -1;
	#endregion
	#region ITEMS
	// Must update when adding new item
	[SerializeField] private GameObject[] ItemTemplates;
	[SerializeField] private List<Item> Items = new();
	// Number of items spawned in a level
	[SerializeField] private int spawnItemCount;
	#endregion
	#region TILEMAPS
	[SerializeField] private Tilemap TilemapGround;
	[SerializeField] private Tilemap TilemapWalls;
	// Tile arrays are used for random generation
	[SerializeField] private Tile[] GroundTiles;
	[SerializeField] private Tile[] WallTiles;
	[SerializeField] private Tilemap TilemapExit;
	#endregion
	#region TILEAREAS
	[SerializeField] private bool needToDrawTileAreas = false;
	[SerializeField] private List<GameObject> TileAreas = new();
	private Dictionary<Vector3Int, Node> TileAreasToDraw = null;
	#endregion
	#region RANGED WEAPON SYSTEM
	[SerializeField] private GameObject TargetTemplate;
	[SerializeField] private List<GameObject> Targets = new();
	[SerializeField] private GameObject TracerTemplate;
	[SerializeField] private List<GameObject> Tracers = new();
	[SerializeField] private List<Vector3> TracerPath = new();
	#endregion
	#region LOADING SCREEN
	[SerializeField] private Text DayText;
	[SerializeField] private Text LevelText;
	[SerializeField] private GameObject LevelImage;
	// How long the psuedo-loading screen lasts in seconds
	private readonly float levelStartDelay = 1.5f;
	// Level # is for each unit of the day, with 5 per day, e.g. level 5 means day 2
	private readonly string[] timeOfDayNames = { "DAWN", "NOON", "AFTERNOON", "DUSK", "NIGHT" };
	[SerializeField] private int level = 0;
	[SerializeField] private int day = 1;
	#endregion
	// Start is called before the first frame update
	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
		InitGame();
	}
	private void Start()
	{
		MainCamera = Camera.main;
	}
	// Update is called once per frame
	void Update()
	{
		if (doingSetup
			|| !Player.FinishedInit
			|| Player.IsInMovement)
		{
			return;
		}
		
		DrawTileAreaIfNeeded();
		
		if (EndTurnButton.interactable == false
			&& playersTurn)
		{
			EndTurnButton.interactable = true;
		}
		
		MouseInput();
		EnemyMovement();
	}
	/// <summary>
	/// Resets grid state after Player enters new level
	/// </summary>
	private void ResetForNextLevel()
	{
		// Increments level and day
		level++;
		LevelText.text = timeOfDayNames[level % timeOfDayNames.Length];
		if (level % 5 == 0)
		{
			day++;
			DayText.text = $"DAY {day}";
		}
		// Resets turn timer and end turn button
		TurnTimer.timerIsRunning = false;
		TurnTimer.ResetTimer();
		// EndTurnButton.interactable = true;
		// Clears tiles, items, and enemies
		TilemapGround.ClearAllTiles();
		TilemapWalls.ClearAllTiles();
		DestroyAllItems();
		DestroyAllEnemies();
		// Resets Player position and energy
		Player.transform.position = new(-3.5f, 0.5f, 0f);
		Player.RestoreEnergy();
		InitGame();
		DrawTargetsAndTracers();
		Tiledot.gameObject.SetActive(true);
	}
	/// <summary>
	/// Begins grid state generation
	/// </summary>
	void InitGame()
	{
		doingSetup = true;
		needToDrawTileAreas = true;
		LevelImage.SetActive(true);
		LevelText.gameObject.SetActive(true);
		DayText.gameObject.SetActive(true);
		spawnItemCount = Random.Range(5, 10);
		spawnEnemyCount = Random.Range(1 + (int)(level * 0.5), 3 + (int)(level * 0.5));
		MapGenerator = new();
		GroundGeneration();
		WallGeneration();
		EnemyGeneration();
		ItemGeneration();
		Invoke(nameof(HideLevelLoadScreen), levelStartDelay);
	}
	private void HideLevelLoadScreen()
	{
		LevelImage.SetActive(false);
		LevelText.gameObject.SetActive(false);
		DayText.gameObject.SetActive(false);
		EndTurnButton.interactable = true;
		doingSetup = false;
	}
	/// <summary>
	/// Called when Player dies
	/// </summary>
	public void GameOver()
	{
		DayText.gameObject.SetActive(true);
		LevelText.gameObject.SetActive(true);
		DayText.text = "YOU DIED";
		LevelText.text = day == 1 ? "AFTER 1 DAY" : $"AFTER {(level / 5) + 1} DAYS";
		LevelImage.SetActive(true);
		enabled = false;
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
				Vector3Int TilePosition = new(x, y, 0);
				TilemapGround.SetTile(TilePosition, GroundTiles[Random.Range(0, GroundTiles.Length)]);
			}
		}
	}
	/// <summary>
	/// Procedurally generates walls once per level
	/// </summary>
	public void WallGeneration()
	{
		MapGenerator.GenerateMap(TilemapWalls, WallTiles);
	}
	#region ITEM METHODS
	/// <summary>
	/// Spawns items for each level
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
	/// Instantiates Item at Position
	/// </summary>
	public void InstantiateNewItem(ItemInfo NewItemInfo, Vector3 Position)
	{
		int index = (int)NewItemInfo.Tag;
		Item NewItem = Instantiate(ItemTemplates[index], Position, Quaternion.identity).GetComponent<Item>();
		NewItem.Info = NewItemInfo;
		Items.Add(NewItem);
	}
	/// <summary>
	/// Returns false if no item at position, returns true if found
	/// </summary>
	public bool HasItemAtPosition(Vector3 Position)
	{
		return Items.Find(Item => Item.transform.position == Position) != null;
	}
	/// <summary>
	/// Returns null if no item at position, returns Item if found
	/// </summary>
	public Item GetItemAtPosition(Vector3 Position)
	{
		return Items.Find(Item => Item.transform.position == Position);
	}
	/// <summary>
	/// Removes item at Position
	/// </summary>
	public void RemoveItemAtPosition(Item ItemAtPosition)
	{
		Items.Remove(ItemAtPosition);
	}
	/// <summary>
	/// Removes item when picked up
	/// </summary>
	private void DestroyItemAtPosition(Vector3 Position)
	{
		Item Item = Items.Find(Item => Item.transform.position == Position);
		Items.Remove(Item);
		Destroy(Item.gameObject);
	}
	/// <summary>
	/// Destroys all items on the grid
	/// </summary>
	private void DestroyAllItems()
	{
		foreach (Item Item in Items)
		{
			Destroy(Item.gameObject);
		}
		Items.Clear();
	}
	#endregion
	#region ENEMY METHODS
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
	/// Instantiates Enemy at Position
	/// </summary>
	public void InstantiateNewEnemy(EnemyInfo NewEnemyInfo, Vector3 Position)
	{
		int index = (int)NewEnemyInfo.Tag;
		Enemy NewEnemy = Instantiate(EnemyTemplates[index], Position, Quaternion.identity).GetComponent<Enemy>();
		NewEnemy.Initialize(TilemapGround, TilemapWalls, NewEnemyInfo);
		Enemies.Add(NewEnemy);
	}
	/// <summary>
	/// Returns false if no enemy at position, returns true if found
	/// </summary>
	public bool HasEnemyAtPosition(Vector3 Position)
	{
		return Enemies.Find(Enemy => Enemy.transform.position == Position) != null;
	}
	/// <summary>
	/// Returns -1 if no enemy is present at selected position
	/// or index of enemy if enemy is present
	/// </summary>
	private int GetEnemyIndexAtPosition(Vector3Int Position)
	{
		Vector3 ShiftedPosition = Position + new Vector3(0.5f, 0.5f, 0);
		return Enemies.FindIndex(Enemy => Enemy.transform.position == ShiftedPosition);
	}
	private void DestroyEnemy(Enemy Enemy)
	{
		Destroy(Enemy.StunIcon);
		Destroy(Enemy.gameObject);
	}
	/// <summary>
	/// Destroy all enemies on the grid
	/// </summary>
	private void DestroyAllEnemies()
	{
		foreach (Enemy Enemy in Enemies)
		{
			DestroyEnemy(Enemy);
		}
		Enemies.Clear();
	}
	/// <summary>
	/// Resets every enemy's energy when Player turn ends
	/// </summary>
	private void RestoreAllEnemyEnergy()
	{
		foreach (Enemy Enemy in Enemies)
		{
			Enemy.RestoreEnergy();
		}
	}
	/// <summary>
	/// Calculates each enemy's movement path then sets Player turn
	/// </summary>
	private void EnemyMovement()
	{
		// If there are no enemies
		if (Enemies.Count == 0)
		{
			playersTurn = true;
			EndTurnButton.interactable = true;
			return;
		}
		// When enemies start moving
		if (needToStartEnemyMovement)
		{
			EndTurnButton.interactable = false;
			Tiledot.gameObject.SetActive(false);
			ClearTileAreas();
			ClearTargetsAndTracers();
			needToStartEnemyMovement = false;
			indexOfMovingEnemy = 0;
			Enemies[indexOfMovingEnemy].ComputePathAndStartMovement(Player.transform.position);
			enemiesInMovement = true;
			return;
		}
		// Enemy movement
		if (enemiesInMovement
			&& !Enemies[indexOfMovingEnemy].IsInMovement)
		{
			if (indexOfMovingEnemy < Enemies.Count - 1)
			{
				indexOfMovingEnemy++;
				Enemies[indexOfMovingEnemy].ComputePathAndStartMovement(Player.transform.position);
				return;
			}
			EndEnemyTurn();
		}
	}
	/// <summary>
	/// Once enemies stopped moving
	/// </summary>
	private void EndEnemyTurn()
	{
		enemiesInMovement = false;
		RestoreAllEnemyEnergy();
		TurnTimer.ResetTimer();
		playersTurn = true;
		EndTurnButton.interactable = true;
		needToDrawTileAreas = true;
		Tiledot.gameObject.SetActive(true);
		DrawTileAreaIfNeeded();
		DrawTargetsAndTracers();
	}
	/// <summary>
	/// Called when an enemy takes damage, assumes player.DamagePoints > 0
	/// </summary>
	private void HandleDamageToEnemy(int index)
	{
		Enemy DamagedEnemy = Enemies[index];
		DamagedEnemy.DecreaseHealthBy(Player.DamagePoints);
		// If enemy is dead
		if (DamagedEnemy.Info.CurrentHealth <= 0)
		{
			Enemies.RemoveAt(index);
			DestroyEnemy(DamagedEnemy);
			DrawTargetsAndTracers();
			return;
		}
		// If weapon is stunning
		if (Player.SelectedItemInfo.IsStunning)
		{
			// Enemy cannot move for 1 turn
			DamagedEnemy.Info.IsStunned = true;
			DamagedEnemy.StunIcon.SetActive(true);
			DrawTargetsAndTracers();
		}
	}
	/// <summary>
	/// Returns true if there is a wall at a position
	/// </summary>
	public bool HasWallAtPosition(Vector3Int Position)
	{
		return TilemapWalls.HasTile(Position);
	}
	#endregion
	#region PLAYER METHODS
	public void EndTurnTimer()
	{
		TurnTimer.timeRemaining = 0;
		TurnTimer.timerIsRunning = false;
	}
	/// <summary>
	/// Called by EndTurnButton, redraws tile areas, resets timer, resets energy
	/// </summary>
	public void OnEndTurnPress()
	{
		if (Player.IsInMovement)
		{
			return;
		}
		EndTurnButton.interactable = false;
		TurnTimer.timerIsRunning = false;
		TurnTimer.ResetTimer();
		playersTurn = false;
		Player.RestoreEnergy();
		needToDrawTileAreas = true;
		if (Enemies.Count == 0)
		{
			Tiledot.gameObject.SetActive(true);
		}
		needToStartEnemyMovement = Enemies.Count > 0;
	}
	/// <summary>
	/// Called when the turn timer ends to stop Player from acting
	/// </summary>
	public void OnTurnTimerEnd()
	{
		Tiledot.gameObject.SetActive(false);
		Player.SetEnergyToZero();
		ClearTileAreas();
		ClearTargetsAndTracers();
	}
	/// <summary>
	/// Returns true if Player is on exit tile and resets grid for next level
	/// </summary>
	public bool PlayerIsOnExitTile()
	{
		if (!TilemapExit.HasTile(Vector3Int.FloorToInt(Player.transform.position)))
		{
			return false;
		}
		ResetForNextLevel();
		return true;
	}
	/// <summary>
	/// Acts based on what mouse button is clicked
	/// </summary>
	private void MouseInput()
	{
		BoundsInt CellBounds = TilemapGround.cellBounds;
		Vector3 WorldPoint = MainCamera.ScreenToWorldPoint(Input.mousePosition);
		Vector3Int TilePoint = TilemapGround.WorldToCell(WorldPoint);
		Vector3 ShiftedClickPoint = new(TilePoint.x + 0.5f, TilePoint.y + 0.5f, 0);
		// If LMB clicks within grid, begin Player movement system
		if (Input.GetMouseButtonDown(0)
			&& !Player.IsInMovement
			&& IsWithinCellBounds(TilePoint, CellBounds)
			&& !TilemapWalls.HasTile(TilePoint))
		{
			// If Player clicks on its own tile but energy is not used
			TryAddItem(ShiftedClickPoint);
			// For actions that require energy, first check if it is Player turn
			if (!playersTurn || Player.CurrentEnergy == 0)
			{
				return;
			}
			TryUseItemOnPlayer(ShiftedClickPoint);
			bool isInMovementRange = IsInMovementRange(TilePoint);
			int enemyIndex = GetEnemyIndexAtPosition(TilePoint);
			if (TryPlayerMovement(isInMovementRange, enemyIndex, ShiftedClickPoint, WorldPoint))
			{
				return;
			}
			bool isInMeleeRange = IsInMeleeRange(ShiftedClickPoint);
			bool isInRangedWeaponRange = Player.GetWeaponRange() > 0 && IsInRangedWeaponRange(ShiftedClickPoint);
			TryPlayerAttack(enemyIndex, isInMeleeRange, isInRangedWeaponRange);
		}
		// If mouse is hovering over tile and within grid
		else if (IsWithinCellBounds(TilePoint, CellBounds))
		{
			// If mouse is hovering over tileArea
			if (IsInMovementRange(TilePoint))
			{
				Tiledot.MoveToPlace(TilePoint);
			}
		}
		// If mouse is hovering over UI since UI is outside the grid
		else
		{
			// If mouse is hovering over an inventory slot
			Player.InventoryUI.ProcessHoverForInventory(WorldPoint);
		}
	}
	/// <summary>
	/// Tries to add item to Player inventory after clicking on own tile with item on it
	/// </summary>
	private void TryAddItem(Vector3 ShiftedClickPoint)
	{
		// If click and item is on Player position
		if (ShiftedClickPoint == Player.transform.position
			&& GetItemAtPosition(ShiftedClickPoint) is Item ItemAtPosition)
		{
			// Player picks up item
			if (Player.TryAddItem(ItemAtPosition))
			{
				DestroyItemAtPosition(ShiftedClickPoint);
			}
		}
	}
	/// <summary>
	/// Tries to use item on Player after clicking on own tile with selected item
	/// </summary>
	private void TryUseItemOnPlayer(Vector3 ShiftedClickPoint) 
	{
		if (ShiftedClickPoint != Player.transform.position
			|| !Player.ClickOnPlayerToUseItem())
		{
			return;
		}
		needToDrawTileAreas = true;
		TurnTimer.StartTimer();
	}
	/// <summary>
	/// Tries to move to clicked tile
	/// </summary>
	private bool TryPlayerMovement(bool isInMovementRange, int enemyIndex, Vector3 ShiftedClickPoint, Vector3 WorldPoint) 
	{
		if (isInMovementRange
			&& enemyIndex == -1
			&& ShiftedClickPoint != Player.transform.position)
		{
			EndTurnButton.interactable = false;
			Player.IsInMovement = true;
			needToDrawTileAreas = true;
			Player.ComputePathAndStartMovement(WorldPoint);
			ClearTileAreas();
			TurnTimer.StartTimer();
			return true;
		}
		return false;
	}
	/// <summary>
	/// Tries to attack enemy at clicked tile
	/// </summary>
	private void TryPlayerAttack(int enemyIndex, bool isInMeleeRange, bool isInRangedWeaponRange) 
	{
		if (enemyIndex >= 0
			&& (isInMeleeRange || isInRangedWeaponRange)
			&& Player.SelectedItemInfo?.Type is ItemInfo.Types.Weapon)
		{
			HandleDamageToEnemy(enemyIndex);
			Player.AttackEnemy();
			needToDrawTileAreas = true;
			TurnTimer.StartTimer();
			if (isInRangedWeaponRange
				&& Player.SelectedItemInfo?.CurrentUses == 0)
			{
				ClearTargetsAndTracers();
			}
		}
	}
	/// <summary>
	/// Called by MouseInput(), returns true if position is within area
	/// </summary>
	private bool IsWithinCellBounds(Vector3Int TilePoint, BoundsInt CellBounds)
	{
		return CellBounds.Contains(TilePoint);
	}
	/// <summary>
	/// Called when Player takes damage to decrease health and redraw tile areas
	/// </summary>
	public void HandleDamageToPlayer(int damage)
	{
		Player.DecreaseHealth(damage);
		needToDrawTileAreas = true;
		DrawTileAreaIfNeeded();
	}
	/// <summary>
	/// Checks if tile is within range based on Player energy
	/// </summary>
	private bool IsInMovementRange(Vector3Int Position)
	{
		return TileAreasToDraw?.ContainsKey(Position) == true;
	}
	/// <summary>
	/// Returns true if an enemy is adjacent to Player
	/// </summary>
	private bool IsInMeleeRange(Vector3 ObjectPosition)
	{
		return Vector3.Distance(Player.transform.position, ObjectPosition) <= 1.0f;
	}
	/// <summary>
	/// Returns true if an enemy is within range of ranged weapon
	/// </summary>
	private bool IsInRangedWeaponRange(Vector3 ObjectPosition)
	{
		return Targets.Find(Target => Target.transform.position == ObjectPosition);
	}
	#endregion
	#region TILEAREAS
	/// <summary>
	/// Deletes all tile areas, used to reset tile areas
	/// </summary>
	private void ClearTileAreas()
	{
		if (TileAreas.Count == 0)
		{
			return;
		}
		foreach (GameObject TileArea in TileAreas)
		{
			Destroy(TileArea);
		}
		TileAreas.Clear();
		TileAreasToDraw = null;
	}
	/// <summary>
	/// Redraws tile areas when Player energy changes
	/// </summary>
	private void DrawTileAreaIfNeeded()
	{
		if (!Player.IsInMovement
			&& needToDrawTileAreas
			&& playersTurn)
		{
			ClearTileAreas();
			TileAreasToDraw = Player.CalculateArea();
			if (TileAreasToDraw.Count > 0)
			{
				foreach (KeyValuePair<Vector3Int, Node> TileAreaPosition in TileAreasToDraw)
				{
					Vector3 ShiftedDistance = new(TileAreaPosition.Value.Position.x + 0.5f, TileAreaPosition.Value.Position.y + 0.5f, TileAreaPosition.Value.Position.z);
					GameObject TileAreaInstance = Instantiate(TileArea, ShiftedDistance, Quaternion.identity);
					TileAreas.Add(TileAreaInstance);
				}
			}
			needToDrawTileAreas = false;
		}
	}
	#endregion
	#region TARGETS AND TRACERS
	public void ClearTargetsAndTracers()
	{
		ClearTargets();
		ClearTracers();
	}
	private void ClearTargets()
	{
		foreach (GameObject Target in Targets)
		{
			Destroy(Target);
		}
		Targets.Clear();
	}
	private void ClearTracers()
	{
		foreach (GameObject Tracer in Tracers)
		{
			Destroy(Tracer);
		}
		Tracers.Clear();
	}
	/// <summary>
	/// Draws targets and tracers when ranged weapon is selected
	/// </summary>
	public void DrawTargetsAndTracers()
	{
		ClearTargetsAndTracers();
		int weaponRange = Player.GetWeaponRange();
		if (weaponRange > 0
			&& !enemiesInMovement
			&& Player.CurrentEnergy > 0)
		{
			foreach (Enemy Enemy in Enemies)
			{
				// Don't draw targets and tracers if enemy is currently stunned and using stunning ranged weapon
				if (Enemy.StunIcon.activeSelf && Player.SelectedItemInfo.IsStunning)
				{
					continue;
				}
				Vector3 EnemyPoint = Enemy.transform.position;
				bool canTargetEnemy = IsInLineOfSight(Player.transform.position, EnemyPoint, weaponRange);
				if (TracerPath.Count > 0)
				{
					foreach (Vector3 tracerPosition in TracerPath)
					{
						GameObject Tracer = Instantiate(TracerTemplate, tracerPosition, Quaternion.identity);
						Tracers.Add(Tracer);
					}
				}
				if (canTargetEnemy)
				{
					GameObject Target = Instantiate(TargetTemplate, Enemy.transform.position, Quaternion.identity);
					Targets.Add(Target);
				}
			}
			TracerPath.Clear();
		}
	}
	/// <summary>
	/// Returns true if player can target enemy with ranged weapon
	/// </summary>
	private bool IsInLineOfSight(Vector3 PlayerPosition, Vector3 EnemyPosition, int weaponRange)
	{
		float distanceFromPlayerToEnemy = Mathf.Sqrt(Mathf.Pow(EnemyPosition.x - PlayerPosition.x, 2) + Mathf.Pow(EnemyPosition.y - PlayerPosition.y, 2));
		if (distanceFromPlayerToEnemy > weaponRange)
		{
			return false;
		}
		Vector3Int PlayerPositionInt = new((int)(PlayerPosition.x - 0.5f), (int)(PlayerPosition.y - 0.5f), 0);
		Vector3Int EnemyPositionInt = new((int)(EnemyPosition.x - 0.5f), (int)(EnemyPosition.y - 0.5f), 0);
		TracerPath = BresenhamsAlgorithm(PlayerPositionInt.x, PlayerPositionInt.y, EnemyPositionInt.x, EnemyPositionInt.y);
		foreach (Vector3 TracerPosition in TracerPath)
		{
			Vector3Int TracerPositionInt = new((int)TracerPosition.x, (int)TracerPosition.y, 0);
			if (TilemapWalls.HasTile(TracerPositionInt))
			{
				return false;
			}
		}
		return true;
	}
	/// <summary>
	/// Returns list of points on line from (x0, y0) to (x1, y1)
	/// </summary>
	private static List<Vector3> BresenhamsAlgorithm(int x0, int y0, int x1, int y1)
	{
		List<Vector3> PointsOnLine = new();
		int dx = Mathf.Abs(x1 - x0);
		int dy = Mathf.Abs(y1 - y0);
		int sx = (x0 < x1) ? 1 : -1;
		int sy = (y0 < y1) ? 1 : -1;
		int err = dx - dy;
		while (true)
		{
			PointsOnLine.Add(new(x0, y0, 0));
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
		return PointsOnLine;
	}
	#endregion
}