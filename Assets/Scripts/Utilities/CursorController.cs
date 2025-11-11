using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    [SerializeField] private Transform CursorSprite;
    [SerializeField] private Transform SelectSprite;
    [SerializeField] private PixelPerfectCamera PixelPerfectCamera;
    [SerializeField] private TileManager TileManager;
    [SerializeField] private Tilemap Tilemap;
    [SerializeField] private Player Player;
    [SerializeField] private InventoryUI InventoryUI;
    [SerializeField] private LevelManager LevelManager;
    private Camera MainCamera;
    private int PixelsPerUnit;
    private bool SelectCursorActive;
    private bool LoadingScreenVisible;
    private PointerEventData PointerEventData;
    private readonly List<RaycastResult> RaycastResults = new();
    private void Awake()
    {
        MainCamera      = Camera.main;
        PixelsPerUnit   = PixelPerfectCamera.assetsPPU;
        TileManager     = FindFirstObjectByType<TileManager>();
        Player          = FindFirstObjectByType<Player>();
        InventoryUI     = Player.InventoryUI;
        Tilemap         = Player.TilemapGround;
        LevelManager    = FindFirstObjectByType<LevelManager>();
        LevelManager.OnLoadingScreenVisibilityChanged += HandleLoadingScreenVisibilityChanged;
        CursorSprite.gameObject.SetActive(true);
        SelectSprite.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        Cursor.visible = false;
        CursorSprite.gameObject.SetActive(!SelectCursorActive);
        SelectSprite.gameObject.SetActive(SelectCursorActive);
    }
    private void OnDisable()
    {
        Cursor.visible = true;
        if (CursorSprite != null)
            CursorSprite.gameObject.SetActive(false);
        if (SelectSprite != null)
            SelectSprite.gameObject.SetActive(false);
        SelectCursorActive = false;
    }
    private void OnDestroy()
    {
        if (LevelManager != null)
            LevelManager.OnLoadingScreenVisibilityChanged -= HandleLoadingScreenVisibilityChanged;
    }
    private void Update()
    {
        Vector3 MousePosition   = Input.mousePosition;
        Vector3 ScreenPosition  = MainCamera.WorldToScreenPoint(CursorSprite.position);
        Vector3 WorldPosition   = MainCamera.ScreenToWorldPoint(new Vector3(MousePosition.x, MousePosition.y, ScreenPosition.z));
        WorldPosition.z         = CursorSprite.position.z;
        EnsureTileManagerReference();
        bool shouldUseSelectCursor  = !LoadingScreenVisible && IsHoveringSelectable(WorldPosition);
        Vector3 SnappedPosition     = SnapToPixelGrid(WorldPosition);
        CursorSprite.position       = SnappedPosition;
        SelectSprite.position       = SnappedPosition;
        UpdateActiveCursor(shouldUseSelectCursor);
    }
    private Vector3 SnapToPixelGrid(Vector3 Position)
    {
        if (PixelsPerUnit <= 0)
            return Position;
        Position.x = Mathf.Round(Position.x * PixelsPerUnit) / PixelsPerUnit;
        Position.y = Mathf.Round(Position.y * PixelsPerUnit) / PixelsPerUnit;
        return Position;
    }
    private void UpdateActiveCursor(bool useSelectCursor)
    {
        if (SelectCursorActive == useSelectCursor)
            return;
        SelectCursorActive = useSelectCursor;
        CursorSprite.gameObject.SetActive(!useSelectCursor);
        SelectSprite.gameObject.SetActive(useSelectCursor);
    }
    private bool IsHoveringSelectable(Vector3 WorldPosition)
    {
        if (LoadingScreenVisible)
            return false;
        if (IsHoveringButton())
            return true;
        Vector3Int tilePoint = ConvertWorldToTilePoint(WorldPosition);
        if (TileManager.IsInMovementRange(tilePoint))
            return true;
        Vector3 tileCenter = GetTileCenterWorld(tilePoint);
        return TileManager.IsInRangedWeaponRange(tileCenter);
    }
    private void HandleLoadingScreenVisibilityChanged(bool isVisible)
    {
        LoadingScreenVisible = isVisible;
        if (isVisible)
            UpdateActiveCursor(false);
    }
    private bool IsHoveringButton()
    {
        if (EventSystem.current == null)
            return false;
        PointerEventData ??= new PointerEventData(EventSystem.current);
        PointerEventData.position = Input.mousePosition;
        RaycastResults.Clear();
        EventSystem.current.RaycastAll(PointerEventData, RaycastResults);
        foreach (RaycastResult Result in RaycastResults)
        {
            if (Result.gameObject.TryGetComponent(out Button Button)
                && Button.interactable
                && ButtonAllowsSelectCursor(Result.gameObject))
                return true;
        }
        return false;
    }
    private void EnsureTileManagerReference()
    {
        if (TileManager == null)
            TileManager = FindFirstObjectByType<TileManager>();
    }
    private Vector3Int ConvertWorldToTilePoint(Vector3 WorldPosition)
    {
        if (Tilemap != null)
            return Tilemap.WorldToCell(WorldPosition);
        return Vector3Int.FloorToInt(WorldPosition);
    }
    private Vector3 GetTileCenterWorld(Vector3Int TilePoint)
    {
        if (Tilemap != null)
        {
            Vector3 Center = Tilemap.GetCellCenterWorld(TilePoint);
            Center.z = 0f;
            return Center;
        }
        return TilePoint + new Vector3(0.5f, 0.5f);
    }
    private bool ButtonAllowsSelectCursor(GameObject ButtonObject)
    {
        if (!IsInventoryIcon(ButtonObject))
            return true;
        return HasItemForInventoryIcon(ButtonObject.name);
    }
    private static bool IsInventoryIcon(GameObject ButtonObject)
    {
        return ButtonObject.name.StartsWith("InventoryIcon", StringComparison.OrdinalIgnoreCase);
    }
    private bool HasItemForInventoryIcon(string iconName)
    {
        if (!TryGetInventoryIndex(iconName, out int index))
            return false;
        Inventory inventory = ResolveInventory();
        if (inventory == null)
            return false;
        return index < inventory.Count;
    }
    private static bool TryGetInventoryIndex(string iconName, out int index)
    {
        const string inventoryPrefix = "InventoryIcon";
        index = -1;
        if (string.IsNullOrEmpty(iconName)
            || !iconName.StartsWith(inventoryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        string suffix = iconName[inventoryPrefix.Length..];
        return int.TryParse(suffix, out index);
    }
    private Inventory ResolveInventory()
    {
        if (InventoryUI != null)
            return InventoryUI.Inventory;
        if (Player != null)
        {
            InventoryUI = Player.InventoryUI;
            if (InventoryUI != null)
                return InventoryUI.Inventory;
            if (Tilemap == null)
                Tilemap = Player.TilemapGround;
        }
        InventoryUI = FindFirstObjectByType<InventoryUI>();
        if (InventoryUI != null)
            return InventoryUI.Inventory;
        Player = FindFirstObjectByType<Player>();
        if (Player != null)
        {
            InventoryUI = Player.InventoryUI;
            if (Tilemap == null)
                Tilemap = Player.TilemapGround;

            return InventoryUI != null ? InventoryUI.Inventory : null;
        }
        if (Tilemap == null)
            Tilemap = FindFirstObjectByType<Tilemap>();
        return null;
    }
}