using UnityEngine;
using UnityEngine.EventSystems;

public class RightClickDrop : MonoBehaviour, IPointerClickHandler
{
    public int idx = 0;
    public void OnPointerClick(PointerEventData eventData)
    {
        Player p = GameObject.Find("Player").GetComponent<Player>();

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            p.TryDropItem(idx);
        }
    }
}
