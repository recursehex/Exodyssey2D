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
    [SerializeField] private int spawnRetryMultiplier = 2;
    private Tilemap TilemapGround;
    private Tilemap TilemapWalls;
    public void Initialize(Tilemap Ground, Tilemap Walls, GameObject[] Templates)
    {
        TilemapGround       = Ground;
        TilemapWalls        = Walls;
        VehicleTemplates    = Templates;
    }
    public void GenerateVehicles()
    {
        spawnVehicleCount = Random.Range(minSpawnCount, maxSpawnCountExclusive);
        int cap = spawnVehicleCount * spawnRetryMultiplier;
        while (cap > 0 && spawnVehicleCount > 0)
        {
            if (WeightedRarityGeneration.Generate<Vehicle>())
                spawnVehicleCount--;
            cap--;
        }
    }
    /// <summary>
    /// Spawns vehicle at specified position
    /// </summary>
    public Vehicle SpawnVehicle(int index, Vector3 Position)
    {
        Vehicle Vehicle = Instantiate(VehicleTemplates[index], Position, Quaternion.identity).GetComponent<Vehicle>();
        Vehicle.Initialize(TilemapGround, TilemapWalls, new VehicleInfo(index));
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
