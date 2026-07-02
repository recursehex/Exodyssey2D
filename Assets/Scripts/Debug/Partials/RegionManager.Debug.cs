#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// Cheat-menu-only setters for RegionManager. TryAdvanceRegion only steps forward
/// one region; this lets the cheat menu jump to any region directly.
/// </summary>
public partial class RegionManager
{
	public static int Debug_RegionCount => (int)RegionInfo.Tags.Unknown;

	/// <summary>
	/// Current region's display name for cheat-menu readouts.
	/// </summary>
	public string GetRegionName() => CurrentRegion != null ? CurrentRegion.Name : "?";

	/// <summary>
	/// Loads an arbitrary region by index, clamped to the valid range.
	/// </summary>
	public void Debug_SetRegion(int index)
	{
		index = Mathf.Clamp(index, 0, Debug_RegionCount - 1);
		LoadRegion(index);
		OnRegionChanged?.Invoke(CurrentRegion);
	}

	/// <summary>
	/// Maps a total number of completed grids onto the natural region progression
	/// (each region needs GridsRequired grids), setting the region and its progress.
	/// </summary>
	public void Debug_SetProgressFromTotalGrids(int totalGrids)
	{
		totalGrids = Mathf.Max(0, totalGrids);
		int index = 0;
		int remaining = totalGrids;
		while (index < Debug_RegionCount - 1)
		{
			int required = Mathf.Max(1, new RegionInfo(index).GridsRequired);
			if (remaining < required)
				break;
			remaining -= required;
			index++;
		}
		LoadRegion(index);
		CurrentRegion.GridsCompleted = remaining;
		OnRegionChanged?.Invoke(CurrentRegion);
	}
}
#endif
