using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
	private Camera MainCamera;
	public static GameManager Instance;
	public bool playersTurn = true;
	[SerializeField]
	private Player Player;
	public TileDot Tiledot;
	public GameObject TileArea;
	public TurnTimer TurnTimer;
	// Must update when adding new enemy
	public GameObject[] EnemyTemplates;
	public List<GameObject> Enemies = new();
	// Must update when adding new item
	public GameObject[] ItemTemplates;
	public List<GameObject> Items = new();
	[SerializeField]
	private Tilemap TilemapGround;
	[SerializeField]
	private Tilemap TilemapWalls;
	// Tile arrays are used for random generation
	public Tile[] GroundTiles;
	public Tile[] WallTiles;
	[SerializeField]
	private Tilemap TilemapExit;
	public Button EndTurnButton;
	public bool needToDrawTileAreas = false;
	public List<GameObject> TileAreas = new();
	private Dictionary<Vector3Int, Node> TileAreasToDraw = null;
	#region RANGED WEAPON SYSTEM
	public GameObject TargetTemplate;
	public List<GameObject> Targets = new();
	public List<Vector3> TargetPositions = new();
	public GameObject TracerTemplate;
	public List<GameObject> Tracers = new();
	private List<Vector3> TracerPath = new();
	#endregion
	#region LOADING SCREEN
	[SerializeField]
	private Text DayText;
	[SerializeField]
	private Text LevelText;
	[SerializeField]
	private GameObject LevelImage;
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
	private MapGen MapGenerator;
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
	public static GameManager MyInstance
	{
		get
		{
			if (Instance == null)
			{
				Instance = FindObjectOfType<GameManager>();
			}
			return Instance;
		}
	}
	private void Start()
	{
		MainCamera = Camera.main;
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
			DayText.text = "DAY " + day;
		}
		
		// Resets turn timer and end turn button
		TurnTimer.timerIsRunning = false;
		TurnTimer.ResetTimer();
		EndTurnButton.interactable = true;
		
		// Clears tilemap tiles before generating new tiles
		TilemapGround.ClearAllTiles();
		TilemapWalls.ClearAllTiles();
		
		// Destroys all items on the ground
		for (int i = 0; i < Items.Count; i++)
		{
			Destroy(Items[i]);
		}
		Items.Clear();
		
		// Destroys all enemies
		for (int i = 0; i < Enemies.Count; i++)
		{
			Destroy(Enemies[i]);
		}
		Enemies.Clear();
		
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
		Invoke(nameof(HideLevelLoadScreen), levelStartDelay);
		spawnItemCount = Random.Range(5, 10);
		spawnEnemyCount = Random.Range(1 + (int)(level * 0.5), 3 + (int)(level * 0.5));
		MapGenerator = new();
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
	/// Returns true if no item at Player position, allowing an item in the inventory to be dropped
	/// </summary>
	public bool DropItem(ItemInfo ItemInfo)
	{
		if (HasItemAtPosition(Player.transform.position))
		{
			return false;
		}
		int index = (int)ItemInfo.Tag;
		GameObject DroppedItem = Instantiate(ItemTemplates[index], Player.transform.position, Quaternion.identity);
		DroppedItem.GetComponent<Item>().Info = ItemInfo;
		Items.Add(DroppedItem);
		return true;
	}
	/// <summary>
	/// Returns false if no item at position, returns true if found
	/// </summary>
	public bool HasItemAtPosition(Vector3 Position)
	{
		foreach (GameObject Item in Items)
		{
			if (Item.GetComponent<Item>().transform.position == Position)
			{
				return true;
			}
		}
		return false;
	}
	/// <summary>
	/// Returns null if no item at position, returns Item if found
	/// </summary>
	public Item GetItemAtPosition(Vector3 Position)
	{
		foreach (GameObject Item in Items)
		{
			Item ItemAtPosition = Item.GetComponent<Item>();
			if (ItemAtPosition.transform.position == Position)
			{
				return ItemAtPosition;
			}
		}
		return null;
	}
	/// <summary>
	/// Removes item when picked up or when new level starts
	/// </summary>
	private void DestroyItemAtPosition(Vector3 Position)
	{
		for (int i = 0; i < Items.Count; i++)
		{
			Item ItemAtPosition = Items[i].GetComponent<Item>();
			if (ItemAtPosition.transform.position == Position)
			{
				Items.RemoveAt(i);
				Destroy(ItemAtPosition.gameObject);
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
	public bool HasEnemyAtPosition(Vector3 Position)
	{
		foreach (GameObject Enemy in Enemies)
		{
			if (Enemy.GetComponent<Enemy>().transform.position == Position)
			{
				return true;
			}
		}
		return false;
	}
	private void HideLevelLoadScreen()
	{
		LevelImage.SetActive(false);
		LevelText.gameObject.SetActive(false);
		DayText.gameObject.SetActive(false);
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
		LevelText.text = day == 1 ? "AFTER 1 DAY" : "AFTER " + ((level / 5) + 1) + " DAYS";
		LevelImage.SetActive(true);
		enabled = false;
	}
	// Update is called once per frame
	void Update()
	{
		if (doingSetup
			|| !Player.finishedInit
			|| Player.isInMovement)
		{
			return;
		}
		
		DrawTileAreaIfNeeded();
		
		if (EndTurnButton.interactable == false
			&& playersTurn)
		{
			EndTurnButton.interactable = true;
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
			Enemies[indexOfMovingEnemy].GetComponent<Enemy>().CalculatePathAndStartMovement(Player.transform.position);
			enemiesInMovement = true;
			return;
		}
		// Enemy movement
		if (enemiesInMovement
			&& !Enemies[indexOfMovingEnemy].GetComponent<Enemy>().isInMovement)
		{
			if (indexOfMovingEnemy < Enemies.Count - 1)
			{
				indexOfMovingEnemy++;
				Enemies[indexOfMovingEnemy].GetComponent<Enemy>().CalculatePathAndStartMovement(Player.transform.position);
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
		TurnTimer.ResetTimer();
		playersTurn = true;
		EndTurnButton.interactable = true;
		needToDrawTileAreas = true;
		Tiledot.gameObject.SetActive(true);
		DrawTileAreaIfNeeded();
		DrawTargetsAndTracers();
	}
	/// <summary>
	/// Called by EndTurnButton, redraws tile areas, resets timer, resets energy
	/// </summary>
	public void OnEndTurnPress()
	{
		if (Player.isInMovement)
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
	/// Resets every enemy's energy when Player turn ends
	/// </summary>
	private void UpdateEnemyEnergy()
	{
		foreach (GameObject Enemy in Enemies)
		{
			Enemy.GetComponent<Enemy>().RestoreEnergy();
		}
	}
	/// <summary>
	/// Acts based on what mouse button is clicked
	/// </summary>
	private void ClickTarget()
	{
		BoundsInt CellBounds = TilemapGround.cellBounds;
		Vector3 WorldPoint = MainCamera.ScreenToWorldPoint(Input.mousePosition);
		Vector3Int TilePoint = TilemapGround.WorldToCell(WorldPoint);
		Vector3 ShiftedClickPoint = new(TilePoint.x + 0.5f, TilePoint.y + 0.5f, 0);
		// If RMB clicks
		if (Input.GetMouseButtonDown(1))
		{
			// This should be for the Player inspecting something
		}
		// If LMB clicks within grid, begin Player movement system
		else if (Input.GetMouseButtonDown(0)
			&& !Player.isInMovement
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
			bool isInRangedWeaponRange = Player.GetWeaponRange() > 0 && IsInRangeForRangedWeapon(ShiftedClickPoint);
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
			Player.ProcessHoverForInventory(WorldPoint);
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
			if (Player.AddItem(ItemAtPosition.Info))
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
			Player.isInMovement = true;
			needToDrawTileAreas = true;
			Player.CalculatePathAndStartMovement(WorldPoint);
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
			&& Player.DamagePoints > 0)
		{
			HandleDamageToEnemy(enemyIndex);
			Player.DecreaseEnergy(1);
			Player.SetAnimation("playerAttack");
			Player.DecreaseWeaponDurability();
			needToDrawTileAreas = true;
			TurnTimer.StartTimer();
			if (isInRangedWeaponRange
				&& Player.SelectedItem?.currentUses == 0)
			{
				ClearTargetsAndTracers();
			}
		}
	}
	/// <summary>
	/// Called by ClickTarget(), returns true if position is within area
	/// </summary>
	private bool IsWithinCellBounds(Vector3Int TilePoint, BoundsInt CellBounds)
	{
		return TilePoint.x >= CellBounds.min.x
			&& TilePoint.x < CellBounds.max.x
			&& TilePoint.y >= CellBounds.min.y
			&& TilePoint.y < CellBounds.max.y;
	}
	/// <summary>
	/// Called when an enemy takes damage, assumes player.DamagePoints > 0
	/// </summary>
	private void HandleDamageToEnemy(int index)
	{
		GameObject Enemy = Enemies[index];
		Enemy DamagedEnemy = Enemy.GetComponent<Enemy>();
		DamagedEnemy.DamageEnemy(Player.DamagePoints);
		if (DamagedEnemy.Info.currentHealth <= 0)
		{
			Enemies.RemoveAt(index);
			Destroy(Enemy);
			DrawTargetsAndTracers();
		}
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
	/// Called when the turn timer ends to stop Player from acting
	/// </summary>
	public void OnTurnTimerEnd()
	{
		Tiledot.gameObject.SetActive(false);
		Player.DecreaseEnergy(Player.MaxEnergy);
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
	/// Returns -1 if no enemy is present at selected position
	/// or index of enemy if enemy is present
	/// </summary>
	private int GetEnemyIndexAtPosition(Vector3Int Position)
	{
		Vector3 TargetPosition = Position + new Vector3(0.5f, 0.5f, 0);
		return Enemies.FindIndex(Enemy => Enemy.transform.position == TargetPosition);
	}
	/// <summary>
	/// Checks if tile is within range based on Player energy
	/// </summary>
	private bool IsInMovementRange(Vector3Int Position)
	{
		return TileAreasToDraw?.ContainsKey(Position) == true;
	}
	/// <summary>
	/// Returns true if an enemy or object is adjacent to Player
	/// </summary>
	private bool IsInMeleeRange(Vector3 ObjectPosition)
	{
		return Vector3.Distance(Player.transform.position, ObjectPosition) <= 1.0f;
	}
	/// <summary>
	/// Return true if an object is within range of ranged weapon
	/// </summary>
	private bool IsInRangeForRangedWeapon(Vector3 ObjectPosition)
	{
		return TargetPositions.Contains(ObjectPosition);
	}
	/// <summary>
	/// Retrurns true if tilemapWalls has tile at input position
	/// </summary>
	public bool TilemapWallsHasTile(Vector3Int Position) 
	{
		return TilemapWalls.HasTile(Position);
	}
	public Tilemap GetTilemapWalls() 
	{
		return TilemapWalls;
	}
	public Tilemap GetTilemapGround() 
	{
		return TilemapGround;
	}
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
		if (!Player.isInMovement
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
			TargetPositions.Clear();
			foreach (GameObject Enemy in Enemies)
			{
				Enemy ChosenEnemy = Enemy.GetComponent<Enemy>();
				Vector3 EnemyPoint = ChosenEnemy.transform.position;
				bool canTargetEnemy = IsInLineOfSight(Player.transform.position, EnemyPoint, weaponRange);
				if (TracerPath.Count > 0)
				{
					foreach (Vector3 tracerPosition in TracerPath)
					{
						Vector3 ShiftedDistance = new(tracerPosition.x + 0.0f, tracerPosition.y + 0.0f, EnemyPoint.z);
						GameObject Tracer = Instantiate(TracerTemplate, ShiftedDistance, Quaternion.identity);
						Tracers.Add(Tracer);
					}
				}
				if (canTargetEnemy)
				{
					Vector3 TargetPosition = ChosenEnemy.transform.position;
					Vector3 ShiftedDistance = new(TargetPosition.x + 0.0f, TargetPosition.y + 0.0f, EnemyPoint.z);
					GameObject Target = Instantiate(TargetTemplate, ShiftedDistance, Quaternion.identity);
					Targets.Add(Target);
					TargetPositions.Add(ShiftedDistance);
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
}