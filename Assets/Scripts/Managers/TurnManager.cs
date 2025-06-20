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
    public void StopTurnTimer()
    {
        TurnTimer.StopTimer();
    }
    public void OnEndTurnPress()
    {
        EndTurnButton.interactable = false;
        TurnTimer.timerIsRunning = false;
        TurnTimer.ResetTimer();
        // Check if there are enemies before changing turns
        if (GameManager.Instance.EnemyManager.Enemies.Count > 0)
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
    public void OnTurnTimerEnd()
    {
        EndTurnButton.interactable = true;
    }
    public void EndEnemyTurn()
    {
        TurnTimer.ResetTimer();
        IsPlayersTurn = true;
        EndTurnButton.interactable = true;
        OnEnemyTurnEnded?.Invoke();
    }
    public void StartPlayerTurn()
    {
        IsPlayersTurn = true;
        EndTurnButton.interactable = true;
    }
    public void SetEndTurnButtonInteractable(bool interactable)
    {
        EndTurnButton.interactable = interactable;
    }
}