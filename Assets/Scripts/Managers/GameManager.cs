using System.Collections;
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
	public Vector3 PlayerStartPosition { get; } = new(-3.5f, 0.5f);
	[SerializeField] private float turnPhaseDelay = 0.25f;
	private Coroutine TileRevealRoutine;
	private Coroutine FireTurnRoutine;
	private Coroutine PlayerTurnDelayRoutine;
	private Coroutine ExitTransitionRoutine;
	[Header("Managers")]
	[SerializeField] private RegionManager RegionManager;
	private EnemyManager EnemyManager;
	private ItemManager ItemManager;
	private VehicleManager VehicleManager;
	private FireManager FireManager;
	private TileManager TileManager;
	private TurnManager TurnManager;
	private LevelManager LevelManager;
	private InputManager InputManager;
	private ChronoclasmManager ChronoclasmManager;
	private TilemapRevealAnimator TilemapRevealAnimator;
	private VisibilityManager VisibilityManager;
	private CursorController CursorController;
	[Header("Prefab Templates")]
	[SerializeField] private GameObject[] EnemyTemplates;
	[SerializeField] private GameObject[] ItemTemplates;
	[SerializeField] private GameObject[] VehicleTemplates;
	[SerializeField] private GameObject FireTemplate;
	[Header("UI Elements")]
	[SerializeField] private GameObject TileDot;
	[SerializeField] private GameObject TileArea;
	[SerializeField] private GameObject TargetTemplate;
	[SerializeField] private TurnTimer TurnTimer;
	[SerializeField] private Button EndTurnButton;
	[SerializeField] private Button NewGameButton;
	[SerializeField] private Text RegionText;
	[SerializeField] private Text DayText;
	[SerializeField] private Text LevelText;
	[SerializeField] private GameObject LevelImage;
	[Header("Tilemaps")]
	[SerializeField] private Tilemap TilemapGround;
	[SerializeField] private Tilemap TilemapWalls;
	[SerializeField] private Tilemap TilemapExit;
	[Header("Audio")]
	[SerializeField] private AudioClip GameOverClip;
	void Awake()
	{
		if (Instance == null)
			Instance = this;
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
		FireManager 	= gameObject.AddComponent<FireManager>();
		TileManager 	= gameObject.AddComponent<TileManager>();
		TurnManager 	= gameObject.AddComponent<TurnManager>();
		LevelManager 	= gameObject.AddComponent<LevelManager>();
		InputManager 	= gameObject.AddComponent<InputManager>();
		ChronoclasmManager = gameObject.AddComponent<ChronoclasmManager>();
		TilemapRevealAnimator = gameObject.AddComponent<TilemapRevealAnimator>();
		VisibilityManager = gameObject.AddComponent<VisibilityManager>();
		// Initialize managers
		RegionManager	.Initialize();
		EnemyManager	.Initialize(TilemapGround, TilemapWalls, EnemyTemplates);
		ItemManager		.Initialize(ItemTemplates);
		VehicleManager	.Initialize(TilemapGround, TilemapWalls, VehicleTemplates, Player);
		FireManager		.Initialize(TilemapGround, TilemapWalls, Player, EnemyManager, VehicleManager, FireTemplate);
		TileManager		.Initialize(TileDot, TileArea, TargetTemplate);
		TurnManager		.Initialize(TurnTimer, EndTurnButton);
		TilemapRevealAnimator.Initialize(TilemapGround, TilemapWalls);
		LevelManager	.Initialize(TilemapGround, TilemapWalls, TilemapExit, RegionManager, RegionText, DayText, LevelText, LevelImage, TilemapRevealAnimator);
		ChronoclasmManager.Initialize(this, TurnManager, TileManager, Player, TilemapGround);
		VisibilityManager.Initialize(this, TilemapGround, TilemapWalls, TilemapExit, Player, LevelManager, EnemyManager, ItemManager, VehicleManager, FireManager);
		// Subscribe to events
		TurnManager.OnPlayerTurnEnded 	+= OnPlayerTurnEnded;
		TurnManager.OnEnemyTurnEnded 	+= OnEnemyTurnEnded;
		LevelManager.OnLevelInitialized += OnLevelInitialized;
		LevelManager.OnLoadingScreenVisibilityChanged += HandleLoadingScreenVisibilityChanged;
		InputManager.OnPlayerClick 		+= HandlePlayerClick;
		InputManager.OnPlayerHover 		+= HandlePlayerHover;
		Player.OnMovementComplete 		+= OnPlayerMovementComplete;
		EnemyManager.OnEnemyKilled 		+= UpdateTileAreas;
	}
	private void Start()
	{
		MainCamera = Camera.main;
		CursorController = FindFirstObjectByType<CursorController>();
		InputManager.Initialize(MainCamera, TilemapGround, Player, CursorController);
		SoundManager.Instance.PlayMusic();
		NewGameButton.gameObject.SetActive(false);
	}
	void Update()
	{
		// Always allow inventory hover text to update
		if (Player.FinishedInit)
			Player.InventoryUI.ProcessHoverForInventory(MainCamera.ScreenToWorldPoint(CursorController.CursorScreenPosition));
		// Check if game is still setting up or Player is in movement
		if (doingSetup
			|| !Player.FinishedInit
			|| Player.IsInMovement
			|| Player.IsInVehicle && Player.Vehicle.IsInMovement)
			return;
		if (TurnManager.IsPlayersTurn)
			InputManager.ProcessInput();
		// Process enemy movement if it is not Player's turn
		if (!TurnManager.IsPlayersTurn && EnemyManager.IsProcessingEnemyMovement)
			EnemyManager.ProcessEnemyMovement(() => TurnManager.EndEnemyTurn());
	}
	private void OnDestroy()
	{
		TurnManager.OnPlayerTurnEnded 	-= OnPlayerTurnEnded;
		TurnManager.OnEnemyTurnEnded 	-= OnEnemyTurnEnded;
		LevelManager.OnLevelInitialized -= OnLevelInitialized;
		LevelManager.OnLoadingScreenVisibilityChanged -= HandleLoadingScreenVisibilityChanged;
		InputManager.OnPlayerClick 		-= HandlePlayerClick;
		InputManager.OnPlayerHover 		-= HandlePlayerHover;
		Player.OnMovementComplete 		-= OnPlayerMovementComplete;
		EnemyManager.OnEnemyKilled 		-= UpdateTileAreas;
	}
	/// <summary>
	/// Resets grid state after Player enters new level
	/// </summary>
	private void ResetForNextLevel()
	{
		LevelManager.PrepareNextLevel();
		ChronoclasmManager.HandleGridExit();
		TurnManager.TurnTimer.timerIsRunning = false;
		TurnManager.TurnTimer.ResetTimer();
		FireManager.DestroyAllFires();
		ItemManager.DestroyAllItems();
		EnemyManager.DestroyAllEnemies();
		VehicleManager.DestroyAllVehicles(Player.Vehicle);
		Player.transform.position = PlayerStartPosition;
		if (Player.IsInVehicle)
			Player.Vehicle.transform.position = Player.transform.position;
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
		FireManager.ResetForLevel();
		// Guarantee vehicle spawn on first level
		if (LevelManager.Level == 0)
			SpawnFirstLevelVehicle();
		else
			VehicleManager.GenerateVehicles();
		EnemyManager.GenerateEnemies();
		ItemManager.GenerateItems();
		TileManager.TileDot.SetActive(false);
		TileManager.ClearTileAreas();
		TileManager.ClearTargets();
		RefreshVisibility();
	}
	private IEnumerator RunTileRevealAndFinalize()
	{
		TurnManager.SetEndTurnButtonInteractable(false);
		TileManager.TileDot.SetActive(false);
		TileManager.ClearTileAreas();
		TileManager.ClearTargets();
		if (TilemapRevealAnimator != null && TilemapRevealAnimator.HasPreparedTiles)
			yield return TilemapRevealAnimator.PlayReveal();
		OnLevelLoadComplete();
		TileRevealRoutine = null;
	}
	private IEnumerator RunExitTransition()
	{
		doingSetup = true;
		TurnManager.StopTurnTimer();
		TurnManager.SetEndTurnButtonInteractable(false);
		TileManager.TileDot.SetActive(false);
		TileManager.ClearTileAreas();
		TileManager.ClearTargets();
		ClearAllLights();
		if (TilemapRevealAnimator != null)
		{
			Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
			TilemapRevealAnimator.PrepareTilesForCollapse(PlayerCell, TilemapExit);
			RegisterObjectsForTileCollapse();
			if (TilemapRevealAnimator.HasPreparedTiles)
				yield return TilemapRevealAnimator.PlayCollapse();
		}
		ResetForNextLevel();
		ExitTransitionRoutine = null;
	}
	/// <summary>
	/// Sets EndTurnButton interactable and updates targets after level load is complete
	/// </summary>
	private void OnLevelLoadComplete()
	{
		doingSetup = false;
		TurnManager.SetEndTurnButtonInteractable(true);
		RefreshVisibility();
		// Update targets if player has ranged weapon
		if (!Player.IsInVehicle)
			UpdateTargets();
		// Draw tile areas at start of game
		UpdateTileAreas();
		ChronoclasmManager.OnPlayerTurnStart();
	}
	/// <summary>
	/// Handles changes to loading screen visibility
	/// </summary>
	private void HandleLoadingScreenVisibilityChanged(bool isVisible)
	{
		TurnManager.SetEndTurnButtonLock(isVisible);
		if (isVisible)
		{
			TurnManager.SetEndTurnButtonInteractable(false);
			if (TileRevealRoutine != null)
			{
				StopCoroutine(TileRevealRoutine);
				TileRevealRoutine = null;
			}
			return;
		}
		if (TileRevealRoutine != null)
			StopCoroutine(TileRevealRoutine);
		TileRevealRoutine = StartCoroutine(RunTileRevealAndFinalize());
	}
	/// <summary>
	/// Called when Player dies, cleans up scene and show Game Over screen
	/// </summary>
	public void GameOver()
	{
		CleanupWorldEntities();
		ChronoclasmManager.ResetForNewRun();
		SoundManager.Instance.PlayGameOver(GameOverClip);
		LevelManager.ShowGameOver();
		NewGameButton.gameObject.SetActive(true);
		TurnManager.SetEndTurnButtonInteractable(false);
		enabled = false;
	}
	/// <summary>
	/// Called by NewGameButton to start a new game after game over
	/// </summary>
	public void StartNewGame()
	{
		CleanupWorldEntities();
		enabled = true;
		StopAllCoroutines();
		NewGameButton.gameObject.SetActive(false);
		RegionManager.ResetRegionProgress();
		LevelManager.ResetLevelProgress();
		TurnManager.ResetTurnState();
		ChronoclasmManager.ResetForNewRun();
		Player.ResetForNewGame(PlayerStartPosition);
		SoundManager.Instance.PlayMusic();
		InitGame();
	}
	/// <summary>
	/// Cleans up all spawned entities/markers except Player
	/// </summary>
	private void CleanupWorldEntities()
	{
		TurnManager.StopTurnTimer();
		ItemManager.DestroyAllItems();
		EnemyManager.DestroyAllEnemies();
		if (Player.IsInVehicle)
			Player.ExitVehicle();
		VehicleManager.DestroyAllVehicles();
		FireManager.DestroyAllFires();
		TileManager.DestroyAllMarkers();
		TileManager.TileDot.SetActive(false);
		RefreshVisibility();
	}
	// Public methods for spawning entities, called by WeightedRarityGeneration
	public Item SpawnItem(int index, Vector3 Position)
	{
		Item Item = ItemManager.SpawnItem(index, Position);
		RegisterObjectForTileReveal(Position, Item.transform);
		return Item;
	}
	public Item SpawnItem(ItemInfo Info, Vector3 Position)
	{
		Item Item = ItemManager.SpawnItem(Info, Position);
		RegisterObjectForTileReveal(Position, Item.transform);
		return Item;
	}
	public Enemy SpawnEnemy(int index, Vector3 Position)
	{
		Enemy Enemy = EnemyManager.SpawnEnemy(index, Position);
		RegisterObjectForTileReveal(Position, Enemy.transform);
		return Enemy;
	}
	public Vehicle SpawnVehicle(int index, Vector3 Position, int startingFuel = -1)
	{
		Vehicle Vehicle = VehicleManager.SpawnVehicle(index, Position, startingFuel);
		RegisterObjectForTileReveal(Position, Vehicle.transform);
		return Vehicle;
	}
	private void SpawnFirstLevelVehicle()
	{
		int startingFuel = 4;
		if (!TryGetRandomVehicleSpawnPosition(out Vector3 Position))
		{
			Debug.LogWarning("Failed to find spawn position for first level rover.");
			return;
		}
		SpawnVehicle((int)VehicleInfo.Tags.Rover, Position, startingFuel);
	}
	private bool TryGetRandomVehicleSpawnPosition(out Vector3 Position)
	{
		List<Vector3Int> CandidateCells = new();
		int middleRowMinY = GameConfig.Grid.MinY + GameConfig.Grid.QuadrantSize;
		int middleRowMaxY = GameConfig.Grid.MaxY - GameConfig.Grid.QuadrantSize;
		for (int x = GameConfig.Grid.MinX; x <= GameConfig.Grid.MaxX; x++)
		{
			for (int y = middleRowMinY; y <= middleRowMaxY; y++)
			{
				Vector3Int Cell = new(x, y);
				if (HasWallAtPosition(Cell)
					|| HasFireAtPosition(Cell)
					|| HasExitTileAtPosition(Cell)
					|| (x <= GameConfig.Grid.SafeZoneMaxX
						&& y <= GameConfig.Grid.SafeZoneMaxY
						&& y >= GameConfig.Grid.SafeZoneMinY))
					continue;
				Vector3 ShiftedPosition = Cell + new Vector3(0.5f, 0.5f);
				if (HasItemAtPosition(ShiftedPosition)
					|| HasEnemyAtPosition(ShiftedPosition)
					|| HasVehicleAtPosition(ShiftedPosition))
					continue;
				CandidateCells.Add(Cell);
			}
		}
		if (CandidateCells.Count == 0)
		{
			Position = default;
			return false;
		}
		Vector3Int SelectedCell = CandidateCells[Random.Range(0, CandidateCells.Count)];
		Position = SelectedCell + new Vector3(0.5f, 0.5f);
		return true;
	}
	// Public accessor methods
	public bool IsDoingSetup 								=> doingSetup;
	public bool IsChronoclasmReady 							=> ChronoclasmManager != null && ChronoclasmManager.IsChronoclasmReady;
	public bool HasChronoclasmSnapshot 						=> ChronoclasmManager != null && ChronoclasmManager.HasChronoclasmSnapshot;
	public int ChronoclasmGridsRemaining 					=> ChronoclasmManager.ChronoclasmGridsRemaining;
	public string ChronoclasmStatusLabel 					=> ChronoclasmManager.ChronoclasmStatusLabel;
	public string ChronoclasmFailureReason 					=> ChronoclasmManager.ChronoclasmFailureReason;
	public bool CanUndo 									=> ChronoclasmManager != null && ChronoclasmManager.CanUndo;
	public string UndoBlockReason 							=> ChronoclasmManager.UndoBlockReason;
	public void OnUndoButtonPress() 						=> ChronoclasmManager.TryUndoLastMove();
	public void ForceChronoclasmReady() 					=> ChronoclasmManager.ForceChronoclasmReady();
	public void ClearChronoclasmReadyOverride() 			=> ChronoclasmManager.ClearChronoclasmReadyOverride();
	public void OnPlayerActionPointSpent() 				=> ChronoclasmManager.MarkActionPointSpentThisTurn();
	public void ClearUndoHistory(string Reason) 			=> ChronoclasmManager.ClearUndoHistory(Reason);
	public void RecordUndoSnapshot(bool recordGroundItem = false) => ChronoclasmManager.RecordUndoSnapshot(recordGroundItem);
	public bool HasItemAtPosition(Vector3 Position) 		=> ItemManager.HasItemAtPosition(Position);
	public Item GetItemAtPosition(Vector3 Position) 		=> ItemManager.GetItemAtPosition(Position);
	public void RemoveItemAtPosition(Item Item) 			=> ItemManager.RemoveItemAtPosition(Item);
	public bool HasEnemyAtPosition(Vector3 Position) 		=> EnemyManager.HasEnemyAtPosition(Position);
	public Enemy GetEnemyAtPosition(Vector3 Position) 		=> EnemyManager.GetEnemyAtPosition(Position);
	public bool HasVehicleAtPosition(Vector3 Position) 		=> VehicleManager.HasVehicleAtPosition(Position);
	public Vehicle GetVehicleAtPosition(Vector3Int Position) => VehicleManager.GetVehicleAtPosition(Position);
	public bool HasWallAtPosition(Vector3Int Position) 		=> LevelManager.HasWallAtPosition(Position);
	public bool DamageVehicle(Vehicle Vehicle, int damage) 	=> VehicleManager.DamageVehicle(Vehicle, damage);
	public void DestroyVehicle(Vehicle Vehicle) 			=> VehicleManager.DestroyVehicle(Vehicle);
	public void ClearTileAreas() 							=> TileManager.ClearTileAreas();
	public void ClearTargets() 								=> TileManager.ClearTargets();
	public bool HasEnemies() 								=> EnemyManager.Enemies.Count > 0;
	public bool HasExitTileAtPosition(Vector3Int Position) 	=> LevelManager.HasExitTileAtPosition(Position);
	public bool HasFireAtPosition(Vector3Int Position) 		=> FireManager.HasFireAtCell(Position);
	public bool HasFireAtWorld(Vector3 Position) 			=> FireManager.HasFireAtWorld(Position);
	public bool TrySpawnFire(Vector3Int Position, bool isWildfire = false, bool allowBurnedCell = false) => FireManager.TrySpawnFire(Position, isWildfire, allowBurnedCell);
	public bool TryExtinguishFire(Vector3Int Position) 		=> FireManager.ExtinguishFire(Position);
	public int Level 										=> LevelManager.Level;
	public RegionManager GetRegionManager() 				=> RegionManager;
    public void StopTurnTimer() 							=> TurnManager.StopTurnTimer();
	public bool IsCellVisible(Vector3Int Cell) 				=> VisibilityManager == null || VisibilityManager.IsCellVisible(Cell);
	public void RefreshVisibility() 						=> VisibilityManager?.RefreshVisibility();
	public void ClearAllLights() 							=> VisibilityManager?.ClearAllLights();
	public void RegisterObjectForTileReveal(Vector3 WorldPosition, Transform ObjectTransform)
	{
		if (TilemapRevealAnimator == null
			|| !TilemapRevealAnimator.HasPreparedTiles
			|| ObjectTransform == null)
			return;
		Vector3Int Cell = TilemapGround.WorldToCell(WorldPosition);
		TilemapRevealAnimator.RegisterObjectAtCell(Cell, ObjectTransform);
	}
	private void RegisterObjectForTileCollapse(Transform ObjectTransform)
	{
		if (TilemapRevealAnimator == null
			|| !TilemapRevealAnimator.HasPreparedTiles
			|| ObjectTransform == null)
			return;
		Vector3Int Cell = TilemapGround.WorldToCell(ObjectTransform.position);
		TilemapRevealAnimator.RegisterObjectAtCellForCollapse(Cell, ObjectTransform);
	}
	private void RegisterObjectsForTileCollapse()
	{
		foreach (Enemy Enemy in EnemyManager.Enemies)
		{
			if (Enemy != null)
				RegisterObjectForTileCollapse(Enemy.transform);
		}
		foreach (Item Item in ItemManager.Items)
		{
			if (Item != null)
				RegisterObjectForTileCollapse(Item.transform);
		}
		foreach (Vehicle Vehicle in VehicleManager.Vehicles)
		{
			if (Vehicle != null)
				RegisterObjectForTileCollapse(Vehicle.transform);
		}
		foreach (Fire Fire in FireManager.Fires)
		{
			if (Fire != null)
				RegisterObjectForTileCollapse(Fire.transform);
		}
		if (Player != null)
			RegisterObjectForTileCollapse(Player.transform);
	}
	public void OnEndTurnPress()
	{
		if (Player.IsInMovement || doingSetup)
			return;
		TurnManager.OnEndTurnPress();
	}
	public void OnChronoclasmButtonPress() => ChronoclasmManager.TryUseChronoclasm();
	/// <summary>
	/// Prepares Enemy turn after Player turn ends
	/// </summary>
	private void OnPlayerTurnEnded()
	{
		Player.RestoreEnergy();
		// Hide TileDot during enemy turn
		TileManager.TileDot.SetActive(false);
		TurnManager.SetEndTurnButtonInteractable(false);
		if (FireTurnRoutine != null)
		{
			StopCoroutine(FireTurnRoutine);
			FireTurnRoutine = null;
		}
		if (EnemyManager.Enemies.Count == 0 && Player.HasEnergy)
		{
			TileManager.ClearTileAreas();
			TileManager.TileDot.SetActive(false);
		}
		RefreshVisibility();
		EnemyManager.NeedToStartEnemyMovement = false;
		FireTurnRoutine = StartCoroutine(RunEnemyTurnSequence());
	}
	/// <summary>
	/// Clears tile areas, targets, after turn timer ends
	/// </summary>
	public void OnTurnTimerEnd()
	{
		TileManager.TileDot.SetActive(false);
		Player.SetEnergyToZero();
		TileManager.ClearTileAreas();
		TileManager.ClearTargets();
		ChronoclasmManager.OnTurnTimerExpired();
		TurnManager.OnTurnTimerEnd();
	}
	/// <summary>
	/// Prepares Player turn after Enemy turn ends
	/// </summary>
	private void OnEnemyTurnEnded()
	{
		TurnManager.SetEndTurnButtonInteractable(false);
		ChronoclasmManager.OnPlayerTurnStart();
		RefreshVisibility();
		if (PlayerTurnDelayRoutine != null)
		{
			StopCoroutine(PlayerTurnDelayRoutine);
			PlayerTurnDelayRoutine = null;
		}
		PlayerTurnDelayRoutine = StartCoroutine(StartPlayerTurnAfterDelay());
	}
	private IEnumerator RunEnemyTurnSequence()
	{
		bool hadFires = FireManager.HasActiveFires();
		if (turnPhaseDelay > 0f)
			yield return new WaitForSecondsRealtime(turnPhaseDelay);
		VisibilityManager?.TickActiveFlaresOnRoundStart();
		FireManager.HandleTurnStart(false);
		RefreshVisibility();
		if (EnemyManager.Enemies.Count == 0)
		{
			TurnManager.EndEnemyTurn();
			FireTurnRoutine = null;
			yield break;
		}
		// Ensure a visible gap between fire resolution and enemy movement whenever fires were present
		bool shouldDelayEnemies = hadFires && turnPhaseDelay > 0f;
		if (shouldDelayEnemies)
			yield return new WaitForSecondsRealtime(turnPhaseDelay);
		if (EnemyManager.Enemies.Count == 0)
			TurnManager.EndEnemyTurn();
		else
			EnemyManager.NeedToStartEnemyMovement = true;
		FireTurnRoutine = null;
	}
	private IEnumerator StartPlayerTurnAfterDelay()
	{
		if (turnPhaseDelay > 0f)
			yield return new WaitForSecondsRealtime(turnPhaseDelay);
		FireManager.HandleTurnStart(true);
		RefreshVisibility();
		// Hide TileDot if player is in vehicle with no charge, otherwise show it
		bool hideForDepletedVehicle = Player.IsInVehicle
			&& Player.Vehicle.Info.IsOn
			&& !Player.Vehicle.HasCharge();
		TileManager.TileDot.SetActive(!hideForDepletedVehicle);
		if (!Player.IsInVehicle)
			UpdateTargets();
		// Draw tile areas at start of player's turn
		UpdateTileAreas();
		TurnManager.SetEndTurnButtonInteractable(true);
		PlayerTurnDelayRoutine = null;
	}
	/// <summary>
	/// Called when Player stops moving
	/// </summary>
	private void OnPlayerMovementComplete()
	{
		// Move Player to vehicle position if in vehicle
		if (Player.IsInVehicle && Player.Vehicle != null)
			Player.transform.position = Player.Vehicle.transform.position;
		RefreshVisibility();
		// Check if player is on exit tile
		HandlePlayerExitTile();
		if (TurnManager.IsPlayersTurn)
		{
			TurnManager.SetEndTurnButtonInteractable(true);
			UpdateTileAreas();
		}
	}
	/// <summary>
	/// Checks if Player is on exit tile to reset for next level
	/// </summary>
	private void HandlePlayerExitTile()
	{
		if (LevelManager.HasExitTileAtPosition(Vector3Int.FloorToInt(Player.transform.position)))
		{
			TileManager.TileDot.SetActive(false);
			if (ExitTransitionRoutine != null)
				return;
			// Return if Player's vehicle has insufficient charge to move to next grid
			if (Player.IsInVehicle && !Player.Vehicle.DecreaseChargeBy(Player.Vehicle.Info.Efficiency))
				return;
			ExitTransitionRoutine = StartCoroutine(RunExitTransition());
		}
	}
	/// <summary>
	/// Handles Player click on a tile
	/// </summary>
	private void HandlePlayerClick(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		if (Player.IsInMovement || !TurnManager.IsPlayersTurn)
			return;
		if (!IsCellVisible(TilePoint))
		{
			TileManager.TileDot.SetActive(false);
			return;
		}
		if (!LevelManager.HasWallAtPosition(TilePoint))
		{
			if (PlayerIsInVehicle(WorldPoint, TilePoint, ShiftedClickPoint)) return;
			if (TryAddItem(ShiftedClickPoint)) return;
			if (!Player.HasEnergy) return;
			if (TryUseItemOnTile(TilePoint, ShiftedClickPoint)) return;
			if (TryUseItemOnPlayer(ShiftedClickPoint)) return;
			if (TryUseItemOnVehicle(TilePoint)) return;
			if (TryEnterVehicle(TilePoint)) return;
			if (TryPlayerMovement(WorldPoint, TilePoint, ShiftedClickPoint)) return;
			TryPlayerAttack(ShiftedClickPoint);
		}
	}
	/// <summary>
	/// Handles TileDot hover over a tile
	/// </summary>
	private void HandlePlayerHover(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		if (!IsCellVisible(TilePoint))
		{
			TileManager.TileDot.SetActive(false);
			return;
		}
		// Hide TileDot if player is in vehicle with no charge
		if (Player.IsInVehicle
			&& Player.Vehicle.Info.IsOn
			&& !Player.Vehicle.HasCharge())
			TileManager.TileDot.SetActive(false);
		// Move TileDot to hovered tile if Player is in movement range
		else if (TileManager.IsInTileArea(TilePoint))
		{
			TileManager.TileDot.SetActive(true);
			TileManager.TileDot.transform.position = ShiftedClickPoint;
		}
		// Hide TileDot if hovered tile is out of movement range
		else
			TileManager.TileDot.SetActive(false);
	}
	/// <summary>
	/// Checks if Player is in a vehicle at specified world point
	/// </summary>
	private bool PlayerIsInVehicle(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		// Return false if Player is not in a vehicle
		if (!Player.IsInVehicle)
			return false;
		// If Player's vehicle is at clicked position, try self-use item first (e.g. flare), then toggle ignition
		if (Player.Vehicle.transform.position == ShiftedClickPoint)
		{
			if (Player.HasEnergy && Player.ClickOnToUseItem())
			{
				TurnManager.TurnTimer.StartTimer();
				RefreshVisibility();
				UpdateTileAreas();
				ChronoclasmManager.ClearUndoHistory("Undo history cleared after using an item.");
			}
			else
			{
				Player.Vehicle.SwitchIgnition();
				TileManager.ClearTileAreas();
				TileManager.ClearTargets();
				RefreshVisibility();
				UpdateTileAreas();
			}
		}
		// If Player's vehicle is off and clicked tile is in movement range, try to exit vehicle
		else if (!Player.Vehicle.Info.IsOn
				&& TileManager.IsInTileArea(TilePoint))
			TryExitVehicle(WorldPoint, TilePoint, ShiftedClickPoint);
		// If Player's vehicle is on, has fuel, and clicked tile is in movement range, try to move vehicle
		else if (Player.Vehicle.Info.IsOn
				&& Player.Vehicle.HasCharge())
			TryVehicleMovement(WorldPoint, TilePoint, ShiftedClickPoint);
		return true;
	}
	/// <summary>
	/// Tries to exit Player's vehicle at specified world point
	/// </summary>
	private void TryExitVehicle(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		// Check if Player's vehicle can exit to clicked tile
		bool isInMovementRange = TileManager.IsInTileArea(TilePoint);
		// Return if not in movement range, enemy present, clicked on current position, or Player has no energy
		if (!isInMovementRange
			|| HasEnemyAtPosition(ShiftedClickPoint)
			|| HasFireAtPosition(TilePoint)
			|| ShiftedClickPoint == Player.transform.position
			|| !Player.HasEnergy)
			return;
		// Player exits vehicle
		TurnManager.SetEndTurnButtonInteractable(false);
		ChronoclasmManager.RecordUndoSnapshot();
		Player.IsInMovement = true;
		Player.ExitVehicle();
		RefreshVisibility();
		Player.ComputePathAndStartMovement(WorldPoint);
		TileManager.ClearTileAreas();
		UpdateTargets();
		TurnManager.TurnTimer.StartTimer();
	}
	/// <summary>
	/// Tries to move Player's vehicle to specified world point
	/// </summary>
	private void TryVehicleMovement(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		// Check if Player's vehicle can move to clicked tile
		bool isInMovementRange = TileManager.IsInTileArea(TilePoint);
		// Return if not in movement range, enemy present, or clicked on current position
		if (!isInMovementRange
			|| HasEnemyAtPosition(ShiftedClickPoint)
			|| HasFireAtPosition(TilePoint)
			|| ShiftedClickPoint == Player.transform.position)
			return;
		// Start Player's vehicle movement
		TurnManager.SetEndTurnButtonInteractable(false);
		ChronoclasmManager.RecordUndoSnapshot();
		Player.IsInMovement = true;
		Player.VehicleMovement(WorldPoint);
		TileManager.ClearTileAreas();
		TurnManager.TurnTimer.StartTimer();
	}
	/// <summary>
	/// Tries to add an item to Player's inventory at specified shifted click point
	/// </summary>
	private bool TryAddItem(Vector3 ShiftedClickPoint)
	{
		// Return false if Player is not adjacent to clicked position or if click fails
		if (ShiftedClickPoint != Player.transform.position
			|| GetItemAtPosition(ShiftedClickPoint) is not Item Item)
			return false;
		Inventory Inventory = Player.InventoryUI != null ? Player.InventoryUI.Inventory : null;
		if (Inventory == null || Inventory.Count >= Inventory.Size)
			return false;
		ChronoclasmManager.RecordUndoSnapshot(true);
		// Return false if Player's inventory is full
		if (!Player.TryAddItem(Item))
			return false;
		ItemManager.DestroyItemAtPosition(ShiftedClickPoint);
		RefreshVisibility();
		return true;
	}
	/// <summary>
	/// Tries to use a selected item directly on the clicked tile
	/// </summary>
	private bool TryUseItemOnTile(Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		ItemInfo Selected = Player.SelectedItemInfo;
		if (Selected == null)
			return false;
		// Extinguisher puts out fire on the clicked tile
		if (Selected.Tag is ItemInfo.Tags.Extinguisher)
		{
			if (!IsPlayerAdjacentTo(ShiftedClickPoint) || !HasFireAtPosition(TilePoint))
				return false;
			if (TryExtinguishFire(TilePoint))
			{
				Player.UseItem();
				TurnManager.TurnTimer.StartTimer();
				RefreshVisibility();
				UpdateTileAreas();
				ChronoclasmManager.ClearUndoHistory("Undo history cleared after using an item on a tile.");
				return true;
			}
		}
		// Firestarters place a fire tile, but only if no fire, wall, enemy, vehicle, or player at position
		else if (Selected.Tag is ItemInfo.Tags.Blowtorch or ItemInfo.Tags.Flamethrower)
		{
			if (!TileManager.IsInTileArea(TilePoint)
				|| LevelManager.HasWallAtPosition(TilePoint)
				|| HasEnemyAtPosition(ShiftedClickPoint)
				|| HasFireAtPosition(TilePoint)
				|| HasVehicleAtPosition(ShiftedClickPoint)
				|| ShiftedClickPoint == Player.transform.position)
				return false;
			bool spawnedFire = Selected.Tag == ItemInfo.Tags.Flamethrower
				? TrySpawnFlamethrowerLine(ShiftedClickPoint)
				: TrySpawnFire(TilePoint, false, true);
			if (spawnedFire)
			{
				Player.UseItem();
				TurnManager.TurnTimer.StartTimer();
				TileManager.ClearTileAreas();
				RefreshVisibility();
				UpdateTileAreas();
				ChronoclasmManager.ClearUndoHistory("Undo history cleared after using an item on a tile.");
				return true;
			}
		}
		return false;
	}
	/// <summary>
	/// Tries use an item on Player at specified shifted click point
	/// </summary>
    private bool TryUseItemOnPlayer(Vector3 ShiftedClickPoint)
	{
		// Return false if Player is not adjacent to clicked position or if click fails
		if (ShiftedClickPoint != Player.transform.position
			|| !Player.ClickOnToUseItem())
			return false;
		TurnManager.TurnTimer.StartTimer();
		RefreshVisibility();
		UpdateTileAreas();
		ChronoclasmManager.ClearUndoHistory("Undo history cleared after using an item.");
		return true;
	}
	/// <summary>
	/// Tries use an item on a vehicle at specified tile point
	/// </summary>
	private bool TryUseItemOnVehicle(Vector3Int TilePoint)
	{
		// Get vehicle index at position
		Vehicle Vehicle = GetVehicleAtPosition(TilePoint);
		// Return false if no vehicle found or Player has no selected item
		if (Vehicle == null)
			return false;
		// Return false if player is not adjacent to vehicle or if click fails
		if (!IsPlayerAdjacentTo(Vehicle.transform.position)
			|| !Player.ClickOnVehicleToUseItem(Vehicle))
			return false;
		TurnManager.TurnTimer.StartTimer();
		RefreshVisibility();
		UpdateTileAreas();
		ChronoclasmManager.ClearUndoHistory("Undo history cleared after using an item on a vehicle.");
		return true;
	}
	private bool TryEnterVehicle(Vector3Int TilePoint)
	{
		// Get vehicle index at position
		Vehicle Vehicle = GetVehicleAtPosition(TilePoint);
		// Return false if no vehicle found
		if (Vehicle == null)
			return false;
		// Return false if player is not adjacent to vehicle or if vehicle is on fire
		if (!IsPlayerAdjacentTo(Vehicle.transform.position)
			|| HasFireAtPosition(TilePoint))
			return false;
		ChronoclasmManager.RecordUndoSnapshot();
		Player.EnterVehicle(Vehicle);
		TurnManager.TurnTimer.StartTimer();
		TileManager.ClearTargets();
		RefreshVisibility();
		UpdateTileAreas();
		return true;
	}
	/// <summary>
	/// Tries to move Player to clicked tile point
	/// </summary>
	private bool TryPlayerMovement(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint)
	{
		if (IsFirestarterSelected())
			return false;
		// Check if player can move to clicked tile
		bool isInMovementRange = TileManager.IsInTileArea(TilePoint);
		// Return false if not in movement range, enemy present, or clicked on current position
		if (!isInMovementRange
			|| HasEnemyAtPosition(ShiftedClickPoint)
			|| HasFireAtPosition(TilePoint)
			|| ShiftedClickPoint == Player.transform.position)
		{
			return false;
		}
		// Start Player movement
		TurnManager.SetEndTurnButtonInteractable(false);
		ChronoclasmManager.RecordUndoSnapshot();
		Player.IsInMovement = true;
		Player.ComputePathAndStartMovement(WorldPoint);
		TileManager.ClearTileAreas();
		TurnManager.TurnTimer.StartTimer();
		return true;
	}
	/// <summary>
	/// Tries to attack an enemy at specified tile point
	/// </summary>
    private void TryPlayerAttack(Vector3 ShiftedClickPoint)
	{
		// Check if player can attack an enemy at clicked tile
		bool isInMeleeRange = IsPlayerAdjacentTo(ShiftedClickPoint);
		bool isInRangedWeaponRange = Player.HasRange && TileManager.IsInRangedWeaponRange(ShiftedClickPoint);
		// Return if no enemy found, not in melee or ranged weapon range, or selected item is not a weapon
		if (!HasEnemyAtPosition(ShiftedClickPoint)
			|| !isInMeleeRange
			&& !isInRangedWeaponRange
			|| Player.SelectedItemInfo?.Type is not ItemInfo.Types.Weapon
			|| !Player.HasUses)
			return;
		// Flamethrower sprays a fire streak; blowtorch spawns a single fire tile
		if (Player.SelectedItemInfo.Tag is ItemInfo.Tags.Blowtorch or ItemInfo.Tags.Flamethrower)
		{
			Vector3Int TilePoint = TilemapGround.WorldToCell(ShiftedClickPoint);
			if (LevelManager.HasWallAtPosition(TilePoint)
				|| HasFireAtPosition(TilePoint)
				|| HasVehicleAtPosition(ShiftedClickPoint)
				|| ShiftedClickPoint == Player.transform.position)
				return;
			bool spawnedFire = Player.SelectedItemInfo.Tag is ItemInfo.Tags.Flamethrower
				? TrySpawnFlamethrowerLine(ShiftedClickPoint)
				: TrySpawnFire(TilePoint, false, true);
			if (spawnedFire)
			{
				Player.AttackEnemy();
				TurnManager.TurnTimer.StartTimer();
				TileManager.TileDot.SetActive(false);
				RefreshVisibility();
				UpdateTargets();
				UpdateTileAreas();
				ChronoclasmManager.ClearUndoHistory("Undo history cleared after attacking.");
			}
			return;
		}
		// Drop a rock after it is thrown
		if (Player.SelectedItemInfo.Tag == ItemInfo.Tags.Rock)
			SpawnItem((int)Player.SelectedItemInfo.Tag, ShiftedClickPoint);
		// Handle damage to enemy
		Enemy Enemy = GetEnemyAtPosition(ShiftedClickPoint);
		EnemyManager.HandleDamageToEnemy(Enemy, Player.DamagePoints, Player.SelectedItemInfo.IsStunning);
		Player.AttackEnemy();
		TurnManager.TurnTimer.StartTimer();
		TileManager.TileDot.SetActive(false);
		UpdateTargets();
		UpdateTileAreas();
		ChronoclasmManager.ClearUndoHistory("Undo history cleared after attacking.");
	}
	/// <summary>
	/// Spawns a flamethrower fire streak between Player and target
	/// </summary>
	private bool TrySpawnFlamethrowerLine(Vector3 TargetWorldPosition)
	{
		Vector3Int TargetCell = TilemapGround.WorldToCell(TargetWorldPosition);
		if (!TrySpawnFire(TargetCell, false, true))
			return false;
		List<Vector3Int> LineCells = TileManager.BresenhamsAlgorithm(Player.transform.position, TargetWorldPosition);
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		foreach (Vector3Int Cell in LineCells)
		{
			if (Cell == PlayerCell || Cell == TargetCell)
				continue;
			TrySpawnFire(Cell, false, true);
		}
		return true;
	}
	/// <summary>
	/// Checks if Player is adjacent to a specified position
	/// </summary>
	private bool IsPlayerAdjacentTo(Vector3 Position) => Vector3.Distance(Player.transform.position, Position) <= 1.0f;
	/// <summary>
	/// Returns true when Player has a selected firestarter item
	/// </summary>
	private bool IsFirestarterSelected() => Player.SelectedItemInfo?.Tag is ItemInfo.Tags.Blowtorch or ItemInfo.Tags.Flamethrower;
	private bool IsValidFireTarget(Vector3Int Cell)
	{
		if (!IsCellVisible(Cell))
			return false;
		if (Cell == TilemapGround.WorldToCell(Player.transform.position))
			return false;
		if (LevelManager.HasWallAtPosition(Cell))
			return false;
		if (HasFireAtPosition(Cell))
			return false;
		Vector3 WorldPosition = Cell + new Vector3(0.5f, 0.5f);
		if (HasEnemyAtPosition(WorldPosition))
			return false;
		if (HasVehicleAtPosition(WorldPosition))
			return false;
		return true;
	}
	/// <summary>
	/// Updates tile areas based on Player's movement and energy
	/// </summary>
	public void UpdateTileAreas()
	{
		// Only update areas if Player is not in movement and it is Player's turn
		if (Player.IsInMovement || !TurnManager.IsPlayersTurn)
			return;
		// Clear areas if player has no energy
		if (!Player.HasEnergy)
		{
			TileManager.ClearTileAreas();
			return;
		}
		Dictionary<Vector3Int, Node> AreasToDraw = null;
		// Firestarters override movement areas while on foot
		if (!Player.IsInVehicle && IsFirestarterSelected())
		{
			Vector3 PlayerPosition = Player.transform.position;
			Vector3Int PlayerCell = TilemapGround.WorldToCell(PlayerPosition);
			bool useLineOfSight = Player.SelectedItemInfo.Tag == ItemInfo.Tags.Flamethrower;
			AreasToDraw = TileManager.CalculateFirestarterArea(
				PlayerPosition,
				PlayerCell,
				Player.WeaponRange,
				useLineOfSight,
				TilemapWalls,
				TilemapGround.cellBounds,
				IsValidFireTarget);
		}
		// If Player is in a vehicle that is on and has charge, calculate vehicle area
		else if (Player.IsInVehicle
			&& Player.Vehicle.Info.IsOn
			&& Player.Vehicle.HasCharge())
			AreasToDraw = Player.Vehicle.CalculateArea();
		// If Player is not in a vehicle, or is in a vehicle that is off, calculate player area
		else if (!Player.IsInVehicle
			|| (Player.IsInVehicle && !Player.Vehicle.Info.IsOn))
			AreasToDraw = Player.CalculateArea();
		// Draw areas if any needed
		if (AreasToDraw != null)
		{
			AreasToDraw = VisibilityManager != null
				? VisibilityManager.FilterVisibleCells(AreasToDraw)
				: AreasToDraw;
			TileManager.DrawTileAreas(AreasToDraw);
		}
		// Clear areas if in vehicle with no charge
		else if (Player.IsInVehicle
			&& Player.Vehicle.Info.IsOn
			&& !Player.Vehicle.HasCharge())
			TileManager.ClearTileAreas();
	}
	/// <summary>
	/// Updates targets based on Player's weapon range and energy
	/// </summary>
	public void UpdateTargets()
	{
		// Only update targets if Player is not in movement, has a ranged weapon, it is Player's turn, and Player has energy
		if (!Player.HasRange
			|| !TurnManager.IsPlayersTurn
			|| !Player.HasEnergy)
		{
			TileManager.ClearTargets();
			return;
		}
		// When flamethrower is equipped, don't draw targets on enemies on fire
		if (Player.SelectedItemInfo.Tag is ItemInfo.Tags.Flamethrower)
		{
			List<Enemy> TargetableEnemies = new();
			foreach (Enemy Enemy in EnemyManager.Enemies)
			{
				if (Enemy == null)
					continue;
				Vector3Int EnemyCell = TilemapGround.WorldToCell(Enemy.transform.position);
				if (!IsCellVisible(EnemyCell))
					continue;
				if (HasFireAtWorld(Enemy.transform.position))
					continue;
				TargetableEnemies.Add(Enemy);
			}
			TileManager.DrawTargets(TargetableEnemies, Player.transform.position, Player.WeaponRange, Player.SelectedItemInfo.IsStunning, TilemapWalls);
		}
		else
		{
			List<Enemy> TargetableEnemies = new();
			foreach (Enemy Enemy in EnemyManager.Enemies)
			{
				if (Enemy == null)
					continue;
				Vector3Int EnemyCell = TilemapGround.WorldToCell(Enemy.transform.position);
				if (!IsCellVisible(EnemyCell))
					continue;
				TargetableEnemies.Add(Enemy);
			}
			TileManager.DrawTargets(TargetableEnemies, Player.transform.position, Player.WeaponRange, Player.SelectedItemInfo.IsStunning, TilemapWalls);
		}
	}
}
