using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject[] EnemyTemplates;
    public List<Enemy> Enemies { get; private set; } = new();
    [SerializeField] private int spawnEnemyCount;
    public bool NeedToStartEnemyMovement { get; set; } = false;
    [SerializeField] private bool EnemiesAreMoving = false;
    [SerializeField] private int indexOfMovingEnemy = -1;
    private readonly List<Enemy> BlockedEnemies = new();
    private bool IsRetryingBlockedEnemies = false;
    private Tilemap TilemapGround;
    private Tilemap TilemapWalls;
    public System.Action OnEnemyKilled;
    public void Initialize(Tilemap Ground, Tilemap Walls, GameObject[] Templates)
    {
        TilemapGround   = Ground;
        TilemapWalls    = Walls;
        EnemyTemplates  = Templates;
    }
    /// <summary>
    /// Generates random number of enemies based on the current level
    /// </summary>
    public void GenerateEnemies()
    {
        spawnEnemyCount = Random.Range(1 + (int)(GameManager.Instance.Level * 0.5),
                                       3 + (int)(GameManager.Instance.Level * 0.5));
        int cap = spawnEnemyCount * 2;
        while (cap > 0 && spawnEnemyCount > 0)
        {
            if (WeightedRarityGeneration.Generate<Enemy>())
            {
                spawnEnemyCount--;
            }
            cap--;
        }
    }
    public void SpawnEnemy(int index, Vector3 Position)
    {
        Enemy Enemy = Instantiate(EnemyTemplates[index], Position, Quaternion.identity).GetComponent<Enemy>();
        Enemy.Initialize(TilemapGround, TilemapWalls, new EnemyInfo(index));
        Enemies.Add(Enemy);
    }
    /// <summary>
    /// Returns true if an enemy is at the specified position
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
    public bool HasEnemyAtPosition(Vector3 Position) => Enemies.Find(Enemy => Enemy.transform.position == Position) != null;
    /// <summary>
    /// Returns the index of the enemy at the specified position, or -1 if no enemy is found
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
    public int GetEnemyIndexAtPosition(Vector3Int Position)
    {
        Vector3 ShiftedPosition = Position + new Vector3(0.5f, 0.5f);
        return Enemies.FindIndex(Enemy => Enemy.transform.position == ShiftedPosition);
    }
    /// <summary>
    /// Destroys the specified enemy
    /// </summary>
    /// <param name="Enemy"></param>
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
    }
    /// <summary>
    /// Restores all enemies' energy
    /// </summary>
    private void RestoreAllEnemyEnergy() => Enemies.ForEach(Enemy => Enemy.RestoreEnergy());
    /// <summary>
    /// Handles damage to an enemy at the specified index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="damagePoints"></param>
    /// <param name="isStunning"></param>
    public void HandleDamageToEnemy(int index, int damagePoints, bool isStunning)
    {
        Enemy DamagedEnemy = Enemies[index];
        DamagedEnemy.DecreaseHealthBy(damagePoints);
        if (DamagedEnemy.Info.CurrentHealth <= 0)
        {
            Enemies.RemoveAt(index);
            DestroyEnemy(DamagedEnemy);
            OnEnemyKilled?.Invoke();
            return;
        }
        if (isStunning)
        {
            DamagedEnemy.Info.IsStunned = true;
            DamagedEnemy.StunIcon.SetActive(true);
        }
    }
    /// <summary>
    /// Processes enemy movement for all enemies
    /// </summary>
    /// <param name="OnMovementComplete"></param>
    public void ProcessEnemyMovement(System.Action OnMovementComplete)
    {
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
            if (!IsRetryingBlockedEnemies && CurrentEnemy.WasBlockedThisTurn)
            {
                BlockedEnemies.Add(CurrentEnemy);
            }
            // Continue with next enemy in current list
            int maxIndex = IsRetryingBlockedEnemies
                            ? BlockedEnemies.Count - 1
                            : Enemies.Count - 1;
            if (indexOfMovingEnemy < maxIndex)
            {
                indexOfMovingEnemy++;
                if (IsRetryingBlockedEnemies)
                {
                    BlockedEnemies[indexOfMovingEnemy].ComputePathAndStartMovement();
                }
                else
                {
                    Enemies[indexOfMovingEnemy].ComputePathAndStartMovement();
                }
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
    /// <param name="OnMovementComplete"></param>
    private void EndEnemyTurn(System.Action OnMovementComplete)
    {
        EnemiesAreMoving = false;
        RestoreAllEnemyEnergy();
        OnMovementComplete?.Invoke();
    }
}