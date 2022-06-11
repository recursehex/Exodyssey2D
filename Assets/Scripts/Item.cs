using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemInfo info;

    float prevX = 0.0f;
    float prevY = 0.0f;

    public Item()
    {
        info = new ItemInfo();
    }

    public void MoveToPlace(Vector3 goal)
    {
        if (prevX != goal.x || prevY != goal.y)
        {
            prevX = goal.x;
            prevY = goal.y;
            Vector3 shiftedDst = new Vector3(goal.x + 0.5f, goal.y + 0.5f, goal.z);
            transform.position = shiftedDst;
        }
    }
}
