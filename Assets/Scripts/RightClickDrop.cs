using UnityEngine;
using UnityEngine.EventSystems;

public class RightClickDrop : MonoBehaviour, IPointerClickHandler
{
    public int index = 0;
    public void OnPointerClick(PointerEventData EventData)
    {
        Player Player = GameObject.Find("Player").GetComponent<Player>();

        if (EventData.button == PointerEventData.InputButton.Right)
        {
            Player.TryDropItem(index);
        }
    }
}
