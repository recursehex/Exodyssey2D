using UnityEngine;

public class ChangeSprite : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Sprite oldSprite;
    public Sprite newSprite;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    public void ChangeToNew()
    {
        spriteRenderer.sprite = newSprite;
    }

    public void ChangeToOld()
    {
        spriteRenderer.sprite = oldSprite;
    }
}
