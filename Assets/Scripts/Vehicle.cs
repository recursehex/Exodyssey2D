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

    GameManager gameManager;

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
    /// Changes vehicle's Health, use negative to decrease
    /// </summary>
    public void ChangeHealth(int change)
    {
        info.currentHealth = Mathf.Clamp(info.currentHealth + change, 0, info.maxHealth);
    }

    /// <summary>
    /// Resets vehicle's Health to maxHealth
    /// </summary>
    public void RestoreHealth()
    {
        info.currentHealth = info.maxHealth;
    }

    public void SetGameManager(GameManager g)
    {
        gameManager = g;
    }
}
