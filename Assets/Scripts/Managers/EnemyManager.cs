using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject[] EnemyTemplates;
    private readonly GridEntityRegistry<Enemy> Registry = new(Enemy => GridCoordinates.GetCell(Enemy.transform.position));
    public List<Enemy> Enemies => Registry.Entities;
    [SerializeField] private int spawnEnemyCount;
    [Header("Spawning")]
    [SerializeField] private int baseMinSpawn = 1;
    [SerializeField] private int baseMaxSpawn = 3;
    [SerializeField] private int spawnScalePerLevel = 2;
    public bool NeedToStartEnemyMovement { get; set; } = false;
    [SerializeField] private bool EnemiesAreMoving = false;
    [SerializeField] private int indexOfMovingEnemy = -1;
    private readonly List<Enemy> BlockedEnemies = new();
    private bool IsRetryingBlockedEnemies = false;
    private Tilemap TilemapGround;
    private Tilemap TilemapWalls;
    public System.Action OnEnemyKilled;
    public bool IsProcessingEnemyMovement => NeedToStartEnemyMovement || EnemiesAreMoving;
    public void Initialize(Tilemap Ground, Tilemap Walls, GameObject[] Templates)
    {
        TilemapGround   = Ground;
        TilemapWalls    = Walls;
        EnemyTemplates  = Templates;
    }
    /// <summary>
    /// Generates random number of enemies based on the current level, guaranteeing
    /// at least the current region's minimum whenever enough empty tiles exist
    /// </summary>
    public void GenerateEnemies()
    {
        int levelBonus = (int)RegionManager.CurrentRegion.Tag / spawnScalePerLevel;
        int regionMin = RegionManager.CurrentRegion.MinEnemySpawn;
        int rolled = Random.Range(baseMinSpawn + levelBonus,
                                  baseMaxSpawn + levelBonus);
        int target = Mathf.Max(rolled, regionMin);
        spawnEnemyCount = WeightedRarityGeneration.GenerateBatch<Enemy>(regionMin, target);
    }
    /// <summary>
    /// Spawns an enemy of the specified type at the specified position
    /// </summary>
    public Enemy SpawnEnemy(int index, Vector3 Position)
    {
        EnemyInfo EnemyInfo = new(index);
        Enemy Enemy = Instantiate(EnemyTemplates[index], Position, Quaternion.identity).GetComponent<Enemy>();
        Enemy.Initialize(TilemapGround, TilemapWalls, EnemyInfo);
        Enemy.OnCellChanged += HandleEnemyCellChanged;
        Registry.Add(Enemy);
        return Enemy;
    }
    /// <summary>
    /// Returns true if an enemy is at the specified position
    /// </summary>
    public bool HasEnemyAtPosition(Vector3 Position) => GetEnemyAtPosition(Position) != null;
    /// <summary>
    /// Returns enemy at the specified position, or null if no enemy is found
    /// </summary>
    public Enemy GetEnemyAtPosition(Vector3 Position)
    {
        return Registry.GetFirst(GridCoordinates.GetCell(Position));
    }
    /// <summary>
    /// Destroys the specified enemy
    /// </summary>
    private void DestroyEnemy(Enemy Enemy)
    {
        Enemy.OnCellChanged -= HandleEnemyCellChanged;
        Destroy(Enemy.StunIcon);
        Destroy(Enemy.gameObject);
    }

    private void HandleEnemyCellChanged(Enemy Enemy) => Registry.UpdateCell(Enemy);
    /// <summary>
    /// Destroys all enemies and clears Enemies list
    /// </summary>
    public void DestroyAllEnemies()
    {
        Enemies.ForEach(Enemy => DestroyEnemy(Enemy));
        Registry.Clear();
        NeedToStartEnemyMovement = false;
        EnemiesAreMoving = false;
        indexOfMovingEnemy = -1;
        BlockedEnemies.Clear();
        IsRetryingBlockedEnemies = false;
    }
    /// <summary>
    /// Restores all enemies' energy
    /// </summary>
    private void RestoreAllEnemyEnergy() => Enemies.ForEach(Enemy => Enemy.RestoreEnergy());
    /// <summary>
    /// Handles damage to an enemy
    /// </summary>
    public void HandleDamageToEnemy(Enemy Enemy, int damagePoints, bool isStunning)
    {
        Enemy.DecreaseHealthBy(damagePoints);
        if (Enemy.Info.CurrentHealth <= 0)
        {
            Registry.Remove(Enemy);
            DestroyEnemy(Enemy);
            OnEnemyKilled?.Invoke();
            return;
        }
        if (isStunning)
        {
            Enemy.Info.IsStunned = true;
            Enemy.StunIcon.SetActive(true);
        }
    }
    /// <summary>
    /// Instantly kills an enemy (e.g. when run over or rammed by a vehicle)
    /// </summary>
    public void KillEnemy(Enemy Enemy)
    {
        if (Enemy == null)
            return;
        Registry.Remove(Enemy);
        DestroyEnemy(Enemy);
        OnEnemyKilled?.Invoke();
    }
    /// <summary>
    /// Processes enemy movement for all enemies
    /// </summary>
    public void ProcessEnemyMovement(System.Action OnMovementComplete)
    {
        CleanupDestroyedEnemies();
        if (Enemies.Count == 0)
        {
            OnMovementComplete?.Invoke();
            return;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Frozen enemies skip their turn entirely but keep the turn flow intact
        if (CheatFlags.FreezeEnemies)
        {
            NeedToStartEnemyMovement = false;
            EnemiesAreMoving = false;
            indexOfMovingEnemy = -1;
            RestoreAllEnemyEnergy();
            OnMovementComplete?.Invoke();
            return;
        }
#endif
        if (NeedToStartEnemyMovement)
        {
            NeedToStartEnemyMovement = false;
            indexOfMovingEnemy = 0;
            BlockedEnemies.Clear();
            IsRetryingBlockedEnemies = false;
            Enemies[indexOfMovingEnemy].ComputePathAndStartMovement();
            EnemiesAreMoving = true;
            return;
        }
        // Handle blocked enemies
        if (EnemiesAreMoving)
        {
            // During the retry pass indexOfMovingEnemy indexes BlockedEnemies, so the
            // wait-for-movement gate must poll that list, not Enemies — otherwise the
            // turn can end (restoring energy) while a retried enemy is still moving
            List<Enemy> ActiveList = IsRetryingBlockedEnemies ? BlockedEnemies : Enemies;
            // An enemy destroyed mid-pass can shrink the list; end the turn cleanly
            // instead of indexing out of range
            if (indexOfMovingEnemy >= ActiveList.Count)
            {
                EndEnemyTurn(OnMovementComplete);
                return;
            }
            Enemy CurrentEnemy = ActiveList[indexOfMovingEnemy];
            if (CurrentEnemy != null && CurrentEnemy.IsInMovement)
                return;
            // Add to blocked list if not already retrying blocked enemies
            if (!IsRetryingBlockedEnemies && CurrentEnemy != null && CurrentEnemy.WasBlockedThisTurn)
                BlockedEnemies.Add(CurrentEnemy);
            // Continue with next enemy in current list
            if (indexOfMovingEnemy < ActiveList.Count - 1)
            {
                indexOfMovingEnemy++;
                Enemy NextEnemy = ActiveList[indexOfMovingEnemy];
                // A destroyed entry is skipped on the next frame by the gate above
                if (NextEnemy != null)
                    NextEnemy.ComputePathAndStartMovement();
                return;
            }
            // If finished first pass and have blocked enemies, retry them
            if (!IsRetryingBlockedEnemies && BlockedEnemies.Count > 0)
            {
                IsRetryingBlockedEnemies = true;
                indexOfMovingEnemy = 0;
                Enemy FirstBlocked = BlockedEnemies[0];
                if (FirstBlocked != null)
                    FirstBlocked.ComputePathAndStartMovement();
                return;
            }
            EndEnemyTurn(OnMovementComplete);
        }
    }
    /// <summary>
    /// Ends the enemy turn and restores energy
    /// </summary>
    private void EndEnemyTurn(System.Action OnMovementComplete)
    {
        EnemiesAreMoving = false;
        RestoreAllEnemyEnergy();
        OnMovementComplete?.Invoke();
    }
    /// <summary>
    /// Cleans up destroyed enemies from the Enemies list
    /// </summary>
    private void CleanupDestroyedEnemies()
    {
        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            if (Enemies[i] == null)
                Registry.Remove(Enemies[i]);
        }
    }
}
