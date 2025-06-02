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
    private Tilemap TilemapGround;
    private Tilemap TilemapWalls;
    public void Initialize(Tilemap Ground, Tilemap Walls, GameObject[] Templates)
    {
        TilemapGround = Ground;
        TilemapWalls = Walls;
        EnemyTemplates = Templates;
    }
    public void GenerateEnemies()
    {
        spawnEnemyCount = Random.Range(1 + (int)(GameManager.Instance.LevelManager.Level * 0.5),
                                       3 + (int)(GameManager.Instance.LevelManager.Level * 0.5));
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
    public bool HasEnemyAtPosition(Vector3 Position)
    {
        return Enemies.Find(Enemy => Enemy.transform.position == Position) != null;
    }
    public int GetEnemyIndexAtPosition(Vector3Int Position)
    {
        Vector3 ShiftedPosition = Position + new Vector3(0.5f, 0.5f, 0);
        return Enemies.FindIndex(Enemy => Enemy.transform.position == ShiftedPosition);
    }
    private void DestroyEnemy(Enemy Enemy)
    {
        Destroy(Enemy.StunIcon);
        Destroy(Enemy.gameObject);
    }
    public void DestroyAllEnemies()
    {
        Enemies.ForEach(Enemy => DestroyEnemy(Enemy));
        Enemies.Clear();
    }
    private void RestoreAllEnemyEnergy()
    {
        Enemies.ForEach(Enemy => Enemy.RestoreEnergy());
    }
    public void HandleDamageToEnemy(int index, int damagePoints, bool isStunning)
    {
        Enemy DamagedEnemy = Enemies[index];
        DamagedEnemy.DecreaseHealthBy(damagePoints);
        if (DamagedEnemy.Info.CurrentHealth <= 0)
        {
            Enemies.RemoveAt(index);
            DestroyEnemy(DamagedEnemy);
            return;
        }
        if (isStunning)
        {
            DamagedEnemy.Info.IsStunned = true;
            DamagedEnemy.StunIcon.SetActive(true);
        }
    }
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
            Enemies[indexOfMovingEnemy].ComputePathAndStartMovement();
            GameManager.Instance.TileManager.ClearTileAreas();
            GameManager.Instance.TileManager.ClearTargetsAndTracers();
            EnemiesAreMoving = true;
            return;
        }
        if (EnemiesAreMoving && !Enemies[indexOfMovingEnemy].IsInMovement)
        {
            if (indexOfMovingEnemy < Enemies.Count - 1)
            {
                indexOfMovingEnemy++;
                Enemies[indexOfMovingEnemy].ComputePathAndStartMovement();
                return;
            }
            EndEnemyTurn(OnMovementComplete);
        }
    }
    private void EndEnemyTurn(System.Action OnMovementComplete)
    {
        EnemiesAreMoving = false;
        RestoreAllEnemyEnergy();
        OnMovementComplete?.Invoke();
    }
}