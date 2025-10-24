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
		if (SpriteCache.TryGetValue(Info.Tag, out Sprite CachedSprite))
		{
			return CachedSprite;
		}
		string ResourcePath = $"Sprites/{Info.Tag.ToString().ToLowerInvariant()}";
		Sprite Sprite = Resources.Load<Sprite>(ResourcePath);
		if (Sprite != null)
		{
			SpriteCache[Info.Tag] = Sprite;
		}
		else
		{
			Debug.LogWarning($"Sprite not found at path {ResourcePath} for Item Tag {Info.Tag}.");
		}
		return Sprite;
	}
}