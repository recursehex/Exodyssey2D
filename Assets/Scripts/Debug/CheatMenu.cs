#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Editor/dev-only cheat overlay. Added by GameManager under #if guard. Toggled with
/// backtick. Rendered with IMGUI (no Canvas wiring). All gameplay-affecting logic lives
/// in the shared CheatActions facade; this class is just the view + input glue.
/// </summary>
public class CheatMenu : MonoBehaviour
{
	public static bool IsOpen { get; private set; }
	public static bool IsPlacing { get; private set; }

	private enum Tab { Spawn, Player, World, Debug }
	private static readonly string[] TabNames = { "Spawn", "Player", "World", "Debug" };
	private const int MaxLogLines = 10;
	private const int GridColumns = 4;

	private bool initialized;
	private CheatActions Actions;
	private CheatCommandParser Parser;
	private CheatPlacementMode Placement;
	private Player Player;
	private Camera MainCamera;
	private UnityEngine.EventSystems.EventSystem EventSystem;

	private Tab currentTab = Tab.Spawn;
	private Vector2 contentScroll;
	private Vector2 logScroll;
	private Rect panelRect;

	// Cached enum option lists (exclude "Unknown")
	private string[] itemNames;
	private string[] enemyNames;
	private string[] vehicleNames;
	private string[] timeNames;
	private string[] regionNames;

	// Tab selection state
	private int spawnItemSel, spawnEnemySel, spawnVehicleSel, addItemSel, timeSel, regionSel;
	private bool vehicleFullFuel = true;
	private int vehicleFuel = 4;

	// Command box
	private string commandText = "";
	private bool commandFieldFocused;
	private readonly List<string> history = new();
	private int historyIndex = -1;
	private readonly List<(string Text, bool Ok)> log = new();

	// FPS
	private bool showFps;
	private float fpsDelta;

	private Texture2D whiteTex;
	private bool stylesReady;
	private GUIStyle logStyle;
	private GUIStyle richLabelStyle;
	private GUIStyle titleStyle;
	private GUIStyle hintStyle;

	private void TryInitialize()
	{
		if (GameManager.Instance == null)
			return;
		Actions = new CheatActions(GameManager.Instance);
		Parser = new CheatCommandParser(Actions);
		Placement = new CheatPlacementMode(Actions);
		Player = GameManager.Instance.DebugPlayer;
		MainCamera = Camera.main;
		EventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
		itemNames   = CheatActions.EnumOptions<ItemInfo.Tags>().ToArray();
		enemyNames  = CheatActions.EnumOptions<EnemyInfo.Tags>().ToArray();
		vehicleNames= CheatActions.EnumOptions<VehicleInfo.Tags>().ToArray();
		timeNames   = CheatActions.EnumOptions<LevelManager.TimeOfDay>().ToArray();
		regionNames = CheatActions.EnumOptions<RegionInfo.Tags>().ToArray();
		whiteTex = new Texture2D(1, 1);
		whiteTex.SetPixel(0, 0, Color.white);
		whiteTex.Apply();
		// Skip the IMGUI layout pass (and its per-frame garbage) unless the panel is open
		useGUILayout = false;
		initialized = true;
	}

	private void Update()
	{
		if (!initialized)
		{
			TryInitialize();
			if (!initialized)
				return;
		}
		if (MainCamera == null)
			MainCamera = Camera.main;

		fpsDelta += (Time.unscaledDeltaTime - fpsDelta) * 0.1f;

		Keyboard Kb = Keyboard.current;
		if (Kb != null && Kb.backquoteKey.wasPressedThisFrame && !commandFieldFocused)
			Toggle();

		// Placement runs whether the menu is open or closed (arming closes the menu so the
		// grid is visible). Clicks on the panel are ignored only while it's actually drawn.
		if (Placement != null && Placement.IsArmed)
			Placement.Tick(MainCamera, IsOpen && IsMouseOverPanel(), PushLog);
		IsPlacing = Placement != null && Placement.IsArmed;

		// Only run the IMGUI layout pass when the full panel is drawn (open); FPS/placement
		// overlays use immediate GUI.* calls that don't need layout
		useGUILayout = IsOpen;

		// Disable all uGUI interaction (inventory clicks/drag, buttons under the panel) while active
		if (EventSystem != null)
			EventSystem.enabled = !(IsOpen || IsPlacing);

		// Escape closes the menu when open and not mid-placement (placement eats Escape first)
		if (IsOpen && Kb != null && Kb.escapeKey.wasPressedThisFrame
			&& !IsPlacing && !commandFieldFocused)
			IsOpen = false;
	}

