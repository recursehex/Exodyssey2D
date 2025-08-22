using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VehicleManager : MonoBehaviour
{
    [SerializeField] private GameObject[] VehicleTemplates;
    public List<Vehicle> Vehicles { get; private set; } = new();
    [SerializeField] private int spawnVehicleCount;
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
        spawnVehicleCount = Random.Range(1, 3); // TEMP
        int cap = spawnVehicleCount * 2;
        while (cap > 0 && spawnVehicleCount > 0)
        {
            if (WeightedRarityGeneration.Generate<Vehicle>())
            {
                spawnVehicleCount--;
            }
            cap--;
        }
    }
    /// <summary>
    /// Spawns vehicle at specified position
    /// </summary>
    /// <param name="index"></param>
    /// <param name="Position"></param>
    public void SpawnVehicle(int index, Vector3 Position)
    {
        Vehicle Vehicle = Instantiate(VehicleTemplates[index], Position, Quaternion.identity).GetComponent<Vehicle>();
        Vehicle.Initialize(TilemapGround, TilemapWalls, new VehicleInfo(index));
        Vehicles.Add(Vehicle);
    }
    /// <summary>
    /// Returns true if a vehicle is at specified position
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
    public bool HasVehicleAtPosition(Vector3 Position) => Vehicles.Find(vehicle => vehicle.transform.position == Position) != null;
    /// <summary>
    /// Returns index of vehicle at specified position, or -1 if no vehicle is found
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
    public int GetVehicleIndexAtPosition(Vector3Int Position)
    {
        Vector3 ShiftedPosition = Position + new Vector3(0.5f, 0.5f);
        return Vehicles.FindIndex(Vehicle => Vehicle.transform.position == ShiftedPosition);
    }
    /// <summary>
    /// Destroys specified vehicle
    /// </summary>
    /// <param name="Vehicle"></param>
    public void DestroyVehicle(Vehicle Vehicle)
    {
        Destroy(Vehicle.gameObject);
        Vehicles.Remove(Vehicle);
    }
    /// <summary>
    /// Destroys all vehicles except Player's vehicle
    /// </summary>
    /// <param name="ExcludedVehicle"></param>
    public void DestroyAllVehicles(Vehicle ExcludedVehicle = null)
    {
        Vehicles.ForEach(Vehicle =>
        {
            if (Vehicle != ExcludedVehicle)
            {
                Destroy(Vehicle.gameObject);
            }
        });
        Vehicles.RemoveAll(Vehicle => Vehicle != ExcludedVehicle);
    }
}