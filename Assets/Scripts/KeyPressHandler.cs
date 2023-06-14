using UnityEngine;
using UnityEngine.UI;

public class KeyPressHandler : MonoBehaviour
{
    public KeyCode primaryKey;
    public KeyCode secondaryKey;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Update()
    {
        if ((Input.GetKeyDown(primaryKey) || Input.GetKeyDown(secondaryKey)) && button.interactable)
        {
            button.onClick.Invoke();
        }
    }
}
