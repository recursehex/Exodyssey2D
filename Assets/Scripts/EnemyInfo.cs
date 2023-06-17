using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum EnemyTag
{
    // WEAK
    Crawler = 0,

    // MEDIOCRE
    //Spawner,
    Launcher,

    // STRONG
    //Beast,

    // EXOTIC
    //OvergrownScanner,

    Unknown,
}

public enum EnemyType
{
    Weak = 0,
    Mediocre,
    Strong,
    Exotic,

    Unknown,
}

public class EnemyInfo
{
    public EnemyTag tag;                        // Name of enemy
    public Rarity rarity;                       // Rarity of enemy
    public EnemyType type;                      // Type of enemy

    public string name;                         // Ingame name of enemy
    public string description;                  // Ingame desc of enemy

    public int maxHP = 1;
    public int currentHP = 1;

    public int maxAP = 1;
    public int currentAP = 1;

    public int damagePoints = -1;               // Set only for enemies that do direct attacks
    public int range = 0;                       // Maximum distance a Ranged enemy can attack to, 0 = Melee

    public bool isHunting = true;               // true = will hunt the player, false = will guard nearby items
    public bool isShelled = false;              // false = will not have resistance to Steel Beam and Mallet, true = will have resistance and will be vulnerable to Axe, Honed Gavel, and Shell Piercer

    public static int lastEnemyIdx = (int)EnemyTag.Unknown;

    static public List<Rarity> GenerateAllRarities()
    {
        List<Rarity> ret = new();

        for (int i = 0; i < lastEnemyIdx; i++)
        {
            EnemyInfo enemy = EnemyFactory(i);
            ret.Add(enemy.rarity);
        }

        return ret;
    }

    /// <summary>
    /// Returns the percentage for a desired rarity
    /// </summary>
    /// <returns></returns>
    public static Dictionary<Rarity, int> RarityPercentMap()
    {
        Dictionary<Rarity, int> RarityToPercentage = new()
        {
            [Rarity.Common] = 35,
            [Rarity.Limited] = 30,
            [Rarity.Scarce] = 20,
            [Rarity.Rare] = 10,
            [Rarity.Numinous] = 4,
            [Rarity.Secret] = 1,
        };
        return RarityToPercentage;
    }

    /// <summary>
    /// Returns info for a desired enemy 
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static EnemyInfo EnemyFactory(int n)
    {
        EnemyInfo inf = new();

        switch (n)
        {
            case 0:
                inf.tag = EnemyTag.Crawler;
                inf.rarity = Rarity.Common;
                inf.type = EnemyType.Weak;
                inf.maxHP = 2;
                inf.currentHP = inf.maxHP;
                inf.maxAP = 1;
                inf.currentAP = inf.maxAP;
                inf.damagePoints = 1;
                inf.name = "CRAWLER";
                inf.description = "";
                inf.isHunting = true;
                break;
            case 1:
                inf.tag = EnemyTag.Launcher;
                inf.rarity = Rarity.Limited;
                inf.type = EnemyType.Mediocre;
                inf.maxHP = 2;
                inf.currentHP = inf.maxHP;
                inf.maxAP = 2;
                inf.currentAP = inf.maxAP;
                inf.damagePoints = 2;
                inf.range = 3;
                inf.name = "LAUNCHER";
                inf.description = "";
                inf.isHunting = false;
                break;
        }
        return inf;
    }
}
