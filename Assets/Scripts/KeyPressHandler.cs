using UnityEngine;
using UnityEngine.UI;

public class KeyPressHandler : MonoBehaviour
{
    public KeyCode PrimaryKey;
    public KeyCode SecondaryKey;
    private Button Button;

    void Awake()
    {
        Button = GetComponent<Button>();
    }

    void Update()
    {
        if ((Input.GetKeyDown(PrimaryKey) || Input.GetKeyDown(SecondaryKey)) && Button.interactable)
        {
            Button.onClick.Invoke();
        }
    }
}
