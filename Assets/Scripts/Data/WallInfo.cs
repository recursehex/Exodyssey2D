using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-driven metadata for wall tiles, keyed by sprite name.
/// Defines which walls are flammable, choppable (and by which tool), and explodable,
/// plus the item dropped when chopped or exploded. Loaded from WallDefinitions.json.
/// </summary>
public class WallInfo
{
	public string Name 				{ get; private set; }
	public bool IsFlammable 		{ get; private set; } = false;
	public bool IsExplodable 		{ get; private set; } = false;
	public bool IsChoppable 		{ get; private set; } = false;
	public ItemInfo.Tags ChopTool 	{ get; private set; } = ItemInfo.Tags.Unknown;	// Tool that can chop this wall
	public ItemInfo.Tags DropItem 	{ get; private set; } = ItemInfo.Tags.Unknown;	// Item dropped when chopped or exploded
	[Serializable] private class Entry
	{
		public string Name, ChopTool, DropItem;
		public bool isFlammable = false, isExplodable = false, isChoppable = false, disabled = false;
	}
	[Serializable] private class EntryList { public List<Entry> Walls; }
	private static Dictionary<string, WallInfo> Database;
	private static void LoadDatabase()
	{
		if (Database != null)
			return;
		Database = new();
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/WallDefinitions");
		if (JsonFile == null)
		{
			Debug.LogError("WallDefinitions.json not found in Resources folder!");
			return;
		}
		List<Entry> Entries = JsonUtility.FromJson<EntryList>(JsonFile.text).Walls;
		foreach (Entry Entry in Entries)
		{
			if (Entry.disabled || string.IsNullOrEmpty(Entry.Name))
				continue;
			Database[Entry.Name] = new WallInfo(Entry);
		}
	}
	private WallInfo(Entry Source)
	{
		Name 			= Source.Name;
		IsFlammable 	= Source.isFlammable;
		IsExplodable 	= Source.isExplodable;
		IsChoppable 	= Source.isChoppable;
		ChopTool = Enum.TryParse(Source.ChopTool, out ItemInfo.Tags ParsedChop) ? ParsedChop : ItemInfo.Tags.Unknown;
		DropItem = Enum.TryParse(Source.DropItem, out ItemInfo.Tags ParsedDrop) ? ParsedDrop : ItemInfo.Tags.Unknown;
	}
	/// <summary>
	/// Returns the wall definition matching a tile/sprite name, or null if none exists
	/// </summary>
	public static WallInfo Get(string Name)
	{
		if (string.IsNullOrEmpty(Name))
			return null;
		LoadDatabase();
		return Database.TryGetValue(Name, out WallInfo Info) ? Info : null;
	}
}
