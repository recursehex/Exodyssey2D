using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VehicleManager : MonoBehaviour
{
    [SerializeField] private GameObject[] VehicleTemplates;
    private readonly GridEntityRegistry<Vehicle> Registry = new(Vehicle => GridCoordinates.GetCell(Vehicle.transform.position));
    public List<Vehicle> Vehicles => Registry.Entities;
    [SerializeField] private int spawnVehicleCount;
    [Header("Spawning")]
    [SerializeField] private int minSpawnCount = 0;
    [SerializeField] private int maxSpawnCountExclusive = 3;
    private Tilemap TilemapGround;
    private Tilemap TilemapWalls;
    private Player Player;
    public void Initialize(Tilemap Ground, Tilemap Walls, GameObject[] Templates, Player Player)
    {
        TilemapGround       = Ground;
        TilemapWalls        = Walls;
        VehicleTemplates    = Templates;
        this.Player         = Player;
    }
    public void GenerateVehicles()
    {
        int target = Random.Range(minSpawnCount, maxSpawnCountExclusive);
        spawnVehicleCount = WeightedRarityGeneration.GenerateBatch<Vehicle>(minSpawnCount, target);
    }
    /// <summary>
    /// Spawns vehicle at specified position
    /// </summary>
    public Vehicle SpawnVehicle(int index, Vector3 Position, int startingFuel = -1)
    {
        Vehicle Vehicle = Instantiate(VehicleTemplates[index], Position, Quaternion.identity).GetComponent<Vehicle>();
        VehicleInfo VehicleInfo = new(index, startingFuel);
        Vehicle.Initialize(TilemapGround, TilemapWalls, VehicleInfo);
        Vehicle.OnCellChanged += HandleVehicleCellChanged;
        Registry.Add(Vehicle);
        return Vehicle;
    }
    /// <summary>
    /// Returns true if a vehicle is at specified position
    /// </summary>
    public bool HasVehicleAtPosition(Vector3 Position)
    {
        return Registry.Contains(GridCoordinates.GetCell(Position));
    }
    /// <summary>
    /// Returns vehicle at specified position, or null if no vehicle is found
    /// </summary>
    public Vehicle GetVehicleAtPosition(Vector3Int Position)
    {
        return Registry.GetFirst(Position);
    }
    /// <summary>
    /// Destroys specified vehicle
    /// </summary>
    public void DestroyVehicle(Vehicle Vehicle)
    {
        Vehicle.OnCellChanged -= HandleVehicleCellChanged;
        Registry.Remove(Vehicle);
        Destroy(Vehicle.gameObject);
    }

    private void HandleVehicleCellChanged(Vehicle Vehicle) => Registry.UpdateCell(Vehicle);
    /// <summary>
    /// Applies damage to a vehicle, handling destruction and ejecting the player if needed
    /// </summary>
    public bool DamageVehicle(Vehicle Vehicle, int damage)
    {
        if (Vehicle == null)
            return false;
        bool isDestroyed = Vehicle.DecreaseHealthBy(damage);
        if (isDestroyed)
            HandleVehicleDestroyed(Vehicle);
        return isDestroyed;
    }
    /// <summary>
    /// Ejects the player if inside, then destroys the vehicle. Used when a vehicle's
    /// health has already been depleted (e.g. by ramming an enemy)
    /// </summary>
    public void HandleVehicleDestroyed(Vehicle Vehicle)
    {
        if (Vehicle == null)
            return;
        if (Player.IsInVehicle
            && Player.Vehicle == Vehicle)
            Player.ExitVehicle();
        DestroyVehicle(Vehicle);
    }
    /// <summary>
    /// Destroys all vehicles except Player's vehicle
    /// </summary>
    public void DestroyAllVehicles(Vehicle ExcludedVehicle = null)
    {
        for (int i = Vehicles.Count - 1; i >= 0; i--)
        {
            Vehicle Vehicle = Vehicles[i];
            if (Vehicle != ExcludedVehicle)
                DestroyVehicle(Vehicle);
        }
    }
}
