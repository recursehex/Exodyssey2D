using System;
using System.Collections.Generic;

[Serializable] public class RegionData
{
    public string Tag;
    public string Name;
    public int GridsRequired = 3;
    public string GroundTileSetName;
    public string WallTileSetName;
    public List<string> EnemyPool = new();
    public List<string> ItemPool = new();
    public List<string> VehiclePool = new();
    public string Description;
    public bool disabled = false;
}

[Serializable] public class RegionDatabase
{
    public List<RegionData> Regions;
}
