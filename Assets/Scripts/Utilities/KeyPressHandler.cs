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

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Suppress all button hotkeys while the cheat menu is open or placing
        if (CheatMenu.IsOpen || CheatMenu.IsPlacing)
            return;
#endif
        if (!Button.interactable)
            return;
        if (Action == null)
        {
            if (InputSystem.actions == null)
                return;
            Action = InputSystem.actions.FindAction(ActionName);
        }
        if (Action != null && Action.WasPressedThisFrame())
            Button.onClick.Invoke();
    }
}