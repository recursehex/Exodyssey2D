using UnityEngine;

public class TileDot : MonoBehaviour
{
    float previousX = 0.0f;
    float previousY = 0.0f;

    public void MoveToPlace(Vector3 Goal)
    {
        if (previousX != Goal.x || previousY != Goal.y)
        {
            previousX = Goal.x;
            previousY = Goal.y;
            Vector3 ShiftedDistance = new(Goal.x + 0.5f, Goal.y + 0.5f, Goal.z);
            transform.position = ShiftedDistance;
        }
    }
}