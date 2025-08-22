using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public bool IsPlayersTurn { get; private set; } = true;
    public TurnTimer TurnTimer;
    public Button EndTurnButton;    
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
        EndTurnButton.interactable = false;
        TurnTimer.timerIsRunning = false;
        TurnTimer.ResetTimer();
        // Check if there are enemies before changing turns
        if (GameManager.Instance.HasEnemies())
        {
            IsPlayersTurn = false;
        }
        else
        {
            // No enemies, stay on player's turn and re-enable button
            EndTurnButton.interactable = true;
        }
        OnPlayerTurnEnded?.Invoke();
    }
    /// <summary>
    /// Sets EndTurnButton interactable
    /// </summary>
    public void OnTurnTimerEnd() => EndTurnButton.interactable = true;
    /// <summary>
    /// Switches to Player's turn
    /// </summary>
    public void EndEnemyTurn()
    {
        TurnTimer.ResetTimer();
        IsPlayersTurn = true;
        EndTurnButton.interactable = true;
        OnEnemyTurnEnded?.Invoke();
    }
    public void SetEndTurnButtonInteractable(bool interactable) => EndTurnButton.interactable = interactable;
}