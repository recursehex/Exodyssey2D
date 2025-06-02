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
        TilemapGround = Ground;
        TilemapWalls = Walls;
        VehicleTemplates = Templates;
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
    public void SpawnVehicle(int index, Vector3 Position)
    {
        Vehicle Vehicle = Instantiate(VehicleTemplates[index], Position, Quaternion.identity).GetComponent<Vehicle>();
        Vehicle.Initialize(TilemapGround, TilemapWalls, new VehicleInfo(index));
        Vehicles.Add(Vehicle);
    }
    public bool HasVehicleAtPosition(Vector3 Position)
    {
        return Vehicles.Find(vehicle => vehicle.transform.position == Position) != null;
    }
    
    public int GetVehicleIndexAtPosition(Vector3Int Position)
    {
        Vector3 ShiftedPosition = Position + new Vector3(0.5f, 0.5f, 0);
        return Vehicles.FindIndex(Vehicle => Vehicle.transform.position == ShiftedPosition);
    }
    
    public void DestroyVehicle(Vehicle Vehicle)
    {
        Destroy(Vehicle.gameObject);
        Vehicles.Remove(Vehicle);
    }
    
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