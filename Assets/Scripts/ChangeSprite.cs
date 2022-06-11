using System.Collections;
using System.Collections.Generic;
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

    // Update is called once per frame
    void Update()
    {

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
