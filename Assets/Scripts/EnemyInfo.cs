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

    public int maxHealth = 1;
    public int currentHealth = 1;

    public int maxEnergy = 1;
    public int currentEnergy = 1;

    public int damagePoints = -1;               // Set only for enemies that do direct attacks
    public int range = 0;                       // Maximum distance a Ranged enemy can attack to, 0 = Melee

    public bool isHunting = true;               // true = will hunt the player, false = will guard nearby items
    public bool isShelled = false;              // false = will not have resistance to Steel Beam and Mallet, true = will have resistance and will be vulnerable to Axe, Honed Gavel, and Shell Piercer

    public static int lastEnemyIndex = (int)EnemyTag.Unknown;

    static public List<Rarity> GenerateAllRarities()
    {
        List<Rarity> enemyRarityList = new();
        for (int i = 0; i < lastEnemyIndex; i++)
        {
            EnemyInfo enemy = EnemyFactory(i);
            enemyRarityList.Add(enemy.rarity);
        }
        return enemyRarityList;
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
            [Rarity.Anomalous] = 5,
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
                inf.maxHealth = 2;
                inf.currentHealth = inf.maxHealth;
                inf.maxEnergy = 1;
                inf.currentEnergy = inf.maxEnergy;
                inf.damagePoints = 1;
                inf.name = "CRAWLER";
                inf.description = "";
                inf.isHunting = true;
                break;
            case 1:
                inf.tag = EnemyTag.Launcher;
                inf.rarity = Rarity.Limited;
                inf.type = EnemyType.Mediocre;
                inf.maxHealth = 4;
                inf.currentHealth = inf.maxHealth;
                inf.maxEnergy = 2;
                inf.currentEnergy = inf.maxEnergy;
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
