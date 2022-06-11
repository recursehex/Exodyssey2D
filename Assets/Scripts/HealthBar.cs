using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;

    float prevX = 0.0f;
    float prevY = 0.0f;

    public void SetMaxHealth(int hp)
    {
        slider.maxValue = hp;
        slider.value = hp;
    }

    public void SetHealth(int hp)
    {
        slider.value = hp;
    }

    public void MoveToPlace(Vector3 goal)
    {
        if (prevX != goal.x || prevY != goal.y)
        {
            prevX = goal.x;
            prevY = goal.y;
            Vector3 shiftedDst = new Vector3(goal.x + 0.5f, goal.y + 1.1f, goal.z);
            transform.position = shiftedDst;
        }
    }
}
