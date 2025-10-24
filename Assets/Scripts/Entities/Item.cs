using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
	public ItemInfo Info;
	private static readonly Dictionary<ItemInfo.Tags, Sprite> SpriteCache = new();
	public Sprite GetSprite()
	{
		if (Info == null)
		{
			return null;
		}
		if (SpriteCache.TryGetValue(Info.Tag, out Sprite cachedSprite))
		{
			return cachedSprite;
		}
		string resourcePath = $"Sprites/{Info.Tag.ToString().ToLowerInvariant()}";
		Sprite sprite = Resources.Load<Sprite>(resourcePath);
		if (sprite != null)
		{
			SpriteCache[Info.Tag] = sprite;
		}
		else
		{
			Debug.LogWarning($"Sprite not found at path {resourcePath} for Item Tag {Info.Tag}.");
		}
		return sprite;
	}
}