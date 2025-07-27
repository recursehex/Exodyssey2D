using System;
using System.Collections.Generic;

[Serializable]
public class VehicleData
{
    public string Tag;
    public string Rarity;
    public string Type;
    public string Name;
    public string Description;
    public int efficiency = 1;
    public float speed = 2f;
    public int movementRange = 3;
    public int storage = 1;
    public int maxCharge = 1;
    public int maxHealth = 1;
    public bool canOffroad = false;
    public bool hasBattery = false;
    public bool hasSpotlight = false;
}

[Serializable]
public class VehicleDatabase
{
    public List<VehicleData> Vehicles;
}