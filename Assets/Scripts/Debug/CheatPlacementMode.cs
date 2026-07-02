#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Click-to-place mode for the cheat menu. Left-click places at the hovered tile,
/// right-click or Escape cancels, hold Shift to exit after a single placement.
/// Validity reuses CheatActions' existing collision queries — no parallel rules.
/// </summary>
public class CheatPlacementMode
{
	public enum Kind { None, Item, Enemy, Vehicle, Fire, Wildfire, Teleport }

	private readonly CheatActions Actions;
	public Kind Active { get; private set; } = Kind.None;
	public bool IsArmed => Active != Kind.None;
	public Vector3Int TargetCell { get; private set; }
	public bool TargetValid { get; private set; }

	private int payloadIndex;
	private int fuel = -1;

	public CheatPlacementMode(CheatActions Actions) => this.Actions = Actions;

	public void Arm(Kind Mode, int index = 0, int fuel = -1)
	{
		Active = Mode;
		payloadIndex = index;
		this.fuel = fuel;
	}

	public void Cancel() => Active = Kind.None;

	public string Label => Active switch
	{
		Kind.Item     => $"Placing item {(ItemInfo.Tags)payloadIndex}",
		Kind.Enemy    => $"Placing enemy {(EnemyInfo.Tags)payloadIndex}",
		Kind.Vehicle  => $"Placing vehicle {(VehicleInfo.Tags)payloadIndex}",
		Kind.Fire     => "Placing fire",
		Kind.Wildfire => "Placing wildfire",
		Kind.Teleport => "Teleport target",
		_             => string.Empty,
	};

	/// <summary>
	/// Polls mouse/keyboard once per frame. Calls OnResult when a placement happens.
	/// </summary>
	public void Tick(Camera Camera, bool mouseOverMenu, Action<CheatResult> OnResult)
	{
		if (!IsArmed)
			return;
		if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			Cancel();
			return;
		}
		if (Mouse.current == null)
			return;
		if (Mouse.current.rightButton.wasPressedThisFrame)
		{
			Cancel();
			return;
		}
		UpdateTarget(Camera);
		if (mouseOverMenu)
			return;
		if (!Mouse.current.leftButton.wasPressedThisFrame)
			return;
		CheatResult Result = Perform(TargetCell);
		OnResult?.Invoke(Result);
		// Hold Shift while clicking to disarm after one placement
		if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
			Cancel();
	}

	private void UpdateTarget(Camera Camera)
	{
		if (Camera == null || Mouse.current == null)
			return;
		Vector3 Screen = Mouse.current.position.ReadValue();
		Vector3 World = Camera.ScreenToWorldPoint(Screen);
		TargetCell = new Vector3Int(Mathf.FloorToInt(World.x), Mathf.FloorToInt(World.y), 0);
		TargetValid = Active switch
		{
			Kind.Item     => Actions.CanPlaceItem(TargetCell),
			Kind.Enemy    => Actions.CanPlaceEntity(TargetCell),
			Kind.Vehicle  => Actions.CanPlaceEntity(TargetCell),
			Kind.Fire     => Actions.CanPlaceFire(TargetCell),
			Kind.Wildfire => Actions.CanPlaceFire(TargetCell),
			Kind.Teleport => Actions.CanTeleport(TargetCell),
			_             => false,
		};
	}

	private CheatResult Perform(Vector3Int Cell) => Active switch
	{
		Kind.Item     => Actions.SpawnItem(payloadIndex, Cell),
		Kind.Enemy    => Actions.SpawnEnemy(payloadIndex, Cell),
		Kind.Vehicle  => Actions.SpawnVehicle(payloadIndex, fuel, Cell),
		Kind.Fire     => Actions.SpawnFire(Cell, false),
		Kind.Wildfire => Actions.SpawnFire(Cell, true),
		Kind.Teleport => Actions.Teleport(Cell),
		_             => CheatResult.Fail("Nothing armed"),
	};
}
#endif
