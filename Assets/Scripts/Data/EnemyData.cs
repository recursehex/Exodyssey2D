using System;
using System.Collections.Generic;

[Serializable]
public class EnemyData
{
    public string Tag;
    public string Rarity;
    public string Type;
    public string Name;
    public string Description;
    public int maxHealth = 1;
    public int maxEnergy = 1;
    public int speed = 2;
    public int damagePoints = -1;
    public int range = 0;
    public bool isHunting = true;
    public bool isArmored = false;
    public bool disabled = false;
}

[Serializable]
public class EnemyDatabase
{
    public List<EnemyData> Enemies;
}