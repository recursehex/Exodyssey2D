using System;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
	[NonSerialized] public ItemInfo Info;
	[Header("Debug")]
	[SerializeField] private ItemInfo.Tags ItemTag = ItemInfo.Tags.Unknown;
	[SerializeField] private ItemInfo.Types ItemType = ItemInfo.Types.Unknown;
	[SerializeField] private string ItemName = string.Empty;
	[SerializeField] private string ItemStats = string.Empty;
	[SerializeField] private int currentUses = 0;
	private static readonly Dictionary<string, Sprite> SpriteCache = new();
#if UNITY_EDITOR
	private void LateUpdate()
	{
		SyncDebugFields();
	}
	private void SyncDebugFields()
	{
		if (Info == null)
		{
			ItemTag = ItemInfo.Tags.Unknown;
			ItemType = ItemInfo.Types.Unknown;
			ItemName = string.Empty;
			ItemStats = string.Empty;
			currentUses = 0;
			return;
		}
		ItemTag = Info.Tag;
		ItemType = Info.Type;
		ItemName = Info.Name;
		ItemStats = Info.Stats;
		currentUses = Info.CurrentUses;
	}
#endif
	public Sprite GetSprite()
	{
		return GetSpriteForInfo(Info);
	}
	/// <summary>
	/// Updates this ground item's renderer to match its current Info
	/// (e.g. shows the empty sprite when dropped while depleted)
	/// </summary>
	public void RefreshSprite()
	{
		Sprite Sprite = GetSprite();
		if (Sprite != null && TryGetComponent(out SpriteRenderer Renderer))
			Renderer.sprite = Sprite;
	}
	public static Sprite GetSpriteForInfo(ItemInfo ItemInfo)
	{
		if (ItemInfo == null)
			return null;
		string ResourceName = ItemInfo.Tag.ToString().ToLowerInvariant();
		// Depleted items with an alternate sprite use the "_empty" variant
		if (ItemInfo.IsDepleted)
			ResourceName += "_empty";
		return GetSpriteForResource(ResourceName);
	}
	public static Sprite GetSpriteForTag(ItemInfo.Tags Tag)
	{
		return GetSpriteForResource(Tag.ToString().ToLowerInvariant());
	}
	private static Sprite GetSpriteForResource(string ResourceName)
	{
		if (SpriteCache.TryGetValue(ResourceName, out Sprite CachedSprite))
			return CachedSprite;
		string ResourcePath = $"Sprites/{ResourceName}";
		Sprite Sprite = Resources.Load<Sprite>(ResourcePath);
		if (Sprite != null)
			SpriteCache[ResourceName] = Sprite;
		else
			Debug.LogWarning($"Sprite not found at path {ResourcePath}.");
		return Sprite;
	}
}
