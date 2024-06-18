using UnityEngine;

public class ItemAssets : MonoBehaviour
{
	public static ItemAssets Instance { get; private set; }
	private void Awake()
	{
		Instance = this;
	}
	public Sprite medKit;
	public Sprite branch;
	public Sprite knife;
	public Sprite wrench;
	public Sprite diamondChainsaw;
	public Sprite huntingRifle;
	public Sprite plasmaRailgun;
	public Sprite missing;
}
