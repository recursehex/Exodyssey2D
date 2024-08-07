using UnityEngine;

public class ItemAssets : MonoBehaviour
{
	public static ItemAssets Instance { get; private set; }
	private void Awake()
	{
		Instance = this;
	}
	public Sprite MedKit;
	public Sprite Branch;
	public Sprite Knife;
	public Sprite Wrench;
	public Sprite DiamondChainsaw;
	public Sprite Tranquilizer;
	public Sprite HuntingRifle;
	public Sprite PlasmaRailgun;
	public Sprite Missing;
}
