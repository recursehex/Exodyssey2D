using System;
using System.Collections.Generic;
using UnityEngine;

public class StructureInfo
{
	public enum Tags
	{
		MedCrate = 0,
		Unknown,
	}
	public Tags Tag 			{ get; private set; } = Tags.Unknown;
	public string Name 			{ get; private set; }
	public string Description 	{ get; private set; }
	public int Width 			{ get; private set; } = 1;
	public int Height 			{ get; private set; } = 1;
	public bool IsInteractable 	{ get; private set; } = false;
	public bool IsLooted 		{ get; set; } = false;
	[Serializable] private class Entry
	{
		public string Tag, Name, Description;
		public int width = 1, height = 1;
		public bool isInteractable = false, disabled = false;
	}
	[Serializable] private class EntryList { public List<Entry> Structures; }
	private static List<Entry> Database;
	private static void LoadDatabase()
	{
		if (Database != null)
			return;
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/StructureDefinitions");
		if (JsonFile != null)
			Database = JsonUtility.FromJson<EntryList>(JsonFile.text).Structures;
		else
			Debug.LogError("StructureDefinitions.json not found in Resources folder!");
	}
	public StructureInfo(int n)
	{
		LoadDatabase();
		Tags TagData = (Tags)n;
		string TagName = TagData.ToString();
		if (Database != null)
		{
			Entry Entry = Database.Find(e => e.Tag == TagName);
			if (Entry != null && !Entry.disabled)
			{
				LoadFrom(Entry);
				return;
			}
			else if (Entry != null && Entry.disabled)
			{
				Debug.LogWarning($"Structure {n} {TagName} is disabled in JSON");
			}
		}
		Debug.LogWarning($"Structure {n} {TagName} not found in JSON, using default values");
		Tag 		= TagData;
		Name 		= TagName.ToUpper();
		Description = "Unknown structure";
	}
	private void LoadFrom(Entry Source)
	{
		Name 			= Source.Name;
		Description 	= Source.Description;
		Width 			= Source.width;
		Height 			= Source.height;
		IsInteractable 	= Source.isInteractable;
		Tag = Enum.TryParse(Source.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
	}
}
