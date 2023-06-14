using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    public EnemyInfo info;

    public AudioClip moveSound1;
    public AudioClip moveSound2;
    public AudioClip enemyAttack;
    public AudioClip chopSound1;
    public AudioClip chopSound2;

    public bool isInMovement = false;

    #region PATHFINDING

    [SerializeField]
    public Tilemap tilemapGround;

    [SerializeField]
    public Tilemap tilemapWalls;

    private Stack<Vector3Int> path;

    private Vector3Int destination;

    GameManager gm;

    [SerializeField]
    private AStar astar;
    #endregion

    // Start is called before the first frame update
    protected virtual void Start()
    {
        astar = new AStar();
        astar.tilemapGround = tilemapGround;
        astar.tilemapWalls = tilemapWalls;
    }

    public void ExposedStart()
    {
        Start();
    }

    // Player attacks enemy
    public void DamageEnemy(int loss)
    {
        SoundManager.instance.RandomizeSfx(chopSound1, chopSound2);
        // NOTE: Eventually add sprite change for enemy on this line using: spriteRenderer.sprite = dmgSprite;
        info.currentHP -= loss;
        if (info.currentHP <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (GameManager.instance.playersTurn) return;
        MoveAlongThePath();
    }

    public void MoveAlongThePath()
    {
        if (path != null)
        {
            Vector3 shiftedDistance = new(destination.x + 0.5f, destination.y + 0.5f, destination.z);
            transform.position = Vector3.MoveTowards(transform.position, shiftedDistance, 2 * Time.deltaTime);

            float distance = Vector3.Distance(shiftedDistance, transform.position);
            if (distance <= 0f)
            {
                // Move one tile closer to Player
                if (path.Count > 1 && info.currentAP > 0)
                {
                    SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
                    destination = path.Pop();
                    info.currentAP--;
                }
                // Enemy attacks Player if enemy moves to an adjacent tile
                else if (path.Count == 1 && info.currentAP > 0)
                {
                    SoundManager.instance.RandomizeSfx(enemyAttack, enemyAttack);
                    gm.HandleDamageToPlayer(info.damagePoints);
                    info.currentAP--;
                }
                else
                {
                    path = null;
                    isInMovement = false;
                }
            }
        }
    }
    
    public void CalculatePathAndStartMovement(Vector3 goal)
    {
        isInMovement = true;
        astar.Initialize();
        astar.SetAllowDiagonal(false);
        path = astar.ComputePath(transform.position, goal, gm);
        // Compute path to Player
        if (path != null && info.currentAP > 0 && path.Count > 2) // To stop enemy from colliding into Player
        {
            info.currentAP--;
            // Remove first tile in path
            path.Pop();
            // Move one tile closer to Player
            Vector3Int tryDistance = path.Pop();
            if (!HasEnemyAtPosition(tryDistance))
            {
                destination = tryDistance;
            }
            else
            {
                path = null;
                isInMovement = false;
            }
            SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
        }
        else // If enemy is adjacent to Player, attack
        {
            if (path != null && path.Count == 2)
            {
                for (int i = 0; i < info.currentAP; i++)
                {
                    SoundManager.instance.RandomizeSfx(enemyAttack, enemyAttack);
                    gm.HandleDamageToPlayer(info.damagePoints);
                }
            }
            path = null;
            isInMovement = false;
        }
    }
    
    private bool HasEnemyAtPosition(Vector3 p)
    {
        Vector3 shiftedDistance = new(p.x + 0.5f, p.y + 0.5f, 0);
        foreach (GameObject obj in gm.enemies)
        {
            Enemy e = obj.GetComponent<Enemy>();
            if (e.transform.position == shiftedDistance)
            {
                return true;
            }
        }
        return false;
    }

    public void RestoreAP()
    {
        info.currentAP = info.maxAP;
    }

    public void SetGameManager(GameManager g)
    {
        gm = g;
    }
}
