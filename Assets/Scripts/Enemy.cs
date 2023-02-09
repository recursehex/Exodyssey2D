using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    public EnemyInfo info;

    private Animator animator;
    private Transform target;
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
        animator = GetComponent<Animator>();
        target = GameObject.FindGameObjectWithTag("Player").transform;

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
            Vector3 shiftedDistance = new Vector3(destination.x + 0.5f, destination.y + 0.5f, destination.z);
            transform.position = Vector3.MoveTowards(transform.position, shiftedDistance, 2 * Time.deltaTime);

            float distance = Vector3.Distance(shiftedDistance, transform.position);
            if (distance <= 0f)
            {
                if (path.Count > 0 && 
                    info.currentAP > 0  &&
                    path.Count > 1)
                {
                    destination = path.Pop();
                    ChangeActionPoints(-1);
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
        if (path != null  && 
            info.currentAP > 0 &&
            // To stop enemy from colliding into player
            path.Count > 2)
        {
            ChangeActionPoints(-1);

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
            if (path != null && path.Count == 2)
            {
                gm.HandleDamageToPlayer(info.damagePoints);
                SoundManager.instance.RandomizeSfx(enemyAttack, enemyAttack);
            }
            path = null;
            isInMovement = false;
        }
    }

    private bool HasEnemyAtLoc(Vector3 p)
    {
        bool ret = false;
        Vector3 shiftedDistance = new Vector3(p.x + 0.5f, p.y + 0.5f, 0);
        foreach (GameObject obj in gm.enemies)
        {
            Enemy e = obj.GetComponent<Enemy>();
            if (e.transform.position == shiftedDistance)
            {
                ret = true;
                break;
            }
        }
        return ret;
    }

    public void ChangeActionPoints(int change)
    {
        // If new AP is greater than max
        info.currentAP += change;
        if (info.currentAP > info.maxAP)
        {
            info.currentAP = info.maxAP;
        }
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
