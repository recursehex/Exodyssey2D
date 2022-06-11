using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableObject : MonoBehaviour, IPointerClickHandler
{
    public int idx = 0;

    public void OnPointerClick(PointerEventData eventData)
    {
        Player p = GameObject.Find("Player").GetComponent<Player>();

        if (eventData.button == PointerEventData.InputButton.Left) // uses item
        {
            p.TryUseItem(idx);
        }
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {

        }
        else if (eventData.button == PointerEventData.InputButton.Right) // drops item
        {
            p.TryDropItem(idx);
        }
    }
}
