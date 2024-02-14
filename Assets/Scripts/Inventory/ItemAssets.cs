using UnityEngine;

public class ItemAssets : MonoBehaviour
{
    public static ItemAssets Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    public Sprite medKit;
    public Sprite plasmaRailgun;
    public Sprite branch;
    public Sprite diamondChainsaw;
    public Sprite huntingRifle;
    public Sprite knife;
    public Sprite missing;
}
