using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum EnemyTag
{
    // WEAK
    Crawler = 0,

    // MEDIOCRE
    //Spawner,

    // STRONG
    //Beast,

    // EXOTIC
    //OvergrownScanner,

    // BOSS
    //Biolith,

    Unknown,
}
public enum EnemyRarity
{
    Common = 0, // white
    Limited,    // green
    Scarce,     // yellow
    Rare,       // blue
    Numinous,   // purple
    Secret,     // red

    Unknown,
}

public enum EnemyType
{
    Weak = 0,
    Mediocre,
    Strong,
    Exotic,
    Boss,

    Unknown,
}

public class EnemyInfo
{
    public EnemyTag tag;                        // Name of enemy
    public EnemyRarity rarity;                  // Rarity of enemy
    public EnemyType type;                      // Type of enemy

    public string name;                         // Ingame name of enemy
    public string description;                  // Ingame desc of enemy

    public int maxHP = 1;
    public int currentHP = 1;

    public int maxAP = 1;
    public int currentAP = 1;

    public int damagePoints = -1;               // Set only for enemies that do direct attacks
    public bool isRanged = false;               // false = Melee, true = Ranged
    public int range = -1;                      // Maximum distance a Ranged enemy can attack to

    public bool isShelled = false;              // false = will not have resistance to Steel Beam and Mallet, true = will have resistance to those weapons and will be affected by Axe, Honed Gavel, and Shell Piercer

    public static int lastEnemyIdx = (int)EnemyTag.Unknown;

    /// <summary>
    /// Returns the percentage for a desired rarity
    /// </summary>
    /// <returns></returns>
    public static Dictionary<EnemyRarity, int> FillRarityNamestoPercentageMap()
    {
        Dictionary<EnemyRarity, int> RarityToPercentage = new Dictionary<EnemyRarity, int>
        {
            [EnemyRarity.Common] = 35,
            [EnemyRarity.Limited] = 30,
            [EnemyRarity.Scarce] = 20,
            [EnemyRarity.Rare] = 10,
            [EnemyRarity.Numinous] = 4,
            [EnemyRarity.Secret] = 1,
        };
        return RarityToPercentage;
    }

    /// <summary>
    /// Returns the info for a desired enemy 
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static EnemyInfo EnemyFactoryFromNumber(int n)
    {
        EnemyInfo inf = new();

        switch (n)
        {
            case 0:
                inf.tag = EnemyTag.Crawler;
                inf.rarity = EnemyRarity.Common;
                inf.type = EnemyType.Weak;
                inf.maxHP = 1;
                inf.currentHP = inf.maxHP;
                inf.maxAP = 1;
                inf.currentAP = inf.maxAP;
                inf.damagePoints = 1;
                inf.name = "CRAWLER";
                inf.description = "";
                break;
        }
        return inf;
    }
}
