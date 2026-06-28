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
	private static readonly Dictionary<ItemInfo.Tags, Sprite> SpriteCache = new();
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
	public static Sprite GetSpriteForInfo(ItemInfo ItemInfo)
	{
		if (ItemInfo == null)
			return null;
		return GetSpriteForTag(ItemInfo.Tag);
	}
	public static Sprite GetSpriteForTag(ItemInfo.Tags Tag)
	{
		if (SpriteCache.TryGetValue(Tag, out Sprite CachedSprite))
			return CachedSprite;
		string ResourcePath = $"Sprites/{Tag.ToString().ToLowerInvariant()}";
		Sprite Sprite = Resources.Load<Sprite>(ResourcePath);
		if (Sprite != null)
			SpriteCache[Tag] = Sprite;
		else
			Debug.LogWarning($"Sprite not found at path {ResourcePath} for Item Tag {Tag}.");
		return Sprite;
	}
}
