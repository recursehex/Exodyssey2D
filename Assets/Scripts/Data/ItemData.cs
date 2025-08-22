using System;
using System.Collections.Generic;

[Serializable] public class ItemData
{
    public string Tag;
    public string Rarity;
    public string Type;
    public string Name;
    public string Description;
    public int maxUses          = 1;
    public int damagePoints     = -1;
    public int armorDamage      = -1;
    public int range            = -1;
    public bool isEquipable     = false;
    public bool isAttachable    = false;
    public bool isFlammable     = false;
    public bool isStunning      = false;
    public bool disabled        = false;
}
[Serializable] public class ItemDatabase
{
    public List<ItemData> Items;
}