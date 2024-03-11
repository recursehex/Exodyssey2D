using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Vehicle : MonoBehaviour
{
    public VehicleInfo info;

    private AudioClip vehicleMove;

    #region PATHFINDING

    public Tilemap tilemapGround;

    public Tilemap tilemapWalls;

    private Stack<Vector3Int> path;

    private Vector3Int destination;

    GameManager gm;

    [SerializeField]
    private AStar astar;
    #endregion

    public bool isInMovement = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Changes vehicle's HP, use negative to decrease
    /// </summary>
    public void ChangeHP(int change)
    {
        info.currentHP = Mathf.Clamp(info.currentHP + change, 0, info.maxHP);
    }

    /// <summary>
    /// Resets vehicle's HP to maxHP
    /// </summary>
    public void RestoreHP()
    {
        info.currentHP = info.maxHP;
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

    public void SetGameManager(GameManager g)
    {
        gm = g;
    }
}
