using UnityEngine;

public class TileArea : MonoBehaviour
{
    float prevX = 0.0f;
    float prevY = 0.0f;

    public void MoveToPlace(Vector3 goal)
    {
        if (prevX != goal.x || prevY != goal.y)
        {
            prevX = goal.x;
            prevY = goal.y;
            Vector3 shiftedDst = new(goal.x + 0.5f, goal.y + 0.5f, goal.z);
            transform.position = shiftedDst;
        }
    }
}