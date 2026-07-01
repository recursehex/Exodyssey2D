using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject[] EnemyTemplates;
    public List<Enemy> Enemies { get; private set; } = new();
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
        Enemies.Add(Enemy);
        return Enemy;
    }
    /// <summary>
    /// Returns true if an enemy is at the specified position
    /// </summary>
    public bool HasEnemyAtPosition(Vector3 Position)
    {
        CleanupDestroyedEnemies();
        return Enemies.Find(Enemy => Enemy != null && Enemy.transform.position == Position) != null;
    }
    /// <summary>
    /// Returns enemy at the specified position, or null if no enemy is found
    /// </summary>
    public Enemy GetEnemyAtPosition(Vector3 Position)
    {
        CleanupDestroyedEnemies();
        return Enemies.Find(Enemy => Enemy.transform.position == Position);
    }
    /// <summary>
    /// Destroys the specified enemy
    /// </summary>
    private void DestroyEnemy(Enemy Enemy)
    {
        Destroy(Enemy.StunIcon);
        Destroy(Enemy.gameObject);
    }
    /// <summary>
    /// Destroys all enemies and clears Enemies list
    /// </summary>
    public void DestroyAllEnemies()
    {
        Enemies.ForEach(Enemy => DestroyEnemy(Enemy));
        Enemies.Clear();
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
            Enemies.Remove(Enemy);
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
    /// Instantly kills an enemy that has been run over by a vehicle
    /// </summary>
    public void RunOverEnemy(Enemy Enemy)
    {
        if (Enemy == null)
            return;
        Enemies.Remove(Enemy);
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
        if (NeedToStartEnemyMovement)
        {
            NeedToStartEnemyMovement = false;
            indexOfMovingEnemy = 0;
            BlockedEnemies.Clear();
            IsRetryingBlockedEnemies = false;
            Enemies[indexOfMovingEnemy].ComputePathAndStartMovement();
            GameManager.Instance.ClearTileAreas();
            GameManager.Instance.ClearTargets();
            EnemiesAreMoving = true;
            return;
        }
        // Handle blocked enemies
        if (EnemiesAreMoving && !Enemies[indexOfMovingEnemy].IsInMovement)
        {
            // Check if current enemy was blocked
            Enemy CurrentEnemy = IsRetryingBlockedEnemies ? BlockedEnemies[indexOfMovingEnemy] : Enemies[indexOfMovingEnemy];
            // Add to blocked list if not already retrying blocked enemies
            if (!IsRetryingBlockedEnemies && CurrentEnemy.WasBlockedThisTurn)
                BlockedEnemies.Add(CurrentEnemy);
            // Continue with next enemy in current list
            int maxIndex = IsRetryingBlockedEnemies
                            ? BlockedEnemies.Count - 1
                            : Enemies.Count - 1;
            if (indexOfMovingEnemy < maxIndex)
            {
                indexOfMovingEnemy++;
                if (IsRetryingBlockedEnemies)
                    BlockedEnemies[indexOfMovingEnemy].ComputePathAndStartMovement();
                else
                    Enemies[indexOfMovingEnemy].ComputePathAndStartMovement();
                return;
            }
            // If finished first pass and have blocked enemies, retry them
            if (!IsRetryingBlockedEnemies && BlockedEnemies.Count > 0)
            {
                IsRetryingBlockedEnemies = true;
                indexOfMovingEnemy = 0;
                BlockedEnemies[0].ComputePathAndStartMovement();
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
                Enemies.RemoveAt(i);
        }
    }
}
