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
		if (doingSetup
			|| !Player.FinishedInit
			|| Player.IsInMovement
			|| Player.Vehicle != null && Player.Vehicle.IsInMovement)
		{
			return;
		}
		MovePlayerToVehicle();
		if (!Player.IsInMovement)
		{
			PlayerIsOnExitTile();
		}
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
			Player.Vehicle.DecreaseFuelBy(1);
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
	private void OnLevelInitialized()
	{
		EnemyManager.GenerateEnemies();
		ItemManager.GenerateItems();
		VehicleManager.GenerateVehicles();
		Invoke(nameof(OnLevelLoadComplete), 1.5f);
	}
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
	public void SpawnItem(int index, Vector3 Position)
	{
		ItemManager.SpawnItem(index, Position);
	}
	public void SpawnEnemy(int index, Vector3 Position)
	{
		EnemyManager.SpawnEnemy(index, Position);
	}
	public void SpawnVehicle(int index, Vector3 Position)
	{
		VehicleManager.SpawnVehicle(index, Position);
	}
	// Public accessor methods
	public bool HasItemAtPosition(Vector3 Position) => ItemManager.HasItemAtPosition(Position);
	public Item GetItemAtPosition(Vector3 Position) => ItemManager.GetItemAtPosition(Position);
	public void RemoveItemAtPosition(Item item) => ItemManager.RemoveItemAtPosition(item);
	public bool HasEnemyAtPosition(Vector3 Position) => EnemyManager.HasEnemyAtPosition(Position);
	public bool HasVehicleAtPosition(Vector3 Position) => VehicleManager.HasVehicleAtPosition(Position);
	public bool HasWallAtPosition(Vector3Int Position) => LevelManager.HasWallAtPosition(Position);
	public void DestroyVehicle(Vehicle vehicle) => VehicleManager.DestroyVehicle(vehicle);
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
	private void OnPlayerTurnEnded()
	{
		Player.RestoreEnergy();
		if (EnemyManager.Enemies.Count == 0)
		{
			TileManager.TileDot.SetActive(true);
		}
		EnemyManager.NeedToStartEnemyMovement = EnemyManager.Enemies.Count > 0;
	}
	public void OnTurnTimerEnd()
	{
		TileManager.TileDot.SetActive(false);
		Player.SetEnergyToZero();
		TileManager.ClearTileAreas();
		TileManager.ClearTargetsAndTracers();
		TileDot.SetActive(false);
		TurnManager.OnTurnTimerEnd();
	}
	private void OnEnemyTurnEnded()
	{
		TileManager.TileDot.SetActive(true);
		if (!Player.IsInVehicle)
			UpdateTargetsAndTracers();
	}
	private bool PlayerIsOnExitTile()
	{
		if (LevelManager.HasExitTileAtPosition(Vector3Int.FloorToInt(Player.transform.position)))
		{
			TileManager.TileDot.SetActive(false);
			ResetForNextLevel();
			return true;
		}
		return false;
	}
	private void HandlePlayerClick(Vector3 worldPoint, Vector3Int tilePoint, Vector3 shiftedClickPoint)
	{
		if (!Player.IsInMovement
			&& TurnManager.IsPlayersTurn
			&& !LevelManager.HasWallAtPosition(tilePoint))
		{
			if (PlayerIsInVehicle(worldPoint, tilePoint, shiftedClickPoint)) return;
			if (TryAddItem(shiftedClickPoint)) return;
			if (!Player.HasEnergy()) return;
			if (TryUseItemOnPlayer(shiftedClickPoint)) return;
			if (TryEnterVehicle(tilePoint)) return;
			if (TryPlayerMovement(worldPoint, tilePoint, shiftedClickPoint)) return;
			TryPlayerAttack(tilePoint, shiftedClickPoint);
		}
	}
	private void HandlePlayerHover(Vector3 worldPoint, Vector3Int tilePoint, Vector3 shiftedClickPoint)
	{
		if (TileManager.IsInMovementRange(tilePoint))
		{
			TileManager.TileDot.SetActive(true);
			TileManager.TileDot.transform.position = shiftedClickPoint;
		}
	}
    private bool TryAddItem(Vector3 shiftedClickPoint)
    {
        if (shiftedClickPoint != Player.transform.position
			|| GetItemAtPosition(shiftedClickPoint) is not Item itemAtPosition)
        {
            return false;
        }
        if (Player.TryAddItem(itemAtPosition))
        {
            ItemManager.DestroyItemAtPosition(shiftedClickPoint);
			return true;
        }
		return false;
    }
    private bool TryUseItemOnPlayer(Vector3 shiftedClickPoint) 
	{
		if (shiftedClickPoint != Player.transform.position
			|| !Player.ClickOnPlayerToUseItem())
		{
			return false;
		}
		TurnManager.TurnTimer.StartTimer();
		return true;
	}
	private bool TryPlayerMovement(Vector3 worldPoint, Vector3Int tilePoint, Vector3 shiftedClickPoint)
    {
		bool isInMovementRange = TileManager.IsInMovementRange(tilePoint);
		int enemyIndex = EnemyManager.GetEnemyIndexAtPosition(tilePoint);
        if (!isInMovementRange
			|| enemyIndex != -1
			|| shiftedClickPoint == Player.transform.position)
        {
            return false;
        }
        TurnManager.SetEndTurnButtonInteractable(false);
        Player.IsInMovement = true;
        Player.ComputePathAndStartMovement(worldPoint);
        TileManager.ClearTileAreas();
        TurnManager.TurnTimer.StartTimer();
        return true;
    }
    private void TryPlayerAttack(Vector3Int tilePoint, Vector3 shiftedClickPoint)
    {
		int enemyIndex = EnemyManager.GetEnemyIndexAtPosition(tilePoint);
		bool isInMeleeRange = IsPlayerAdjacentTo(shiftedClickPoint);
		bool isInRangedWeaponRange = Player.GetWeaponRange() > 0 && TileManager.IsInRangedWeaponRange(shiftedClickPoint);
        if (enemyIndex == -1
			|| !isInMeleeRange
			&& !isInRangedWeaponRange
			|| Player.SelectedItemInfo?.Type is not ItemInfo.Types.Weapon)
        {
            return;
        }
		if (Player.SelectedItemInfo.Tag == ItemInfo.Tags.Rock)
		{
			SpawnItem((int)Player.SelectedItemInfo.Tag, shiftedClickPoint);
		}
        EnemyManager.HandleDamageToEnemy(enemyIndex, Player.DamagePoints, Player.SelectedItemInfo.IsStunning);
        Player.AttackEnemy();
        TurnManager.TurnTimer.StartTimer();
        TileManager.TileDot.SetActive(false);
        UpdateTargetsAndTracers();
    }
    private bool TryEnterVehicle(Vector3Int tilePoint)
    {
		int vehicleIndex = VehicleManager.GetVehicleIndexAtPosition(tilePoint);
        if (vehicleIndex == -1)
        {
            return false;
        }
		if (!IsPlayerAdjacentTo(VehicleManager.Vehicles[vehicleIndex].transform.position))
		{
			return false;
		}
		Player.EnterVehicle(VehicleManager.Vehicles[vehicleIndex]);
        TurnManager.TurnTimer.StartTimer();
		TileManager.ClearTargetsAndTracers();
		return true;
    }
	private void TryVehicleMovement(Vector3 worldPoint, Vector3Int tilePoint, Vector3 shiftedClickPoint)
    {
		bool isInMovementRange = TileManager.IsInMovementRange(tilePoint);
		int enemyIndex = EnemyManager.GetEnemyIndexAtPosition(tilePoint);
        if (!isInMovementRange
			|| enemyIndex != -1
			|| shiftedClickPoint == Player.transform.position)
        {
            return;
        }
        TurnManager.SetEndTurnButtonInteractable(false);
        Player.IsInMovement = true;
		Player.VehicleMovement(worldPoint);
        TileManager.ClearTileAreas();
        TurnManager.TurnTimer.StartTimer();
    }
    private void TryExitVehicle(Vector3 worldPoint, Vector3Int tilePoint, Vector3 shiftedClickPoint)
    {
        bool isInMovementRange = TileManager.IsInMovementRange(tilePoint);
        int enemyIndex = EnemyManager.GetEnemyIndexAtPosition(tilePoint);
        if (!isInMovementRange
			|| enemyIndex != -1
			|| shiftedClickPoint == Player.transform.position
			|| !Player.HasEnergy())
        {
            return;
        }
        TurnManager.SetEndTurnButtonInteractable(false);
        Player.IsInMovement = true;
        Player.ExitVehicle();
        Player.ComputePathAndStartMovement(worldPoint);
        TileManager.ClearTileAreas();
		UpdateTargetsAndTracers();
        TurnManager.TurnTimer.StartTimer();
    }
	private bool PlayerIsInVehicle(Vector3 worldPoint, Vector3Int tilePoint, Vector3 shiftedClickPoint)
	{
		if (!Player.IsInVehicle)
		{
			return false;
		}
		if (Player.Vehicle.transform.position == shiftedClickPoint)
		{
			Player.Vehicle.SwitchIgnition();
			TileManager.ClearTileAreas();
			TileManager.ClearTargetsAndTracers();
		}
		else if (!Player.Vehicle.Info.IsOn && TileManager.IsInMovementRange(tilePoint))
		{
			TryExitVehicle(worldPoint, tilePoint, shiftedClickPoint);
		}
		else if (Player.Vehicle.Info.IsOn && Player.Vehicle.HasFuel())
		{
			TryVehicleMovement(worldPoint, tilePoint, shiftedClickPoint);
		}
		return true;
	}
	private bool IsPlayerAdjacentTo(Vector3 Position)
    {
        return Vector3.Distance(Player.transform.position, Position) <= 1.0f;
    }
	private void UpdateTileAreas()
	{
		if (!Player.IsInMovement
			&& TurnManager.IsPlayersTurn
			&& Player.HasEnergy())
		{
			Dictionary<Vector3Int, Node> AreasToDraw = null;

			if (Player.IsInVehicle
				&& Player.Vehicle.Info.IsOn
				&& Player.Vehicle.HasFuel())
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