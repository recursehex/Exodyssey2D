using UnityEngine;

public class Item : MonoBehaviour
{
	public ItemInfo Info;
	// Item sprite name must be lowercase of Tag
	public Sprite GetSprite()
	{
		return Resources.Load<Sprite>($"Sprites/{Info.Tag.ToString().ToLower()}");
	}
}