using UnityEngine;

public class ChangeSprite : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;
    public Sprite OldSprite;
    public Sprite NewSprite;

    // Start is called before the first frame update
    void Start()
    {
        SpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    public void ChangeToNew()
    {
        SpriteRenderer.sprite = NewSprite;
    }

    public void ChangeToOld()
    {
        SpriteRenderer.sprite = OldSprite;
    }
}
