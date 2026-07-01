using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VehicleManager : MonoBehaviour
{
    [SerializeField] private GameObject[] VehicleTemplates;
    public List<Vehicle> Vehicles { get; private set; } = new();
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
        Vehicles.Add(Vehicle);
        return Vehicle;
    }
    /// <summary>
    /// Returns true if a vehicle is at specified position
    /// </summary>
    public bool HasVehicleAtPosition(Vector3 Position) => Vehicles.Find(vehicle => vehicle.transform.position == Position) != null;
    /// <summary>
    /// Returns vehicle at specified position, or null if no vehicle is found
    /// </summary>
    public Vehicle GetVehicleAtPosition(Vector3Int Position)
    {
        Vector3 ShiftedPosition = Position + new Vector3(0.5f, 0.5f);
        return Vehicles.Find(Vehicle => Vehicle.transform.position == ShiftedPosition);
    }
    /// <summary>
    /// Destroys specified vehicle
    /// </summary>
    public void DestroyVehicle(Vehicle Vehicle)
    {
        Destroy(Vehicle.gameObject);
        Vehicles.Remove(Vehicle);
    }
    /// <summary>
    /// Applies damage to a vehicle, handling destruction and ejecting the player if needed
    /// </summary>
    public bool DamageVehicle(Vehicle Vehicle, int damage)
    {
        if (Vehicle == null)
            return false;
        bool isDestroyed = Vehicle.DecreaseHealthBy(damage);
        if (isDestroyed)
        {
            if (Player.IsInVehicle
                && Player.Vehicle == Vehicle)
                Player.ExitVehicle();
            DestroyVehicle(Vehicle);
        }
        return isDestroyed;
    }
    /// <summary>
    /// Destroys all vehicles except Player's vehicle
    /// </summary>
    public void DestroyAllVehicles(Vehicle ExcludedVehicle = null)
    {
        Vehicles.ForEach(Vehicle =>
        {
            if (Vehicle != ExcludedVehicle)
                Destroy(Vehicle.gameObject);
        });
        Vehicles.RemoveAll(Vehicle => Vehicle != ExcludedVehicle);
    }
}
