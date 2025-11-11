using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public bool IsPlayersTurn { get; private set; } = true;
    public TurnTimer TurnTimer;
    public Button EndTurnButton;    
    private bool isEndTurnButtonLocked;
    private bool desiredEndTurnButtonInteractable = true;
    public delegate void TurnEndedDelegate();
    public event TurnEndedDelegate OnPlayerTurnEnded;
    public event TurnEndedDelegate OnEnemyTurnEnded;
    public void Initialize(TurnTimer TurnTimer, Button EndTurnButton)
    {
        this.TurnTimer      = TurnTimer;
        this.EndTurnButton  = EndTurnButton;
    }
    public void StopTurnTimer() => TurnTimer.StopTimer();
    /// <summary>
    /// Resets timer and switches to enemy turn
    /// </summary>
    public void OnEndTurnPress()
    {
        SetEndTurnButtonInteractable(false);
        TurnTimer.timerIsRunning = false;
        TurnTimer.ResetTimer();
        // Check if there are enemies before changing turns
        if (GameManager.Instance.HasEnemies())
            IsPlayersTurn = false;
        // No enemies, stay on player's turn and re-enable button
        else
            SetEndTurnButtonInteractable(true);
        OnPlayerTurnEnded?.Invoke();
    }
    /// <summary>
    /// Sets EndTurnButton interactable
    /// </summary>
    public void OnTurnTimerEnd() => SetEndTurnButtonInteractable(true);
    /// <summary>
    /// Switches to Player's turn
    /// </summary>
    public void EndEnemyTurn()
    {
        TurnTimer.ResetTimer();
        IsPlayersTurn = true;
        SetEndTurnButtonInteractable(true);
        OnEnemyTurnEnded?.Invoke();
    }
    /// <summary>
    /// Sets desired interactable state for EndTurnButton
    /// </summary>
    public void SetEndTurnButtonInteractable(bool interactable)
    {
        desiredEndTurnButtonInteractable = interactable;
        ApplyEndTurnButtonState();
    }
    /// <summary>
    /// Locks or unlocks EndTurnButton
    /// </summary>
    /// <param name="locked"></param>
    public void SetEndTurnButtonLock(bool locked)
    {
        isEndTurnButtonLocked = locked;
        ApplyEndTurnButtonState();
    }
    /// <summary>
    /// Applies current lock and desired interactable state to EndTurnButton
    /// </summary>
    private void ApplyEndTurnButtonState()
    {
        EndTurnButton.interactable = !isEndTurnButtonLocked && desiredEndTurnButtonInteractable;
    }
    /// <summary>
    /// Resets turn flow to default state for a new run
    /// </summary>
    public void ResetTurnState()
    {
        IsPlayersTurn = true;
        TurnTimer.timerIsRunning = false;
        TurnTimer.ResetTimer();
        desiredEndTurnButtonInteractable = true;
        ApplyEndTurnButtonState();
    }
}