	private void Toggle() => IsOpen = !IsOpen;

	/// <summary>
	/// Arms click-to-place and closes the menu so the full-screen panel doesn't hide the grid.
	/// </summary>
	private void ArmPlacement(CheatPlacementMode.Kind Kind, int index = 0, int fuel = -1)
	{
		Placement.Arm(Kind, index, fuel);
		IsOpen = false;
	}

	private bool IsMouseOverPanel()
	{
		if (Mouse.current == null)
			return false;
		Vector2 Screen = Mouse.current.position.ReadValue();
		float guiY = UnityEngine.Screen.height - Screen.y;
		return panelRect.Contains(new Vector2(Screen.x, guiY));
	}

	private void OnGUI()
	{
		if (!initialized)
			return;
		// Skip all work when idle: menu closed, no FPS overlay, and not placing
		if (!IsOpen && !showFps && (Placement == null || !Placement.IsArmed))
			return;
		EnsureStyles();

		if (showFps)
		{
			float fps = fpsDelta > 0f ? 1f / fpsDelta : 0f;
			GUI.Label(new Rect(UnityEngine.Screen.width - 160, 6, 150, 30), $"FPS: {fps:0.}");
		}

		// Highlight + hint draw even when the menu is closed (placement keeps the menu hidden)
		if (Placement != null && Placement.IsArmed)
		{
			DrawPlacementHighlight();
			DrawPlacementHint();
		}

		if (!IsOpen)
			return;

		panelRect = new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);
		GUI.Box(panelRect, GUIContent.none);
		GUILayout.BeginArea(new Rect(12, 12, UnityEngine.Screen.width - 24, UnityEngine.Screen.height - 24));

