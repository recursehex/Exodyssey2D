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
    private Camera MainCamera;
    private int PixelsPerUnit;
    private bool SelectCursorActive;
    private PointerEventData PointerEventData;
    private readonly List<RaycastResult> RaycastResults = new();
    private void Awake()
    {
        MainCamera = Camera.main;
        PixelsPerUnit = PixelPerfectCamera.assetsPPU;
        TileManager = TileManager != null ? TileManager : FindFirstObjectByType<TileManager>();
        Player = Player != null ? Player : FindFirstObjectByType<Player>();
        InventoryUI = InventoryUI != null
            ? InventoryUI
            : Player != null ? Player.InventoryUI : FindFirstObjectByType<InventoryUI>();
        Tilemap = Tilemap != null
            ? Tilemap
            : Player != null ? Player.TilemapGround : FindFirstObjectByType<Tilemap>();
        if (CursorSprite == null || MainCamera == null)
        {
            enabled = false;
            return;
        }
        CursorSprite.gameObject.SetActive(true);
        if (SelectSprite != null)
            SelectSprite.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        if (CursorSprite == null || MainCamera == null)
            return;
        Cursor.visible = false;
        CursorSprite.gameObject.SetActive(!SelectCursorActive);
        if (SelectSprite != null)
            SelectSprite.gameObject.SetActive(SelectCursorActive);
    }
    private void OnDisable()
    {
        Cursor.visible = true;
        if (CursorSprite != null)
        {
            CursorSprite.gameObject.SetActive(false);
        }
        if (SelectSprite != null)
        {
            SelectSprite.gameObject.SetActive(false);
        }
        SelectCursorActive = false;
    }
    private void Update()
    {
        if (CursorSprite == null || MainCamera == null)
            return;
        Vector3 MousePosition   = Input.mousePosition;
        Vector3 ScreenPosition  = MainCamera.WorldToScreenPoint(CursorSprite.position);
        Vector3 WorldPosition   = MainCamera.ScreenToWorldPoint(new Vector3(MousePosition.x, MousePosition.y, ScreenPosition.z));
        WorldPosition.z = CursorSprite.position.z;
        EnsureTileManagerReference();
        bool shouldUseSelectCursor = IsHoveringSelectable(WorldPosition);
        Vector3 SnappedPosition = SnapToPixelGrid(WorldPosition);
        CursorSprite.position = SnappedPosition;
        if (SelectSprite != null)
            SelectSprite.position = SnappedPosition;
        UpdateActiveCursor(shouldUseSelectCursor);
    }
    private Vector3 SnapToPixelGrid(Vector3 position)
    {
        if (PixelsPerUnit <= 0)
            return position;
        position.x = Mathf.Round(position.x * PixelsPerUnit) / PixelsPerUnit;
        position.y = Mathf.Round(position.y * PixelsPerUnit) / PixelsPerUnit;
        return position;
    }
    private void UpdateActiveCursor(bool useSelectCursor)
    {
        if (SelectSprite == null || SelectCursorActive == useSelectCursor)
            return;
        SelectCursorActive = useSelectCursor;
        CursorSprite.gameObject.SetActive(!useSelectCursor);
        SelectSprite.gameObject.SetActive(useSelectCursor);
    }
    private bool IsHoveringSelectable(Vector3 worldPosition)
    {
        if (IsHoveringButton())
            return true;
        if (TileManager == null)
            return false;
        Vector3Int tilePoint = ConvertWorldToTilePoint(worldPosition);
        if (TileManager.IsInMovementRange(tilePoint))
            return true;
        Vector3 tileCenter = GetTileCenterWorld(tilePoint);
        return TileManager.IsInRangedWeaponRange(tileCenter);
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
            if (Result.gameObject.TryGetComponent<Button>(out _)
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
    private Vector3Int ConvertWorldToTilePoint(Vector3 worldPosition)
    {
        if (Tilemap != null)
            return Tilemap.WorldToCell(worldPosition);
        return Vector3Int.FloorToInt(worldPosition);
    }
    private Vector3 GetTileCenterWorld(Vector3Int tilePoint)
    {
        if (Tilemap != null)
        {
            Vector3 center = Tilemap.GetCellCenterWorld(tilePoint);
            center.z = 0f;
            return center;
        }
        return new Vector3(tilePoint.x + 0.5f, tilePoint.y + 0.5f, 0f);
    }
    private bool ButtonAllowsSelectCursor(GameObject buttonObject)
    {
        if (!IsInventoryIcon(buttonObject))
            return true;
        return HasItemForInventoryIcon(buttonObject.name);
    }
    private static bool IsInventoryIcon(GameObject buttonObject)
    {
        const string inventoryPrefix = "InventoryIcon";
        return buttonObject != null
               && buttonObject.name.StartsWith(inventoryPrefix, StringComparison.OrdinalIgnoreCase);
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
        string suffix = iconName.Substring(inventoryPrefix.Length);
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