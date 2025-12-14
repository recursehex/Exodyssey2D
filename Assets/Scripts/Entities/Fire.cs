using UnityEngine;

/// <summary>
/// Represents a single tile of fire and basic lifecycle data
/// </summary>
public class Fire : MonoBehaviour
{
    [SerializeField] private Animator Animator;
    [SerializeField] private int remainingLifetime;
    [SerializeField] private bool hasGuaranteedFirstSpread;
    public Vector3Int CellPosition { get; private set; }
    public bool IsWildfire { get; private set; }
    public void Initialize(Vector3Int Cell, bool isWildfire, int lifetime, Vector3 WorldPosition)
    {
        CellPosition = Cell;
        IsWildfire = isWildfire;
        remainingLifetime = lifetime;
        hasGuaranteedFirstSpread = !isWildfire;
        transform.position = WorldPosition;
        if (Animator == null)
            return;
        Animator.Play(0, 0, 0f);
        Animator.Update(0f);
    }
    /// <summary>
    /// Returns true if this fire should expire after this tick.
    /// Wildfire tiles never expire naturally
    /// </summary>
    public bool ShouldExtinguishAfterTurn()
    {
        if (IsWildfire)
            return false;
        remainingLifetime--;
        return remainingLifetime <= 0;
    }
    /// <summary>
    /// Returns true once for manmade fires that should guarantee an initial spread
    /// </summary>
    public bool ConsumeGuaranteedSpread()
    {
        if (!hasGuaranteedFirstSpread)
            return false;
        hasGuaranteedFirstSpread = false;
        return true;
    }
}