		GUILayout.BeginHorizontal();
		GUILayout.Label("<b>CHEAT MENU</b>", titleStyle);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("X", GUILayout.Width(56)))
			IsOpen = false;
		GUILayout.EndHorizontal();

		currentTab = (Tab)GUILayout.Toolbar((int)currentTab, TabNames);

		contentScroll = GUILayout.BeginScrollView(contentScroll, GUILayout.Height(UnityEngine.Screen.height - 320));
		switch (currentTab)
		{
			case Tab.Spawn:  DrawSpawnTab();  break;
			case Tab.Player: DrawPlayerTab(); break;
			case Tab.World:  DrawWorldTab();  break;
			case Tab.Debug:  DrawDebugTab();  break;
		}
		GUILayout.EndScrollView();

		DrawCommandBox();
		GUILayout.EndArea();

		commandFieldFocused = GUI.GetNameOfFocusedControl() == "CheatCommand";
	}

	/// <summary>
	/// Builds cached styles and enlarges the built-in controls (~2x) exactly once. IMGUI
	/// styles persist on the shared skin, so there's no need to re-apply every OnGUI pass.
	/// </summary>
	private void EnsureStyles()
	{
		if (stylesReady)
			return;
		logStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 16 };
		richLabelStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18 };
		titleStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 26, fontStyle = FontStyle.Bold };
		hintStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, richText = true, alignment = TextAnchor.MiddleCenter };
		GUI.skin.button.fontSize = 20;
		GUI.skin.button.fixedHeight = 38;
		GUI.skin.button.margin = new RectOffset(4, 4, 4, 4);
		GUI.skin.label.fontSize = 18;
		GUI.skin.toggle.fontSize = 20;
		GUI.skin.textField.fontSize = 20;
		GUI.skin.textField.fixedHeight = 36;
		GUI.skin.box.fontSize = 18;
		GUI.skin.horizontalSlider.fixedHeight = 24;
		GUI.skin.horizontalSliderThumb.fixedHeight = 24;
		GUI.skin.horizontalSliderThumb.fixedWidth = 24;
		stylesReady = true;
	}

	private void DrawPlacementHint()
	{
		string Text = $"{Placement.Label} — click a tile  •  Shift+click = once  •  Esc/RMB = cancel  •  ` = menu";
		float w = Mathf.Min(900f, UnityEngine.Screen.width - 40f);
		GUI.Box(new Rect((UnityEngine.Screen.width - w) * 0.5f, 8, w, 34), Text, hintStyle);
	}

	#region TABS
	private void DrawSpawnTab()
	{
		GUILayout.Label("<b>Item</b>", RichLabel());
		spawnItemSel = GUILayout.SelectionGrid(spawnItemSel, itemNames, GridColumns);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Place (click tile)"))
			ArmPlacement(CheatPlacementMode.Kind.Item, spawnItemSel);
		if (GUILayout.Button("Spawn at player"))
			PushLog(Actions.SpawnItem(spawnItemSel, Actions.PlayerCell));
		GUILayout.EndHorizontal();

		GUILayout.Space(8);
		GUILayout.Label("<b>Enemy</b>", RichLabel());
		spawnEnemySel = GUILayout.SelectionGrid(spawnEnemySel, enemyNames, GridColumns);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Place (click tile)"))
			ArmPlacement(CheatPlacementMode.Kind.Enemy, spawnEnemySel);
		if (GUILayout.Button("Spawn near player"))
			SpawnEntityNearPlayer(CheatPlacementMode.Kind.Enemy, spawnEnemySel);
		GUILayout.EndHorizontal();

		GUILayout.Space(8);
		GUILayout.Label("<b>Vehicle</b>", RichLabel());
		spawnVehicleSel = GUILayout.SelectionGrid(spawnVehicleSel, vehicleNames, GridColumns);
		vehicleFullFuel = GUILayout.Toggle(vehicleFullFuel, " Full fuel");
		if (!vehicleFullFuel)
		{
			GUILayout.Label($"Fuel: {vehicleFuel}");
			vehicleFuel = Mathf.RoundToInt(GUILayout.HorizontalSlider(vehicleFuel, 0, 20));
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Place (click tile)"))
			ArmPlacement(CheatPlacementMode.Kind.Vehicle, spawnVehicleSel, CurrentFuel());
		if (GUILayout.Button("Spawn near player"))
			SpawnEntityNearPlayer(CheatPlacementMode.Kind.Vehicle, spawnVehicleSel);
		GUILayout.EndHorizontal();

		GUILayout.Space(8);
		GUILayout.Label("<b>Fire</b>", RichLabel());
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Place fire"))
			ArmPlacement(CheatPlacementMode.Kind.Fire);
		if (GUILayout.Button("Place wildfire"))
			ArmPlacement(CheatPlacementMode.Kind.Wildfire);
		GUILayout.EndHorizontal();
	}

	private int CurrentFuel() => vehicleFullFuel ? -1 : vehicleFuel;

	private void SpawnEntityNearPlayer(CheatPlacementMode.Kind Kind, int index)
	{
		if (!Actions.TryDefaultSpawnCell(out Vector3Int Cell))
		{
			PushLog(CheatResult.Fail("No free tile near player"));
			return;
		}
		PushLog(Kind == CheatPlacementMode.Kind.Enemy
			? Actions.SpawnEnemy(index, Cell)
			: Actions.SpawnVehicle(index, CurrentFuel(), Cell));
	}

	private void DrawPlayerTab()
	{
		int curHp = Player.Debug_CurrentHealth;
		int maxHp = Player.Debug_MaxHealth;
		GUILayout.Label($"<b>Health: {curHp}/{maxHp}</b>", RichLabel());
		int newHp = Mathf.RoundToInt(GUILayout.HorizontalSlider(curHp, 0, maxHp));
		if (newHp != curHp)
			PushLog(Actions.SetHealth(newHp));
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Max HP -")) PushLog(Actions.SetMaxHealth(maxHp - 1));
		if (GUILayout.Button("Max HP +")) PushLog(Actions.SetMaxHealth(maxHp + 1));
		if (GUILayout.Button("Restore Full HP")) PushLog(Actions.RestoreFullHealth());
		GUILayout.EndHorizontal();

		int curEn = Player.CurrentEnergy;
		int maxEn = Player.Debug_MaxEnergy;
		GUILayout.Space(6);
		GUILayout.Label($"<b>Energy: {curEn}/{maxEn}</b>", RichLabel());
		int newEn = Mathf.RoundToInt(GUILayout.HorizontalSlider(curEn, 0, maxEn));
		if (newEn != curEn)
			PushLog(Actions.SetEnergy(newEn));
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Max EN -")) PushLog(Actions.SetMaxEnergy(maxEn - 1));
		if (GUILayout.Button("Max EN +")) PushLog(Actions.SetMaxEnergy(maxEn + 1));
		if (GUILayout.Button("Restore Full EN")) PushLog(Actions.RestoreFullEnergy());
		GUILayout.EndHorizontal();

		GUILayout.Space(6);
		ToggleFlag("God Mode", CheatFlags.Invincibility, Actions.ToggleInvincibility);
		ToggleFlag("Infinite Energy", CheatFlags.InfiniteEnergy, Actions.ToggleInfiniteEnergy);

		GUILayout.Space(8);
		GUILayout.Label("<b>Add Item</b>", RichLabel());
		addItemSel = GUILayout.SelectionGrid(addItemSel, itemNames, GridColumns);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Add to Inventory")) PushLog(Actions.AddItem(addItemSel));
		if (GUILayout.Button("Clear Inventory")) PushLog(Actions.ClearInventory());
		GUILayout.EndHorizontal();

		GUILayout.Space(8);
		GUILayout.Label("<b>Equipment</b>", RichLabel());
		bool hasHelmet = Player.Debug_HasHelmet;
		bool nh = GUILayout.Toggle(hasHelmet, " Helmet");
		if (nh != hasHelmet) PushLog(Actions.EquipHelmet(nh));
		bool hasVest = Player.Debug_HasVest;
		bool nv = GUILayout.Toggle(hasVest, " Vest");
		if (nv != hasVest) PushLog(Actions.EquipVest(nv));
		bool hasNvg = Player.HasNightVision;
		bool nn = GUILayout.Toggle(hasNvg, " Night Vision");
		if (nn != hasNvg) PushLog(Actions.EquipNightVision(nn));
		if (GUILayout.Button("Remove All Equipment")) PushLog(Actions.RemoveAllEquipment());
	}

	private void DrawWorldTab()
	{
		GUILayout.Label("<b>Time of Day</b>", RichLabel());
		timeSel = GUILayout.SelectionGrid(timeSel, timeNames, GridColumns);
		if (GUILayout.Button("Set Time"))
			PushLog(Actions.SetTimeOfDay((LevelManager.TimeOfDay)timeSel));

		GUILayout.Space(6);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Advance Level")) PushLog(Actions.AdvanceLevel());
		if (GUILayout.Button("Regenerate Level")) PushLog(Actions.RegenerateLevel());
		GUILayout.EndHorizontal();
		int level = GameManager.Instance.Level;
		GUILayout.BeginHorizontal();
		GUILayout.Label($"Level: {level}", GUILayout.Width(90));
		if (GUILayout.Button("-")) PushLog(Actions.SetLevel(level - 1));
		if (GUILayout.Button("+")) PushLog(Actions.SetLevel(level + 1));
		GUILayout.EndHorizontal();

		GUILayout.Space(6);
		GUILayout.Label("<b>Cleanup</b>", RichLabel());
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Kill All Enemies")) PushLog(Actions.KillAllEnemies());
		if (GUILayout.Button("Remove All Items")) PushLog(Actions.RemoveAllItems());
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Remove All Vehicles")) PushLog(Actions.RemoveAllVehicles());
		if (GUILayout.Button("Extinguish All Fires")) PushLog(Actions.ExtinguishAllFires());
		GUILayout.EndHorizontal();
		if (GUILayout.Button("Clear Everything")) PushLog(Actions.ClearEverything());

		GUILayout.Space(6);
		GUILayout.Label("<b>Turn</b>", RichLabel());
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Force Chronoclasm")) PushLog(Actions.ForceChronoclasmReady());
		if (GUILayout.Button("End Turn")) PushLog(Actions.EndTurn());
		GUILayout.EndHorizontal();

		GUILayout.Space(6);
		ToggleFlag("Reveal Full Map", CheatFlags.RevealAll, Actions.ToggleRevealAll);
	}

	private void DrawDebugTab()
	{
		GUILayout.Label(Actions.GetStatus(), logStyle);

		GUILayout.Space(6);
		showFps = GUILayout.Toggle(showFps, " FPS Counter");

		bool teleportArmed = Placement.Active == CheatPlacementMode.Kind.Teleport;
		bool wantTeleport = GUILayout.Toggle(teleportArmed, " Teleport Mode (click tile)");
		if (wantTeleport != teleportArmed)
		{
			if (wantTeleport) ArmPlacement(CheatPlacementMode.Kind.Teleport);
			else Placement.Cancel();
		}

		GUILayout.Space(6);
		GUILayout.Label("<b>Skip to Region</b>", RichLabel());
		regionSel = GUILayout.SelectionGrid(regionSel, regionNames, 2);
		if (GUILayout.Button("Set Region")) PushLog(Actions.SetRegion(regionSel));

		GUILayout.Space(6);
		ToggleFlag("Invincible Enemies", CheatFlags.InvincibleEnemies, Actions.ToggleInvincibleEnemies);
		ToggleFlag("Freeze Enemies", CheatFlags.FreezeEnemies, Actions.ToggleFreezeEnemies);

		GUILayout.Space(6);
		if (GUILayout.Button("Log Entity Positions")) PushLog(Actions.LogEntityPositions());
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Trigger Game Over")) PushLog(Actions.TriggerGameOver());
		if (GUILayout.Button("Restart Game")) PushLog(Actions.RestartGame());
		GUILayout.EndHorizontal();
	}
	#endregion

	#region COMMAND BOX
	private void DrawCommandBox()
	{
		HandleCommandKeys();
		GUILayout.BeginHorizontal();
		GUI.SetNextControlName("CheatCommand");
		commandText = GUILayout.TextField(commandText);
		if (GUILayout.Button("Execute", GUILayout.Width(70)))
			ExecuteCommand(commandText);
		GUILayout.EndHorizontal();

		logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.Height(120));
		for (int i = 0; i < log.Count; i++)
		{
			Color prev = GUI.color;
			GUI.color = log[i].Ok ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f);
			GUILayout.Label(log[i].Text, logStyle);
			GUI.color = prev;
		}
		GUILayout.EndScrollView();
	}

	/// <summary>
	/// Handles Enter (submit) and Up/Down (history) while the command field is focused.
	/// </summary>
	private void HandleCommandKeys()
	{
		Event e = Event.current;
		if (e.type != EventType.KeyDown || !commandFieldFocused)
			return;
		if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
		{
			ExecuteCommand(commandText);
			e.Use();
		}
		else if (e.keyCode == KeyCode.UpArrow && history.Count > 0)
		{
			historyIndex = historyIndex < 0 ? history.Count - 1 : Mathf.Max(0, historyIndex - 1);
			commandText = history[historyIndex];
			e.Use();
		}
		else if (e.keyCode == KeyCode.DownArrow && history.Count > 0)
		{
			if (historyIndex >= 0 && historyIndex < history.Count - 1)
				commandText = history[++historyIndex];
			else
			{
				historyIndex = -1;
				commandText = "";
			}
			e.Use();
		}
	}

	private void ExecuteCommand(string Text)
	{
		Text = Text?.Trim();
		if (string.IsNullOrEmpty(Text))
			return;
		history.Add(Text);
		historyIndex = -1;
		commandText = "";

		// View-only commands handled here; everything else routes through the parser
		switch (Text.ToLowerInvariant())
		{
			case "fps":   showFps = !showFps; PushLog(CheatResult.Pass($"FPS counter {(showFps ? "ON" : "OFF")}")); return;
			case "clear": log.Clear(); return;
			case "close": IsOpen = false; return;
		}
		PushLog(Parser.Parse(Text));
	}

	private void PushLog(CheatResult Result)
	{
		log.Add(("> " + Result.Message, Result.Ok));
		if (log.Count > MaxLogLines)
			log.RemoveRange(0, log.Count - MaxLogLines);
		logScroll.y = float.MaxValue;
		if (!Result.Ok)
			Debug.LogWarning($"[Cheat] {Result.Message}");
	}
	#endregion

	#region HELPERS
	private GUIStyle RichLabel() => richLabelStyle;

	private void ToggleFlag(string Label, bool current, System.Func<CheatResult> Toggle)
	{
		bool next = GUILayout.Toggle(current, " " + Label);
		if (next != current)
			PushLog(Toggle());
	}

	private void DrawPlacementHighlight()
	{
		if (MainCamera == null || !Placement.IsArmed)
			return;
		if (!Actions.IsCellOnGrid(Placement.TargetCell))
			return;
		Vector3 bottomLeft = MainCamera.WorldToScreenPoint(Placement.TargetCell);
		Vector3 topRight = MainCamera.WorldToScreenPoint(Placement.TargetCell + new Vector3Int(1, 1, 0));
		float x = bottomLeft.x;
		float y = UnityEngine.Screen.height - topRight.y;
		float w = topRight.x - bottomLeft.x;
		float h = topRight.y - bottomLeft.y;
		Color prev = GUI.color;
		GUI.color = (Placement.TargetValid ? Color.green : Color.red) * new Color(1, 1, 1, 0.35f);
		GUI.DrawTexture(new Rect(x, y, w, h), whiteTex);
		GUI.color = prev;
	}

	private void OnDestroy()
	{
		if (whiteTex != null)
			Destroy(whiteTex);
		if (EventSystem != null)
			EventSystem.enabled = true;
		IsOpen = false;
		IsPlacing = false;
	}
	#endregion
}
#endif
