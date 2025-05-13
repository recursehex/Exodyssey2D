using UnityEngine;
using System;
using System.Collections.Generic;

public struct Rarity
{
    public enum Tags
    {
        Common = 45,
        Limited = 35,
        Scarce = 15,
        Rare = 4,
        Anomalous = 1
    }
    public Tags Tag;
    public Color Color;
    private Rarity(Tags Tag, Color Color)
    {
        this.Tag = Tag;
        this.Color = Color;
    }
	public static readonly Rarity Common 	= new(Tags.Common, 		Color.white);
    public static readonly Rarity Limited 	= new(Tags.Limited, 	new(62/255f,  161/255f, 33/255f));
    public static readonly Rarity Scarce 	= new(Tags.Scarce, 		new(222/255f, 161/255f, 18/255f));
    public static readonly Rarity Rare 		= new(Tags.Rare, 		new(34/255f,  113/255f, 191/255f));
    public static readonly Rarity Anomalous = new(Tags.Anomalous, 	new(117/255f, 33/255f,  202/255f));
	public static readonly List<Rarity> RarityList = new() { Common, Limited, Scarce, Rare, Anomalous };
    public readonly int GetDropRate()
    {
        return (int)Tag;
    }
	public override bool Equals(object obj)
    {
        return obj is Rarity rarity && Equals(rarity);
    }
    public bool Equals(Rarity Other)
    {
        return Tag == Other.Tag && Color.Equals(Other.Color);
    }
    public readonly override int GetHashCode()
    {
        return HashCode.Combine(Tag, Color);
    }
    public static bool operator ==(Rarity Left, Rarity Right)
    {
        return Left.Equals(Right);
    }
    public static bool operator !=(Rarity Left, Rarity Right)
    {
        return !(Left == Right);
    }
}