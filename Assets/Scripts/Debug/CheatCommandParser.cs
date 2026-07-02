#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

/// <summary>
/// Parses structured text commands and dispatches them to the shared CheatActions facade,
/// so every command runs the exact same code as its equivalent button.
/// </summary>
public class CheatCommandParser
{
	private readonly CheatActions Actions;
	public CheatCommandParser(CheatActions Actions) => this.Actions = Actions;

	private static readonly char[] Separators = { ' ', '\t' };

	public CheatResult Parse(string Raw)
	{
		if (string.IsNullOrWhiteSpace(Raw))
			return CheatResult.Fail("Empty command");
		string[] t = Raw.Trim().Split(Separators, StringSplitOptions.RemoveEmptyEntries);
		string cmd = t[0].ToLowerInvariant();
		switch (cmd)
		{
			case "spawn":   return ParseSpawn(t);
			case "set":     return ParseSet(t);
			case "give":    return ParseGive(t);
			case "equip":   return ParseEquip(t);
			case "unequip": return Actions.RemoveAllEquipment();
			case "heal":    return Actions.RestoreFullHealth();
			case "restore": return Actions.RestoreFullEnergy();
			case "god":     return Actions.ToggleInvincibility();
			case "noclip":  return Actions.ToggleInfiniteEnergy();
			case "invenemy":return Actions.ToggleInvincibleEnemies();
			case "freeze":  return Actions.ToggleFreezeEnemies();
			case "tp":      return ParseTeleport(t);
			case "time":    return ParseTime(t);
			case "level":   return ParseIntArg(t, Actions.SetLevel, "level <n>");
			case "day":     return ParseIntArg(t, Actions.SetDay, "day <n>");
			case "region":  return ParseIntArg(t, Actions.SetRegion, "region <index>");
			case "killall":      return Actions.KillAllEnemies();
			case "clearitems":   return Actions.RemoveAllItems();
			case "clearvehicles":return Actions.RemoveAllVehicles();
			case "clearfires":   return Actions.ExtinguishAllFires();
			case "clearall":     return Actions.ClearEverything();
			case "endturn": return Actions.EndTurn();
			case "chrono":  return Actions.ForceChronoclasmReady();
			case "reveal":  return Actions.ToggleRevealAll();
			case "gameover":return Actions.TriggerGameOver();
			case "restart": return Actions.RestartGame();
			case "advance":
			case "nextlevel": return Actions.AdvanceLevel();
			case "regen":   return Actions.RegenerateLevel();
			case "logpos":  return Actions.LogEntityPositions();
			case "list":    return ParseList(t);
			case "status":  return CheatResult.Pass("\n" + Actions.GetStatus());
			case "help":    return ParseHelp(t);
			default:        return CheatResult.Fail($"Unknown command '{cmd}' — try 'help'");
		}
	}

	#region SPAWN
	private CheatResult ParseSpawn(string[] t)
	{
		if (t.Length < 2)
			return CheatResult.Fail("Usage: spawn item|enemy|vehicle|fire|wildfire ...");
		string what = t[1].ToLowerInvariant();
		switch (what)
		{
			case "item":    return SpawnTagged<ItemInfo.Tags>(t, 2, (i, c) => Actions.SpawnItem(i, c), playerCellDefault: true);
			case "enemy":   return SpawnTagged<EnemyInfo.Tags>(t, 2, (i, c) => Actions.SpawnEnemy(i, c), playerCellDefault: false);
			case "vehicle": return SpawnVehicle(t);
			case "fire":    return SpawnFire(t, 2, false);
			case "wildfire":return SpawnFire(t, 2, true);
			default:        return CheatResult.Fail($"Unknown spawn target '{what}'");
		}
	}

	private CheatResult SpawnTagged<T>(string[] t, int tagIndex, Func<int, Vector3Int, CheatResult> Spawn, bool playerCellDefault) where T : struct, Enum
	{
		if (t.Length <= tagIndex)
			return CheatResult.Fail($"Missing tag. Try 'list {typeof(T).Name}'");
		if (!CheatActions.TryParseEnum(t[tagIndex], out T Tag))
			return CheatResult.Fail($"Unknown tag '{t[tagIndex]}'");
		Vector3Int Cell = ResolveCell(t, tagIndex + 1, playerCellDefault, out bool ok);
		if (!ok) return CheatResult.Fail("No free tile to spawn on");
		return Spawn(Convert.ToInt32(Tag), Cell);
	}

	private CheatResult SpawnVehicle(string[] t)
	{
		if (t.Length <= 2)
			return CheatResult.Fail("Missing tag. Try 'list vehicle'");
		if (!CheatActions.TryParseEnum(t[2], out VehicleInfo.Tags Tag))
			return CheatResult.Fail($"Unknown vehicle '{t[2]}'");
		int extras = t.Length - 3;
		int fuel = -1;
		Vector3Int Cell;
		bool ok;
		// [fuel] [x y] — disambiguated by extra-token count
		if (extras == 1)
		{
			if (!int.TryParse(t[3], out fuel)) return CheatResult.Fail("Fuel must be a number");
			Cell = ResolveCell(t, 99, false, out ok);
		}
		else if (extras == 2)
		{
			Cell = ResolveCell(t, 3, false, out ok);
		}
		else if (extras >= 3)
		{
			if (!int.TryParse(t[3], out fuel)) return CheatResult.Fail("Fuel must be a number");
			Cell = ResolveCell(t, 4, false, out ok);
		}
		else
		{
			Cell = ResolveCell(t, 99, false, out ok);
		}
		if (!ok) return CheatResult.Fail("No free tile to spawn on");
		return Actions.SpawnVehicle((int)Tag, fuel, Cell);
	}

