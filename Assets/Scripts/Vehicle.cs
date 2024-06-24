using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Vehicle : MonoBehaviour
{
    public VehicleInfo Info;

    private AudioClip VehicleMove;

    #region PATHFINDING

    public Tilemap TilemapGround;

    public Tilemap TilemapWalls;

    private Stack<Vector3Int> path;

    private Vector3Int destination;

    GameManager GameManager;

    [SerializeField]
    private AStar AStar;
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
        Info.currentHealth = Mathf.Clamp(Info.currentHealth + change, 0, Info.maxHealth);
    }

    /// <summary>
    /// Resets vehicle's Health to maxHealth
    /// </summary>
    public void RestoreHealth()
    {
        Info.currentHealth = Info.maxHealth;
    }

    public void SetGameManager(GameManager G)
    {
        GameManager = G;
    }
}
