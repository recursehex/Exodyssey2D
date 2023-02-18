using UnityEngine;
using UnityEngine.UI;

public class KeyPressHandler : MonoBehaviour
{
    public KeyCode key;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Update()
    {
        if (Input.GetKeyDown(key) && button.interactable)
        {
            button.onClick.Invoke();
        }
    }
}
