using System;
using System.Collections.Generic;
using UnityEngine;

public static class DefinitionLoader
{
	public static T Load<T>(string ResourcePath) where T : class
	{
		TextAsset JsonFile = Resources.Load<TextAsset>(ResourcePath);
		if (JsonFile == null)
		{
			Debug.LogError($"{ResourcePath}.json not found in Resources folder!");
			return null;
		}
		T Parsed = JsonUtility.FromJson<T>(JsonFile.text);
		if (Parsed == null)
			Debug.LogError($"Failed to parse {ResourcePath}.json");
		return Parsed;
	}

	public static List<TEntry> LoadEntries<TFile, TEntry>(
		string ResourcePath,
		Func<TFile, List<TEntry>> GetEntries) where TFile : class
	{
		TFile Parsed = Load<TFile>(ResourcePath);
		return Parsed == null ? null : GetEntries(Parsed);
	}

	public static Dictionary<TTag, TEntry> IndexByTag<TTag, TEntry>(
		IEnumerable<TEntry> Entries,
		Func<TEntry, string> GetTag) where TTag : struct, Enum
	{
		Dictionary<TTag, TEntry> Result = new();
		if (Entries == null)
			return Result;
		foreach (TEntry Entry in Entries)
		{
			if (Enum.TryParse(GetTag(Entry), out TTag Tag))
				Result[Tag] = Entry;
		}
		return Result;
	}
}
