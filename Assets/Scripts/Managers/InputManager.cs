using UnityEngine;
using UnityEngine.Tilemaps;

public class InputManager : MonoBehaviour
{
    private Camera MainCamera;
    private Tilemap TilemapGround;
    private Player Player;
    public delegate void PlayerActionDelegate(Vector3 WorldPoint, Vector3Int TilePoint, Vector3 ShiftedClickPoint);
    public event PlayerActionDelegate OnPlayerClick;
    public event PlayerActionDelegate OnPlayerHover;
    public void Initialize(Camera MainCamera, Tilemap TilemapGround, Player Player)
    {
        this.MainCamera     = MainCamera;
        this.TilemapGround  = TilemapGround;
        this.Player         = Player;
    }
    public void ProcessInput()
    {
        BoundsInt   CellBounds          = TilemapGround.cellBounds;
        Vector3     WorldPoint          = MainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int  TilePoint           = TilemapGround.WorldToCell(WorldPoint);
        Vector3     ShiftedClickPoint   = TilePoint + new Vector3(0.5f, 0.5f, 0);
        if (Input.GetMouseButtonDown(0) && CellBounds.Contains(TilePoint))
        {
            OnPlayerClick?.Invoke(WorldPoint, TilePoint, ShiftedClickPoint);
        }
        else if (CellBounds.Contains(TilePoint))
        {
            OnPlayerHover?.Invoke(WorldPoint, TilePoint, ShiftedClickPoint);
        }
        else
        {
            Player.InventoryUI.ProcessHoverForInventory(WorldPoint);
        }
    }
}