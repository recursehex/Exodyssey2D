#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// Single source of truth for persistent cheat toggles. Production hot paths read
/// exactly one symbol per feature (e.g. CheatFlags.Invincibility) behind their own guard.
/// Entire class is stripped from release builds.
/// </summary>
public static class CheatFlags
{
	public static bool Invincibility;          // Player takes no damage
	public static bool InfiniteEnergy;   // Player never spends energy
	public static bool InvincibleEnemies;// Enemies take no damage
	public static bool FreezeEnemies;    // Enemies skip their movement turn
	public static bool RevealAll;        // Fog/visibility disabled, whole grid lit

	/// <summary>
	/// Clears every toggle. Called when a fresh run starts.
	/// </summary>
	public static void Reset()
	{
		Invincibility = false;
		InfiniteEnergy = false;
		InvincibleEnemies = false;
		FreezeEnemies = false;
		RevealAll = false;
	}
}
#endif
