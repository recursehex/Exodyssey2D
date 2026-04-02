using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class InputManager : MonoBehaviour
{
    private Camera MainCamera;
    private Tilemap TilemapGround;
    private Player Player;
    private CursorController CursorController;
    private InputAction ClickAction;
    private InputAction InteractAction;
    private InputAction Drop1Action;
    private InputAction Drop2Action;
    public delegate void PlayerActionDelegate(Vector3 WorldPoint, Vector3Int
TilePoint, Vector3 ShiftedClickPoint);
    public event PlayerActionDelegate OnPlayerClick;
    public event PlayerActionDelegate OnPlayerHover;
    public void Initialize(Camera MainCamera, Tilemap TilemapGround, Player Player, CursorController CursorController)
    {
        this.MainCamera       = MainCamera;
        this.TilemapGround    = TilemapGround;
        this.Player           = Player;
        this.CursorController = CursorController;
        ClickAction = InputSystem.actions.FindAction("UI/Click");
        InteractAction = InputSystem.actions.FindAction("Player/Interact");
        Drop1Action = InputSystem.actions.FindAction("Player/Drop1");
        Drop2Action = InputSystem.actions.FindAction("Player/Drop2");
    }

    public void ProcessInput()
    {
        BoundsInt CellBounds      = TilemapGround.cellBounds;
        Vector3 WorldPoint        = MainCamera.ScreenToWorldPoint(CursorController.CursorScreenPosition);
        Vector3Int TilePoint      = TilemapGround.WorldToCell(WorldPoint);
        Vector3 ShiftedClickPoint = TilePoint + new Vector3(0.5f, 0.5f);

        if (Drop1Action != null && Drop1Action.WasPressedThisFrame())
            Player.TryDropItem(0);
        else if (Drop2Action != null && Drop2Action.WasPressedThisFrame())
            Player.TryDropItem(1);

        bool clicked = ClickAction.WasPressedThisFrame() || InteractAction.WasPressedThisFrame();
        if (clicked && CellBounds.Contains(TilePoint))
            OnPlayerClick?.Invoke(WorldPoint, TilePoint, ShiftedClickPoint);
        else if (CellBounds.Contains(TilePoint))
            OnPlayerHover?.Invoke(WorldPoint, TilePoint, ShiftedClickPoint);
        else
            Player.InventoryUI.ProcessHoverForInventory(WorldPoint);
    }
}