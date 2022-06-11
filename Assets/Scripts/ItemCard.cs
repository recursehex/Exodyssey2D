using UnityEngine;

public class ItemCard : MonoBehaviour
{
    float prevX = 0.0f;
    float prevY = 0.0f;

    public void MoveToPlace(Vector3 goal)
    {
        if (prevX != goal.x || prevY != goal.y)
        {
            prevX = goal.x;
            prevY = goal.y;
            Vector3 shiftedDst = new Vector3(goal.x , goal.y , goal.z);
            transform.position = shiftedDst;
        }
    }
}
