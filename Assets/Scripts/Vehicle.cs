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

    public void SetGameManager(GameManager g)
    {
        gm = g;
    }
}
