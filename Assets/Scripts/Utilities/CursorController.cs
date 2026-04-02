using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    private InputAction PointAction;
    private InputAction MoveAction;
    [SerializeField] private float StickCursorSpeed = 600f;
    private Vector2 VirtualCursorPosition;
    public Vector2 CursorScreenPosition => VirtualCursorPosition;
    // TODO: Can be used to hide hardware cursor on gamepad or show different button prompts (e.g. "Press Y" vs "Left Click")
    // private bool UsingGamepad;
    private int PixelsPerUnit;
    private bool SelectCursorActive;
    private bool LoadingScreenVisible;
    private PointerEventData PointerEventData;
    private readonly List<RaycastResult> RaycastResults = new();
    private static Texture2D InvisibleCursorTexture;
    private static readonly Vector2 InvisibleCursorHotspot = Vector2.zero;
    // Tracks if invisible cursor is applied this frame to avoid redundant calls
    private bool CursorAppliedThisFrame;
    private bool NeedsHoverRefresh = true;
    private Vector3 LastMousePosition = new(float.NaN, float.NaN, float.NaN);
    private bool TileManagerEventsBound;
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
        EnsureInvisibleCursorTexture();
        BindTileManagerEvents();
    }
    private void Start()
    {
        PointAction = InputSystem.actions.FindAction("UI/Point");
        MoveAction = InputSystem.actions.FindAction("Player/Move");
        VirtualCursorPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }
    private void OnEnable()
    {
        ApplyInvisibleSystemCursor();
        SetCustomCursorSpritesActive(true);
        NeedsHoverRefresh = true;
        LastMousePosition = new Vector3(float.NaN, float.NaN, float.NaN);
    }
    private void OnDisable()
    {
        ResetSystemCursor();
        SetCustomCursorSpritesActive(false);
        SelectCursorActive = false;
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyInvisibleSystemCursor();
            SetCustomCursorSpritesActive(true);
            NeedsHoverRefresh = true;
            LastMousePosition = new Vector3(float.NaN, float.NaN, float.NaN);
        }
        else
        {
            ResetSystemCursor();
            SetCustomCursorSpritesActive(false);
        }
    }
    private void OnDestroy()
    {
        UnbindTileManagerEvents();
        if (LevelManager != null)
            LevelManager.OnLoadingScreenVisibilityChanged -= HandleLoadingScreenVisibilityChanged;
    }
    private void Update()
    {
        CursorAppliedThisFrame = false;
        if (!Application.isFocused)
            return;
        ApplyInvisibleSystemCursorOnce();
        Vector2 mouseScreenPos  = PointAction.ReadValue<Vector2>();
        Vector2 stickInput      = MoveAction.ReadValue<Vector2>();
        bool stickActive        = stickInput.sqrMagnitude > 0.01f;
        bool mouseMovedRaw      = (Vector3)(Vector2)mouseScreenPos != LastMousePosition;
        if (stickActive)
        {
            // UsingGamepad = true;
            VirtualCursorPosition += stickInput * StickCursorSpeed * Time.deltaTime;
            VirtualCursorPosition.x = Mathf.Clamp(VirtualCursorPosition.x, 0, Screen.width);
            VirtualCursorPosition.y = Mathf.Clamp(VirtualCursorPosition.y, 0, Screen.height);
        }
        else if (mouseMovedRaw)
        {
            // UsingGamepad = false;
            VirtualCursorPosition = mouseScreenPos;
        }
        bool cursorMoved = stickActive || mouseMovedRaw;
        EnsureTileManagerReference();
        BindTileManagerEvents();
        if (!cursorMoved && !NeedsHoverRefresh)
            return;
        LastMousePosition = mouseScreenPos;
        NeedsHoverRefresh = false;
        Vector3 ScreenPosition  = MainCamera.WorldToScreenPoint(CursorSprite.position);
        Vector3 WorldPosition   = MainCamera.ScreenToWorldPoint(new Vector3(VirtualCursorPosition.x, VirtualCursorPosition.y, ScreenPosition.z));
        WorldPosition.z         = CursorSprite.position.z;
        bool shouldUseSelectCursor  = IsHoveringSelectable(WorldPosition);
        Vector3 SnappedPosition     = SnapToPixelGrid(WorldPosition);
        CursorSprite.position       = SnappedPosition;
        SelectSprite.position       = SnappedPosition;
        UpdateActiveCursor(shouldUseSelectCursor);
    }
    private void LateUpdate()
    {
        if (!Application.isFocused)
            return;
        // Re-apply cursor in LateUpdate to catch Unity editor context switches that may reset cursor state between Update and rendering
        ApplyInvisibleSystemCursorOnce();
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
    /// <summary>
    /// Checks if the cursor is hovering over a selectable tile or UI element
    /// </summary>
    private bool IsHoveringSelectable(Vector3 WorldPosition)
    {
        if (LoadingScreenVisible)
            return false;
        if (IsHoveringButton())
            return true;
        Vector3Int tilePoint = ConvertWorldToTilePoint(WorldPosition);
        if (TileManager.IsInTileArea(tilePoint))
            return true;
        Vector3 tileCenter = GetTileCenterWorld(tilePoint);
        return TileManager.IsInRangedWeaponRange(tileCenter);
    }
    private void HandleLoadingScreenVisibilityChanged(bool isVisible)
    {
        LoadingScreenVisible = isVisible;
        NeedsHoverRefresh = true;
        if (isVisible)
            UpdateActiveCursor(false);
    }
    /// <summary>
    /// Checks if the cursor is hovering over an interactable UI button that allows the select cursor to be shown
    /// </summary>
    private bool IsHoveringButton()
    {
        if (EventSystem.current == null)
            return false;
        PointerEventData ??= new PointerEventData(EventSystem.current);
        PointerEventData.position = PointAction.ReadValue<Vector2>();
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
    private void BindTileManagerEvents()
    {
        if (TileManager == null || TileManagerEventsBound)
            return;
        TileManager.OnMarkersChanged += HandleTileMarkersChanged;
        TileManagerEventsBound = true;
    }
    private void UnbindTileManagerEvents()
    {
        if (!TileManagerEventsBound || TileManager == null)
            return;
        TileManager.OnMarkersChanged -= HandleTileMarkersChanged;
        TileManagerEventsBound = false;
    }
    private void HandleTileMarkersChanged() => NeedsHoverRefresh = true;
    private void SetCustomCursorSpritesActive(bool active)
    {
        if (CursorSprite != null)
            CursorSprite.gameObject.SetActive(active && !SelectCursorActive);
        if (SelectSprite != null)
            SelectSprite.gameObject.SetActive(active && SelectCursorActive);
    }
    private void ApplyInvisibleSystemCursorOnce()
    {
        if (CursorAppliedThisFrame)
            return;
        CursorAppliedThisFrame = true;
        ApplyInvisibleSystemCursor();
    }
    private static void EnsureInvisibleCursorTexture()
    {
        if (InvisibleCursorTexture != null)
            return;
        InvisibleCursorTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        InvisibleCursorTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
        InvisibleCursorTexture.Apply();
    }
    /// <summary>
    /// Applies an invisible system cursor to hide the default OS cursor
    /// </summary>
    private static void ApplyInvisibleSystemCursor()
    {
        EnsureInvisibleCursorTexture();
        Cursor.SetCursor(InvisibleCursorTexture, InvisibleCursorHotspot, CursorMode.Auto);
        Cursor.lockState = CursorLockMode.Confined;
    }
    /// <summary>
    /// Resets the system cursor to default visibility and behavior
    /// </summary>
    private static void ResetSystemCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
    private static bool IsInventoryIcon(GameObject ButtonObject) => ButtonObject.name.StartsWith("InventoryIcon", StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// Checks if the inventory has an item for the given inventory icon name
    /// </summary>
    private bool HasItemForInventoryIcon(string iconName)
    {
        if (!TryGetInventoryIndex(iconName, out int index))
            return false;
        Inventory inventory = ResolveInventory();
        if (inventory == null)
            return false;
        return inventory.HasItemAt(index);
    }
    /// <summary>
    /// Tries to extract the inventory index from the inventory icon name
    /// </summary>
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
    /// <summary>
    /// Resolves the current inventory reference from available components
    /// </summary>
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