	private CheatResult SpawnFire(string[] t, int coordIndex, bool wildfire)
	{
		Vector3Int Cell = ResolveCell(t, coordIndex, true, out bool ok);
		if (!ok) return CheatResult.Fail("No free tile to spawn on");
		return Actions.SpawnFire(Cell, wildfire);
	}

	/// <summary>
	/// Reads optional "x y" coords at the index, else falls back to the player's cell
	/// (or the nearest free cell for entities that can't share the player's tile).
	/// </summary>
	private Vector3Int ResolveCell(string[] t, int index, bool playerCellDefault, out bool ok)
	{
		if (t.Length > index + 1
			&& int.TryParse(t[index], out int x)
			&& int.TryParse(t[index + 1], out int y))
		{
			ok = true;
			return new Vector3Int(x, y);
		}
		if (playerCellDefault)
		{
			ok = true;
			return Actions.PlayerCell;
		}
		return Actions.TryDefaultSpawnCell(out Vector3Int Cell) ? Pass(out ok, Cell) : Fail(out ok);
	}

	private static Vector3Int Pass(out bool ok, Vector3Int Cell) { ok = true; return Cell; }
	private static Vector3Int Fail(out bool ok) { ok = false; return Vector3Int.zero; }
	#endregion

	#region PLAYER / WORLD HELPERS
	private CheatResult ParseSet(string[] t)
	{
		if (t.Length < 3)
			return CheatResult.Fail("Usage: set health|energy|maxhealth|maxenergy <value>");
		if (!int.TryParse(t[2], out int value))
			return CheatResult.Fail($"'{t[2]}' is not a number");
		switch (t[1].ToLowerInvariant())
		{
			case "health":    return Actions.SetHealth(value);
			case "maxhealth": return Actions.SetMaxHealth(value);
			case "energy":    return Actions.SetEnergy(value);
			case "maxenergy": return Actions.SetMaxEnergy(value);
			default:          return CheatResult.Fail($"Unknown stat '{t[1]}'");
		}
	}

	private CheatResult ParseGive(string[] t)
	{
		if (t.Length < 2)
			return CheatResult.Fail("Usage: give <item_tag>");
		if (!CheatActions.TryParseEnum(t[1], out ItemInfo.Tags Tag))
			return CheatResult.Fail($"Unknown item '{t[1]}'");
		return Actions.AddItem((int)Tag);
	}

	private CheatResult ParseEquip(string[] t)
	{
		if (t.Length < 2)
			return CheatResult.Fail("Usage: equip helmet|vest|nightvision|all");
		switch (t[1].ToLowerInvariant())
		{
			case "helmet":      return Actions.EquipHelmet(true);
			case "vest":        return Actions.EquipVest(true);
			case "nightvision": return Actions.EquipNightVision(true);
			case "all":         return Actions.EquipAll();
			default:            return CheatResult.Fail($"Unknown equipment '{t[1]}'");
		}
	}

	private CheatResult ParseTeleport(string[] t)
	{
		if (t.Length < 3 || !int.TryParse(t[1], out int x) || !int.TryParse(t[2], out int y))
			return CheatResult.Fail("Usage: tp <x> <y>");
		return Actions.Teleport(new Vector3Int(x, y));
	}

	private CheatResult ParseTime(string[] t)
	{
		if (t.Length < 2)
			return CheatResult.Fail("Usage: time <dawn|noon|afternoon|dusk|night>");
		if (!CheatActions.TryParseEnum(t[1], out LevelManager.TimeOfDay Time))
			return CheatResult.Fail($"Unknown time '{t[1]}'");
		return Actions.SetTimeOfDay(Time);
	}

	private CheatResult ParseIntArg(string[] t, Func<int, CheatResult> Action, string usage)
	{
		if (t.Length < 2 || !int.TryParse(t[1], out int value))
			return CheatResult.Fail($"Usage: {usage}");
		return Action(value);
	}
	#endregion

	#region UTILITY
	private CheatResult ParseList(string[] t)
	{
		if (t.Length < 2)
			return CheatResult.Fail("Usage: list items|enemies|vehicles");
		switch (t[1].ToLowerInvariant())
		{
			case "items":    case "item":    return CheatResult.Pass("Items: " + string.Join(", ", CheatActions.EnumOptions<ItemInfo.Tags>()));
			case "enemies":  case "enemy":   return CheatResult.Pass("Enemies: " + string.Join(", ", CheatActions.EnumOptions<EnemyInfo.Tags>()));
			case "vehicles": case "vehicle": return CheatResult.Pass("Vehicles: " + string.Join(", ", CheatActions.EnumOptions<VehicleInfo.Tags>()));
			default: return CheatResult.Fail($"Unknown list '{t[1]}'");
		}
	}

	private CheatResult ParseHelp(string[] t)
	{
		return CheatResult.Pass(
			"\nSpawn: spawn item|enemy <tag> [x y] | spawn vehicle <tag> [fuel] [x y] | spawn fire|wildfire [x y]\n" +
			"Player: set health|energy|maxhealth|maxenergy <n> | give <tag> | equip helmet|vest|nightvision|all | unequip | heal | restore | god | noclip | tp <x> <y>\n" +
			"World: time <name> | level <n> | day <n> | region <n> | advance | regen | killall | clearitems|clearvehicles|clearfires|clearall | endturn | chrono | reveal\n" +
			"Debug: gameover | restart | invenemy | freeze | logpos | list items|enemies|vehicles | status | help");
	}
	#endregion
}
#endif
