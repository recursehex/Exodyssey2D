using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	
	[Header("Core References")]
	[SerializeField] private Camera MainCamera;
	[SerializeField] private Player Player;
	[SerializeField] private bool doingSetup;
	
	[Header("Managers")]
	[SerializeField] public EnemyManager EnemyManager;
	[SerializeField] public ItemManager ItemManager;
	[SerializeField] private VehicleManager VehicleManager;
	[SerializeField] public TileManager TileManager;
	[SerializeField] private TurnManager TurnManager;
	[SerializeField] public LevelManager LevelManager;
	[SerializeField] private InputManager InputManager;
	
	[Header("Prefab Templates")]
	[SerializeField] private GameObject[] EnemyTemplates;
	[SerializeField] private GameObject[] ItemTemplates;
	[SerializeField] private GameObject[] VehicleTemplates;
	
	[Header("UI Elements")]
	[SerializeField] private GameObject TileDot;
	[SerializeField] private GameObject TileArea;
	[SerializeField] private GameObject TargetTemplate;
	[SerializeField] private GameObject TracerTemplate;
	[SerializeField] private TurnTimer TurnTimer;
	[SerializeField] private Button EndTurnButton;
	[SerializeField] private Text DayText;
	[SerializeField] private Text LevelText;
	[SerializeField] private GameObject LevelImage;
	
	[Header("Tilemaps")]
	[SerializeField] private Tilemap TilemapGround;
	[SerializeField] private Tilemap TilemapWalls;
	[SerializeField] private Tilemap TilemapExit;
	[SerializeField] private Tile[] GroundTiles;
	[SerializeField] private Tile[] WallTiles;
	
	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		DontDestroyOnLoad(gameObject);
		InitializeManagers();
		InitGame();
	}
	private void InitializeManagers()
	{
		// Instantiate managers
		EnemyManager 	= gameObject.AddComponent<EnemyManager>();
		ItemManager 	= gameObject.AddComponent<ItemManager>();
		VehicleManager 	= gameObject.AddComponent<VehicleManager>();
		TileManager 	= gameObject.AddComponent<TileManager>();
		TurnManager 	= gameObject.AddComponent<TurnManager>();
		LevelManager 	= gameObject.AddComponent<LevelManager>();
		InputManager 	= gameObject.AddComponent<InputManager>();
		// Initialize managers
		EnemyManager	.Initialize(TilemapGround, TilemapWalls, EnemyTemplates);
		ItemManager		.Initialize(ItemTemplates);
		VehicleManager	.Initialize(TilemapGround, TilemapWalls, VehicleTemplates);
		TileManager		.Initialize(TileDot, TileArea, TargetTemplate, TracerTemplate);
		TurnManager		.Initialize(TurnTimer, EndTurnButton);
		LevelManager	.Initialize(TilemapGround, TilemapWalls, TilemapExit, GroundTiles, WallTiles, DayText, LevelText, LevelImage);
		// Subscribe to events
		TurnManager.OnPlayerTurnEnded 	+= OnPlayerTurnEnded;
		TurnManager.OnEnemyTurnEnded 	+= OnEnemyTurnEnded;
		LevelManager.OnLevelInitialized += OnLevelInitialized;
		InputManager.OnPlayerClick 		+= HandlePlayerClick;
		InputManager.OnPlayerHover 		+= HandlePlayerHover;
	}
	private void Start()
	{
		MainCamera = Camera.main;
		InputManager.Initialize(MainCamera, TilemapGround, Player);
		SoundManager.Instance.PlayMusic();
	}
	void Update()
	{
		// Check if game is still setting up or Player is in movement
		if (doingSetup
			|| !Player.FinishedInit
			|| Player.IsInMovement
			|| Player.Vehicle != null && Player.Vehicle.IsInMovement)
		{
			return;
		}
		MovePlayerToVehicle();
		HandlePlayerExitTile();
		UpdateTileAreas();
		if (TurnManager.EndTurnButton.interactable == false
			&& TurnManager.IsPlayersTurn)
		{
			TurnManager.SetEndTurnButtonInteractable(true);
		}
		InputManager.ProcessInput();
		if (!TurnManager.IsPlayersTurn)
		{
			EnemyManager.ProcessEnemyMovement(() => TurnManager.EndEnemyTurn());
		}
	}
	/// <summary>
	/// Resets grid state after Player enters new level
	/// </summary>
	private void ResetForNextLevel()
	{
		LevelManager.PrepareNextLevel();
		TurnManager.TurnTimer.timerIsRunning = false;
		TurnManager.TurnTimer.ResetTimer();
		ItemManager.DestroyAllItems();
		EnemyManager.DestroyAllEnemies();
		VehicleManager.DestroyAllVehicles(Player.Vehicle);
		Player.transform.position = new(-3.5f, 0.5f, 0f);
		if (Player.IsInVehicle)
		{
			Player.Vehicle.transform.position = Player.transform.position;
			Player.Vehicle.DecreaseChargeBy(1);
		}
		Player.RestoreEnergy();
		InitGame();
	}
	/// <summary>
	/// Begins grid state generation
	/// </summary>
	void InitGame()
	{
		doingSetup = true;
		LevelManager.InitializeLevel();
	}
	/// <summary>
	/// Generates enemies, items, and vehicles after level is initialized
	/// </summary>
	private void OnLevelInitialized()
	{
		EnemyManager.GenerateEnemies();
		ItemManager.GenerateItems();
		VehicleManager.GenerateVehicles();
		Invoke(nameof(OnLevelLoadComplete), 1.5f);
	}
	/// <summary>
	/// Sets EndTurnButton interactable and updates targets and tracers after level load is complete
	/// </summary>
	private void OnLevelLoadComplete()
	{
		TurnManager.SetEndTurnButtonInteractable(true);
		doingSetup = false;
		// Update targets and tracers if player has ranged weapon
		if (!Player.IsInVehicle)
		{
			UpdateTargetsAndTracers();
		}
	}
	/// <summary>
	/// Called when Player dies
	/// </summary>
	public void GameOver()
	{
		LevelManager.ShowGameOver();
		enabled = false;
	}
	// Public methods for spawning entities (called by WeightedRarityGeneration)
	public void SpawnItem(int index, Vector3 Position) 		=> ItemManager.SpawnItem(index, Position);
	public void SpawnEnemy(int index, Vector3 Position) 	=> EnemyManager.SpawnEnemy(index, Position);
	public void SpawnVehicle(int index, Vector3 Position) 	=> VehicleManager.SpawnVehicle(index, Position);
	// Public accessor methods
	public bool HasItemAtPosition(Vector3 Position) 		=> ItemManager.HasItemAtPosition(Position);
	public Item GetItemAtPosition(Vector3 Position) 		=> ItemManager.GetItemAtPosition(Position);
	public void RemoveItemAtPosition(Item Item) 			=> ItemManager.RemoveItemAtPosition(Item);
	public bool HasEnemyAtPosition(Vector3 Position) 		=> EnemyManager.HasEnemyAtPosition(Position);
	public bool HasVehicleAtPosition(Vector3 Position) 		=> VehicleManager.HasVehicleAtPosition(Position);
	public bool HasWallAtPosition(Vector3Int Position) 		=> LevelManager.HasWallAtPosition(Position);
	public void DestroyVehicle(Vehicle Vehicle) 			=> VehicleManager.DestroyVehicle(Vehicle);
	private void MovePlayerToVehicle()
	{
		if (Player.IsInVehicle && !Player.Vehicle.IsInMovement)
		{
			Player.transform.position = Player.Vehicle.transform.position;
			Player.IsInMovement = false;
			if (TurnManager.IsPlayersTurn)
			{
				TurnManager.SetEndTurnButtonInteractable(true);
			}
		}
	}
	public void StopTurnTimer()
	{
		TurnManager.StopTurnTimer();
	}
	public void OnEndTurnPress()
	{
		if (Player.IsInMovement)
		{
			return;
		}
		TurnManager.OnEndTurnPress();
	}
	/// <summary>
	/// Prepares Enemy turn after Player turn ends
	/// </summary>
	private void OnPlayerTurnEnded()
	{
		Player.RestoreEnergy();
		if (EnemyManager.Enemies.Count == 0)
		{
			TileManager.TileDot.SetActive(true);
		}
		EnemyManager.NeedToStartEnemyMovement = EnemyManager.Enemies.Count > 0;
	}
	/// <summary>
	/// Clears tile areas, targets, and tracers after turn timer ends
	/// </summary>
	public void OnTurnTimerEnd()
	{
		TileManager.TileDot.SetActive(false);
		Player.SetEnergyToZero();
		TileManager.ClearTileAreas();
		TileManager.ClearTargetsAndTracers();
		TileDot.SetActive(false);
		TurnManager.OnTurnTimerEnd();
	}
	/// <summary>
	/// Prepares Player turn after Enemy turn ends
	/// </summary>
	private void OnEnemyTurnEnded()
	{
		TileManager.TileDot.SetActive(true);
		if (!Player.IsInVehicle)
			UpdateTargetsAndTracers();
	}
	/// <summary>
	/// Checks if Player is on exit tile to reset for next level
	/// </summary>
	private void HandlePlayerExitTile()
	{
		if (Player.IsInMovement)
		{
			return;
		}
		if (LevelManager.HasExitTileAtPosition(Vector3Int.FloorToInt(Player.transform.position)))
		{
			TileManager.TileDot.SetActive(false);
			ResetForNextLevel();
		}
	}
	/// <summary>
	/// Handles Player click on a tile
	/// </summary>
	/// <param name="WorldPoint"></param>
	/// <param name="TilePoint"></param>
	/// <param name="ShiftedClickPoint"></param>
	private void HandlePlayerClick(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		if (!Player.IsInMovement
			&& TurnManager.IsPlayersTurn
			&& !LevelManager.HasWallAtPosition(TilePoint))
		{
			if (PlayerIsInVehicle(WorldPoint, TilePoint, ShiftedClickPoint)) return;
			if (TryAddItem(ShiftedClickPoint)) return;
			if (!Player.HasEnergy()) return;
			if (TryUseItemOnPlayer(ShiftedClickPoint)) return;
			if (TryUseItemOnVehicle(TilePoint)) return;
			if (TryEnterVehicle(TilePoint)) return;
			if (TryPlayerMovement(WorldPoint, TilePoint, ShiftedClickPoint)) return;
			TryPlayerAttack(TilePoint, ShiftedClickPoint);
		}
	}
	/// <summary>
	/// Handles TileDot hover over a tile
	/// </summary>
	/// <param name="WorldPoint"></param>
	/// <param name="TilePoint"></param>
	/// <param name="ShiftedClickPoint"></param>
	private void HandlePlayerHover(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		if (TileManager.IsInMovementRange(TilePoint))
		{
			TileManager.TileDot.SetActive(true);
			TileManager.TileDot.transform.position = ShiftedClickPoint;
		}
	}
	/// <summary>
	/// Checks if Player is in a vehicle at specified world point
	/// </summary>
	/// <param name="WorldPoint"></param>
	/// <param name="TilePoint"></param>
	/// <param name="ShiftedClickPoint"></param>
	/// <returns></returns>
	private bool PlayerIsInVehicle(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		// Return false if Player is not in a vehicle
		if (!Player.IsInVehicle)
		{
			return false;
		}
		// If Player's vehicle is at clicked position, switch ignition
		if (Player.Vehicle.transform.position == ShiftedClickPoint)
		{
			Player.Vehicle.SwitchIgnition();
			TileManager.ClearTileAreas();
			TileManager.ClearTargetsAndTracers();
		}
		// If Player's vehicle is off and clicked tile is in movement range, try to exit vehicle
		else if (!Player.Vehicle.Info.IsOn && TileManager.IsInMovementRange(TilePoint))
		{
			TryExitVehicle(WorldPoint, TilePoint, ShiftedClickPoint);
		}
		// If Player's vehicle is on, has fuel, and clicked tile is in movement range, try to move vehicle
		else if (Player.Vehicle.Info.IsOn && Player.Vehicle.HasCharge())
		{
			TryVehicleMovement(WorldPoint, TilePoint, ShiftedClickPoint);
		}
		return true;
	}
	/// <summary>
	/// Tries to exit Player's vehicle at specified world point
	/// </summary>
	/// <param name="WorldPoint"></param>
	/// <param name="TilePoint"></param>
	/// <param name="ShiftedClickPoint"></param>
	private void TryExitVehicle(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		// Check if Player's vehicle can exit to clicked tile
		bool isInMovementRange = TileManager.IsInMovementRange(TilePoint);
		int enemyIndex = EnemyManager.GetEnemyIndexAtPosition(TilePoint);
		// Return if not in movement range, enemy present, clicked on current position, or Player has no energy
		if (!isInMovementRange
			|| enemyIndex != -1
			|| ShiftedClickPoint == Player.transform.position
			|| !Player.HasEnergy())
		{
			return;
		}
		// Player exits vehicle
		TurnManager.SetEndTurnButtonInteractable(false);
		Player.IsInMovement = true;
		Player.ExitVehicle();
		Player.ComputePathAndStartMovement(WorldPoint);
		TileManager.ClearTileAreas();
		UpdateTargetsAndTracers();
		TurnManager.TurnTimer.StartTimer();
	}
	/// <summary>
	/// Tries to move Player's vehicle to specified world point
	/// </summary>
	/// <param name="WorldPoint"></param>
	/// <param name="TilePoint"></param>
	/// <param name="ShiftedClickPoint"></param>
	private void TryVehicleMovement(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		// Check if Player's vehicle can move to clicked tile
		bool isInMovementRange = TileManager.IsInMovementRange(TilePoint);
		int enemyIndex = EnemyManager.GetEnemyIndexAtPosition(TilePoint);
		// Return if not in movement range, enemy present, or clicked on current position
		if (!isInMovementRange
			|| enemyIndex != -1
			|| ShiftedClickPoint == Player.transform.position)
		{
			return;
		}
		// Start Player's vehicle movement
		TurnManager.SetEndTurnButtonInteractable(false);
		Player.IsInMovement = true;
		Player.VehicleMovement(WorldPoint);
		TileManager.ClearTileAreas();
		TurnManager.TurnTimer.StartTimer();
	}
	/// <summary>
	/// Tries to add an item to Player's inventory at specified shifted click point
	/// </summary>
	/// <param name="ShiftedClickPoint"></param>
	/// <returns></returns>
	private bool TryAddItem(Vector3 ShiftedClickPoint)
	{
		// Return false if Player is not adjacent to clicked position or if click fails
		if (ShiftedClickPoint != Player.transform.position
			|| GetItemAtPosition(ShiftedClickPoint) is not Item Item)
		{
			return false;
		}
		// Return false if Player's inventory is full
		if (!Player.TryAddItem(Item))
		{
			return false;
		}
		ItemManager.DestroyItemAtPosition(ShiftedClickPoint);
		return true;
	}
	/// <summary>
	/// Tries use an item on Player at specified shifted click point
	/// </summary>
	/// <param name="ShiftedClickPoint"></param>
	/// <returns></returns>
    private bool TryUseItemOnPlayer(Vector3 ShiftedClickPoint)
	{
		// Return false if Player is not adjacent to clicked position or if click fails
		if (ShiftedClickPoint != Player.transform.position
			|| !Player.ClickOnToUseItem())
		{
			return false;
		}
		TurnManager.TurnTimer.StartTimer();
		return true;
	}
	/// <summary>
	/// Tries use an item on a vehicle at specified tile point
	/// </summary>
	/// <param name="TilePoint"></param>
	/// <returns></returns>
	private bool TryUseItemOnVehicle(Vector3Int TilePoint)
	{
		// Get vehicle index at position
		int vehicleIndex = VehicleManager.GetVehicleIndexAtPosition(TilePoint);
		// Return false if no vehicle found or Player has no selected item
		if (vehicleIndex == -1)
		{
			return false;
		}
		Vehicle Vehicle = VehicleManager.Vehicles[vehicleIndex];
		// Return false if player is not adjacent to vehicle or if click fails
		if (!IsPlayerAdjacentTo(Vehicle.transform.position)
			|| !Player.ClickOnVehicleToUseItem())
		{
			return false;
		}
		TurnManager.TurnTimer.StartTimer();
		return true;
	}
	private bool TryEnterVehicle(Vector3Int TilePoint)
    {
		// Get vehicle index at position
		int vehicleIndex = VehicleManager.GetVehicleIndexAtPosition(TilePoint);
		// Return false if no vehicle found
		if (vehicleIndex == -1)
		{
			return false;
		}
		// Return false if player is not adjacent to vehicle
		if (!IsPlayerAdjacentTo(VehicleManager.Vehicles[vehicleIndex].transform.position))
		{
			return false;
		}
		Player.EnterVehicle(VehicleManager.Vehicles[vehicleIndex]);
        TurnManager.TurnTimer.StartTimer();
		TileManager.ClearTargetsAndTracers();
		return true;
    }
	/// <summary>
	/// Tries to move Player to clicked tile point
	/// </summary>
	/// <param name="WorldPoint"></param>
	/// <param name="TilePoint"></param>
	/// <param name="ShiftedClickPoint"></param>
	/// <returns></returns>
	private bool TryPlayerMovement(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		// Check if player can move to clicked tile
		bool isInMovementRange = TileManager.IsInMovementRange(TilePoint);
		int enemyIndex = EnemyManager.GetEnemyIndexAtPosition(TilePoint);
		// Return false if not in movement range, enemy present, or clicked on current position
		if (!isInMovementRange
			|| enemyIndex != -1
			|| ShiftedClickPoint == Player.transform.position)
		{
			return false;
		}
		// Start Player movement
		TurnManager.SetEndTurnButtonInteractable(false);
		Player.IsInMovement = true;
		Player.ComputePathAndStartMovement(WorldPoint);
		TileManager.ClearTileAreas();
		TurnManager.TurnTimer.StartTimer();
		return true;
	}
	/// <summary>
	/// Tries to attack an enemy at specified tile point
	/// </summary>
	/// <param name="TilePoint"></param>
	/// <param name="ShiftedClickPoint"></param>
    private void TryPlayerAttack(Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		// Check if player can attack an enemy at clicked tile
		int enemyIndex = EnemyManager.GetEnemyIndexAtPosition(TilePoint);
		bool isInMeleeRange = IsPlayerAdjacentTo(ShiftedClickPoint);
		bool isInRangedWeaponRange = Player.GetWeaponRange() > 0 && TileManager.IsInRangedWeaponRange(ShiftedClickPoint);
		// Return if no enemy found, not in melee or ranged weapon range, or selected item is not a weapon
		if (enemyIndex == -1
			|| !isInMeleeRange
			&& !isInRangedWeaponRange
			|| Player.SelectedItemInfo?.Type is not ItemInfo.Types.Weapon)
		{
			return;
		}
		// Drop a rock after it is thrown
		if (Player.SelectedItemInfo.Tag == ItemInfo.Tags.Rock)
		{
			SpawnItem((int)Player.SelectedItemInfo.Tag, ShiftedClickPoint);
		}
		// Handle damage to enemy
		EnemyManager.HandleDamageToEnemy(enemyIndex, Player.DamagePoints, Player.SelectedItemInfo.IsStunning);
		Player.AttackEnemy();
		TurnManager.TurnTimer.StartTimer();
		TileManager.TileDot.SetActive(false);
		UpdateTargetsAndTracers();
	}
	/// <summary>
	/// Checks if Player is adjacent to a specified position
	/// </summary>
	/// <param name="Position"></param>
	/// <returns></returns>
	private bool IsPlayerAdjacentTo(Vector3 Position)
	{
		return Vector3.Distance(Player.transform.position, Position) <= 1.0f;
	}
	/// <summary>
	/// Updates tile areas based on Player's movement and energy
	/// </summary>
	private void UpdateTileAreas()
	{
		if (!Player.IsInMovement
			&& TurnManager.IsPlayersTurn
			&& Player.HasEnergy())
		{
			Dictionary<Vector3Int, Node> AreasToDraw = null;

			if (Player.IsInVehicle
				&& Player.Vehicle.Info.IsOn
				&& Player.Vehicle.HasCharge())
			{
				AreasToDraw = Player.Vehicle.CalculateArea();
			}
			else if (!Player.IsInVehicle
					|| (Player.IsInVehicle && !Player.Vehicle.Info.IsOn))
			{
				AreasToDraw = Player.CalculateArea();
			}

			if (AreasToDraw != null)
			{
				TileManager.DrawTileAreas(AreasToDraw);
			}
		}
	}
	/// <summary>
	/// Updates targets and tracers based on Player's weapon range and energy
	/// </summary>
	public void UpdateTargetsAndTracers()
	{
		int weaponRange = Player.GetWeaponRange();
		if (weaponRange > 0
			&& TurnManager.IsPlayersTurn
			&& Player.HasEnergy())
		{
			TileManager.DrawTargetsAndTracers(EnemyManager.Enemies, Player.transform.position, weaponRange, Player.SelectedItemInfo.IsStunning, TilemapWalls);
		}
	}
}