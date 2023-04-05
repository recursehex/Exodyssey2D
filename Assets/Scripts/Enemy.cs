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
                // Move one tile closer to the Player
                if (path.Count > 1 && info.currentAP > 0)
                {
                    SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
                    destination = path.Pop();
                    info.currentAP--;
                }
                // Enemy attacks the Player if enemy moves to an adjacent tile
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
        // Disables diagonal movement
        astar.SetAllowDiagonal(false);

        path = astar.ComputePath(transform.position, goal, gm);
        if (path != null && info.currentAP > 0 &&
            // To stop enemy from colliding into the Player
            path.Count > 2)
        {
            info.currentAP--;
            // Do not remove this, need to pop first path element
            path.Pop();

            Vector3Int tryDistance = path.Pop();
            if (!HasEnemyAtLoc(tryDistance))
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
        else
        {
            // Enemy attacks the Player if enemy is adjacent
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
    /*
    public void CalculatePathAndStartMovement(Vector3 goal)
    {
        isInMovement = true;
        astar.Initialize();
        astar.SetAllowDiagonal(false);

        // Calculate path
        path = astar.ComputePath(transform.position, goal, gm);

        // Handle path and movement
        if (path != null && info.currentAP > 0 && CanMove(path))
        {
            // Move to new destination
            MoveToDestination(path);
            SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
        }
        else
        {
            HandleNoValidPathOrNoAP();
        }
    }

    private bool CanMove(Stack<Vector3Int> path)
    {
        // Check if path is valid
        if (path.Count < 3) // Minimum path length is 3 (start, goal, and at least one intermediate point)
        {
            return false;
        }
        Vector3Int tryDistance = path.Pop();
        if (HasEnemyAtLoc(tryDistance))
        {
            return false;
        }
        // Destination is valid
        destination = tryDistance;
        info.currentAP--;
        return true;
    }

    private void MoveToDestination(Stack<Vector3Int> path)
    {
        // Pop first element (current location)
        path.Pop();
        // Pop second element (destination)
        Vector3Int nextLoc = path.Pop();
        // Move to next location
        transform.position = gm.grid.CellToWorld(nextLoc);
    }

    private void HandleNoValidPathOrNoAP()
    {
        // Handle no valid path found or no AP remaining
        if (path != null && path.Count == 2) // Enemy is adjacent
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
    */
    private bool HasEnemyAtLoc(Vector3 p)
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
