using UnityEngine;

/// <summary>
/// Represents a single tile of fire and basic lifecycle data.
/// </summary>
public class Fire : MonoBehaviour
{
    [SerializeField] private Animator Animator;
    [SerializeField] private int remainingLifetime;
    public Vector3Int CellPosition { get; private set; }
    public bool IsWildfire { get; private set; }
    public void Initialize(Vector3Int cell, bool isWildfire, int lifetime, Vector3 worldPosition)
    {
        CellPosition = cell;
        IsWildfire = isWildfire;
        remainingLifetime = lifetime;
        transform.position = worldPosition;
        // Restart animation so newly spawned fires always play from the start.
        if (Animator != null)
            Animator.Play(0, 0, 0f);
    }
    /// <summary>
    /// Returns true if this fire should expire after this tick.
    /// Wildfire tiles never expire naturally.
    /// </summary>
    public bool ShouldExtinguishAfterTurn()
    {
        if (IsWildfire)
            return false;
        remainingLifetime = Mathf.Max(remainingLifetime - 1, 0);
        return remainingLifetime <= 0;
    }
}