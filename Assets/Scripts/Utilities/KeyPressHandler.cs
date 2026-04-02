using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyPressHandler : MonoBehaviour
{
    public string ActionName; // e.g. "Player/EndTurn"
    private Button Button;
    private InputAction Action;

    void Awake()
    {
        Button = GetComponent<Button>();
    }

    void Start()
    {
        Action = InputSystem.actions.FindAction(ActionName);
    }

    void Update()
    {
        if (!Button.interactable)
            return;
        if (Action != null && Action.WasPressedThisFrame())
            Button.onClick.Invoke();
    }
}