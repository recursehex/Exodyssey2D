using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerActionController : MonoBehaviour
{
	private const string UndoAfterItemAction = "Undo history cleared after using an item.";
	private const string UndoAfterTileItemAction = "Undo history cleared after using an item on a tile.";
	private const string UndoAfterAttack = "Undo history cleared after attacking.";
	private readonly List<Item> LitDynamite = new();
	private GameManager GameManager;
	private Player Player;
	private TileManager TileManager;
	private TurnManager TurnManager;
	private LevelManager LevelManager;
	private ChronoclasmManager ChronoclasmManager;
	private StructureManager StructureManager;
	private EnemyManager EnemyManager;
	private ItemManager ItemManager;
	private Tilemap TilemapGround;
	private Tilemap TilemapWalls;

	public void Initialize(
		GameManager GameManager,
		Player Player,
		TileManager TileManager,
		TurnManager TurnManager,
		LevelManager LevelManager,
		ChronoclasmManager ChronoclasmManager,
		StructureManager StructureManager,
		EnemyManager EnemyManager,
		ItemManager ItemManager,
		Tilemap TilemapGround,
		Tilemap TilemapWalls)
	{
		this.GameManager = GameManager;
		this.Player = Player;
		this.TileManager = TileManager;
		this.TurnManager = TurnManager;
		this.LevelManager = LevelManager;
		this.ChronoclasmManager = ChronoclasmManager;
		this.StructureManager = StructureManager;
		this.EnemyManager = EnemyManager;
		this.ItemManager = ItemManager;
		this.TilemapGround = TilemapGround;
		this.TilemapWalls = TilemapWalls;
	}

	public void ClearTransientState() => LitDynamite.Clear();

	public bool TryUseItemOnTile(Vector3Int TilePoint, Vector3 ClickedPosition)
	{
		ItemInfo Selected = Player.SelectedItemInfo;
		if (Selected == null)
			return false;
		if (Selected.Tag is ItemInfo.Tags.Extinguisher)
		{
			if (!IsPlayerAdjacentTo(ClickedPosition))
				return false;
			bool didSomething = false;
			if (GameManager.HasFireAtPosition(TilePoint)
				&& GameManager.TryExtinguishFire(TilePoint))
				didSomething = true;
			Item LitDynamiteItem = LitDynamite.Find(Dynamite => Dynamite != null && Dynamite.transform.position == ClickedPosition);
			if (LitDynamiteItem != null)
			{
				LitDynamite.Remove(LitDynamiteItem);
				SetDynamiteLitSprite(LitDynamiteItem, false);
				didSomething = true;
			}
			if (!didSomething)
				return false;
			Player.UseItem();
			CompleteWorldAction(UndoAfterTileItemAction);
			return true;
		}
		if (Selected.Tag is not (ItemInfo.Tags.Blowtorch or ItemInfo.Tags.Flamethrower))
			return false;
		if (!TileManager.IsInTileArea(TilePoint)
			|| LevelManager.HasWallAtPosition(TilePoint)
			|| GameManager.HasEnemyAtPosition(ClickedPosition)
			|| GameManager.HasFireAtPosition(TilePoint)
			|| ClickedPosition == Player.transform.position)
			return false;
		bool spawnedFire = Selected.Tag == ItemInfo.Tags.Flamethrower
			? TrySpawnFlamethrowerLine(ClickedPosition)
			: GameManager.TrySpawnFire(TilePoint, false, true);
		if (!spawnedFire)
			return false;
		Player.UseItem();
		CompleteWorldAction(UndoAfterTileItemAction);
		return true;
	}

	public bool TryUseItemOnPlayer(Vector3 ClickedPosition)
	{
		if (ClickedPosition != Player.transform.position
			|| !Player.ClickOnToUseItem())
			return false;
		CompleteWorldAction(UndoAfterItemAction);
		return true;
	}

	public bool TryUseItemOnVehicle(Vector3Int TilePoint)
	{
		Vehicle Vehicle = GameManager.GetVehicleAtPosition(TilePoint);
		if (Vehicle == null
			|| !IsPlayerAdjacentTo(Vehicle.transform.position)
			|| !Player.ClickOnVehicleToUseItem(Vehicle))
			return false;
		CompleteWorldAction("Undo history cleared after using an item on a vehicle.");
		return true;
	}

	public bool TryInteractWithStructure(Vector3Int TilePoint)
	{
		Structure Structure = StructureManager.GetStructureAtCell(TilePoint);
		if (Structure == null
			|| !Structure.Info.IsInteractable
			|| Structure.Info.IsLooted
			|| !Structure.IsAdjacentTo(Player.transform.position))
			return false;
		bool interacted = Structure.Info.Tag switch
		{
			StructureInfo.Tags.MedCrate => InteractMedCrate(Structure),
			_ => false,
		};
		if (!interacted)
			return false;
		Player.SpendEnergy(1);
		CompleteAction("Undo history cleared after structure interaction.");
		return true;
	}

	private bool InteractMedCrate(Structure Structure)
	{
		Structure.Info.IsLooted = true;
		Structure.UpdateSprite();
		ItemInfo MedKitInfo = new((int)ItemInfo.Tags.MedKit);
		Inventory Inventory = Player.InventoryUI.Inventory;
		if (Inventory != null && Inventory.TryAddItem(MedKitInfo))
			Player.InventoryUI.RefreshInventoryIcons();
		else
			GameManager.SpawnItem(MedKitInfo, Player.transform.position);
		return true;
	}

	public bool TryPlayerAttack(Vector3 ClickedPosition)
	{
		bool isInMeleeRange = IsPlayerAdjacentTo(ClickedPosition);
		bool isInRangedWeaponRange = Player.HasRange && TileManager.IsInRangedWeaponRange(ClickedPosition);
		if (!GameManager.HasEnemyAtPosition(ClickedPosition)
			|| !isInMeleeRange && !isInRangedWeaponRange
			|| Player.SelectedItemInfo?.Type is not ItemInfo.Types.Weapon
			|| !Player.HasUses
			|| IsDynamiteSelected())
			return false;
		if (Player.SelectedItemInfo.Tag is ItemInfo.Tags.Blowtorch or ItemInfo.Tags.Flamethrower)
		{
			Vector3Int TilePoint = TilemapGround.WorldToCell(ClickedPosition);
			if (LevelManager.HasWallAtPosition(TilePoint)
				|| GameManager.HasFireAtPosition(TilePoint)
				|| GameManager.HasVehicleAtPosition(ClickedPosition)
				|| ClickedPosition == Player.transform.position)
				return false;
			bool spawnedFire = Player.SelectedItemInfo.Tag is ItemInfo.Tags.Flamethrower
				? TrySpawnFlamethrowerLine(ClickedPosition)
				: GameManager.TrySpawnFire(TilePoint, false, true);
			if (!spawnedFire)
				return false;
			Player.AttackEntity();
			CompleteWorldAttack();
			return true;
		}
		if (Player.SelectedItemInfo.Tag == ItemInfo.Tags.Rock)
			GameManager.SpawnItem((int)Player.SelectedItemInfo.Tag, ClickedPosition);
		Enemy Enemy = GameManager.GetEnemyAtPosition(ClickedPosition);
		EnemyManager.HandleDamageToEnemy(Enemy, Player.GetDamagePointsAgainst(Enemy), Player.SelectedItemInfo.IsStunning);
		Player.AttackEntity();
		CompleteAttack();
		return true;
	}

	public bool TryThrowDynamite(Vector3Int TilePoint, Vector3 ClickedPosition)
	{
		if (!IsDynamiteSelected()
			|| !Player.HasUses
			|| ClickedPosition == Player.transform.position
			|| !TileManager.IsInTileArea(TilePoint))
			return false;
		Item SpawnedDynamite = GameManager.SpawnItem((int)ItemInfo.Tags.Dynamite, ClickedPosition);
		SetDynamiteLitSprite(SpawnedDynamite, true);
		LitDynamite.Add(SpawnedDynamite);
		Player.AttackEntity();
		CompleteAttack("Undo history cleared after throwing dynamite.");
		return true;
	}

	public bool TryBreakWall(Vector3Int TilePoint, Vector3 ClickedPosition)
	{
		if (!LevelManager.HasWallAtPosition(TilePoint)
			|| !IsPlayerAdjacentTo(ClickedPosition)
			|| Player.SelectedItemInfo == null
			|| !Player.HasUses)
			return false;
		Sprite WallSprite = TilemapWalls.GetSprite(TilePoint);
		WallInfo Wall = WallInfo.Get(WallSprite != null ? WallSprite.name : "");
		if (Wall == null
			|| !Wall.IsChoppable
			|| Player.SelectedItemInfo.Tag != Wall.ChopTool)
			return false;
		TilemapWalls.SetTile(TilePoint, null);
		if (Wall.DropItem != ItemInfo.Tags.Unknown)
			GameManager.SpawnItem((int)Wall.DropItem, ClickedPosition);
		Player.AttackEntity();
		CompleteWorldAction("Undo history cleared after breaking a wall.");
		return true;
	}

	public bool TryIgniteWall(Vector3Int TilePoint)
	{
		if (!IsFirestarterSelected()
			|| !Player.HasUses
			|| !LevelManager.HasWallAtPosition(TilePoint)
			|| GameManager.HasFireAtPosition(TilePoint))
			return false;
		Sprite WallSprite = TilemapWalls.GetSprite(TilePoint);
		WallInfo Wall = WallInfo.Get(WallSprite != null ? WallSprite.name : "");
		if (Wall == null || !Wall.IsFlammable || !HasFirestarterReachToWall(TilePoint))
			return false;
		Vector3 WallCenter = GridCoordinates.GetCellCenter(TilePoint);
		bool spawnedFire = Player.SelectedItemInfo.Tag is ItemInfo.Tags.Flamethrower
			? TrySpawnFlamethrowerLine(WallCenter)
			: GameManager.TrySpawnFire(TilePoint, false, true);
		if (!spawnedFire)
			return false;
		Player.UseItem();
		TileManager.TileDot.SetActive(false);
		CompleteWorldAction(UndoAfterTileItemAction);
		return true;
	}

	public bool IsFirestarterSelected() =>
		Player.SelectedItemInfo?.Tag is ItemInfo.Tags.Blowtorch or ItemInfo.Tags.Flamethrower;
	public bool IsDynamiteSelected() => Player.SelectedItemInfo?.Tag is ItemInfo.Tags.Dynamite;

	public bool IsValidFireTarget(Vector3Int Cell)
	{
		if (!GameManager.IsCellVisible(Cell)
			|| Cell == TilemapGround.WorldToCell(Player.transform.position)
			|| LevelManager.HasWallAtPosition(Cell)
			|| GameManager.HasFireAtPosition(Cell))
			return false;
		return !GameManager.HasEnemyAtPosition(GridCoordinates.GetCellCenter(Cell));
	}

	public bool IsValidDynamiteTarget(Vector3Int Cell) =>
		GameManager.IsCellVisible(Cell)
		&& Cell != TilemapGround.WorldToCell(Player.transform.position)
		&& !LevelManager.HasWallAtPosition(Cell);

	private bool TrySpawnFlamethrowerLine(Vector3 TargetWorldPosition)
	{
		Vector3Int TargetCell = TilemapGround.WorldToCell(TargetWorldPosition);
		if (!GameManager.TrySpawnFire(TargetCell, false, true))
			return false;
		List<Vector3Int> LineCells = TileManager.BresenhamsAlgorithm(Player.transform.position, TargetWorldPosition);
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		foreach (Vector3Int Cell in LineCells)
		{
			if (Cell != PlayerCell && Cell != TargetCell)
				GameManager.TrySpawnFire(Cell, false, true);
		}
		return true;
	}

	private bool HasFirestarterReachToWall(Vector3Int WallCell)
	{
		Vector3 WallCenter = GridCoordinates.GetCellCenter(WallCell);
		if (IsPlayerAdjacentTo(WallCenter))
			return true;
		if (Player.SelectedItemInfo.Tag is not ItemInfo.Tags.Flamethrower || !Player.HasRange)
			return false;
		return TileManager.HasLineOfSightToWall(Player.transform.position, WallCell, Player.WeaponRange, TilemapWalls);
	}

	private bool IsPlayerAdjacentTo(Vector3 Position)
	{
		Vector3Int PlayerCell = TilemapGround.WorldToCell(Player.transform.position);
		Vector3Int TargetCell = TilemapGround.WorldToCell(Position);
		return GridCoordinates.AreOrthogonallyAdjacent(PlayerCell, TargetCell);
	}

	public void RemoveLitDynamite(Item Dynamite) => LitDynamite.Remove(Dynamite);

	public void ProcessDynamiteExplosions()
	{
		if (LitDynamite.Count == 0)
			return;
		List<Item> ToProcess = new(LitDynamite);
		LitDynamite.Clear();
		foreach (Item Dynamite in ToProcess)
		{
			if (Dynamite == null)
				continue;
			Vector3 Position = Dynamite.transform.position;
			int damage = Dynamite.Info.DamagePoints;
			ItemManager.RemoveItemAtPosition(Dynamite);
			Destroy(Dynamite.gameObject);
			GameManager.ExplodeArea(Position, damage);
		}
		GameManager.RefreshVisibility();
	}

	private static void SetDynamiteLitSprite(Item Dynamite, bool isLit)
	{
		string Path = isLit ? "Sprites/dynamite_lit" : "Sprites/dynamite";
		Sprite Sprite = Resources.Load<Sprite>(Path);
		if (Sprite != null)
			Dynamite.GetComponent<SpriteRenderer>().sprite = Sprite;
	}

	private void CompleteAction(string UndoReason)
	{
		TurnManager.TurnTimer.StartTimer();
		GameManager.UpdateTileAreas();
		ChronoclasmManager.ClearUndoHistory(UndoReason);
	}

	private void CompleteWorldAction(string UndoReason)
	{
		GameManager.RefreshVisibility();
		CompleteAction(UndoReason);
	}

	private void CompleteAttack(string UndoReason = UndoAfterAttack)
	{
		TurnManager.TurnTimer.StartTimer();
		TileManager.TileDot.SetActive(false);
		GameManager.UpdateTargets();
		GameManager.UpdateTileAreas();
		ChronoclasmManager.ClearUndoHistory(UndoReason);
	}

	private void CompleteWorldAttack()
	{
		GameManager.RefreshVisibility();
		CompleteAttack();
	}
}
