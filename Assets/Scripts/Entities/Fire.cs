using UnityEngine;

/// <summary>
/// Represents a single tile of fire and basic lifecycle data
/// </summary>
public class Fire : MonoBehaviour
{
    [SerializeField] private Animator Animator;
    [SerializeField] private int remainingLifetime;
    public Vector3Int CellPosition { get; private set; }
    public bool IsWildfire { get; private set; }
    public void Initialize(Vector3Int Cell, bool isWildfire, int lifetime, Vector3 WorldPosition)
    {
        CellPosition = Cell;
        IsWildfire = isWildfire;
        remainingLifetime = lifetime;
        transform.position = WorldPosition;
        if (Animator == null)
            return;
        Animator.Play(0, 0, 0f);
    }
    /// <summary>
    /// Returns true if this fire should expire after this tick.
    /// Every tile counts down from the moment it is lit, so a wildfire burns out
    /// from its oldest tiles to its newest rather than all at once
    /// </summary>
    public bool ShouldExtinguishAfterTurn()
    {
        remainingLifetime--;
        return remainingLifetime <= 0;
    }
}
