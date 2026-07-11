using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VisibilityManagerTests
{
	[Test]
	public void LightRadii_MatchNightVisibilitySources()
	{
		BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
		FieldInfo FlareRadiusField = typeof(VisibilityManager).GetField("flareRadius", Flags);
		FieldInfo LightrodRadiusField = typeof(VisibilityManager).GetField("lightrodRadius", Flags);

		Assert.That(FlareRadiusField, Is.Not.Null);
		Assert.That(LightrodRadiusField, Is.Not.Null);
		Assert.That(FlareRadiusField.GetRawConstantValue(), Is.EqualTo(1));
		Assert.That(LightrodRadiusField.GetRawConstantValue(), Is.EqualTo(2));
	}

	[Test]
	public void GetFlareLightKey_PreservesIdentityWhenFlareMovesToGround()
	{
		GameObject ManagerObject = new("VisibilityManagerTests");
		try
		{
			VisibilityManager Manager = ManagerObject.AddComponent<VisibilityManager>();
			MethodInfo GetFlareLightKeyMethod = typeof(VisibilityManager).GetMethod(
				"GetFlareLightKey",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(GetFlareLightKeyMethod, Is.Not.Null);

			ItemInfo FirstFlare = new((int)ItemInfo.Tags.Flare);
			ItemInfo SecondFlare = new((int)ItemInfo.Tags.Flare);
			int firstInventoryKey = (int)GetFlareLightKeyMethod.Invoke(Manager, new object[] { FirstFlare });
			int firstGroundKey = (int)GetFlareLightKeyMethod.Invoke(Manager, new object[] { FirstFlare });
			int secondFlareKey = (int)GetFlareLightKeyMethod.Invoke(Manager, new object[] { SecondFlare });

			Assert.That(firstGroundKey, Is.EqualTo(firstInventoryKey));
			Assert.That(secondFlareKey, Is.Not.EqualTo(firstInventoryKey));
		}
		finally
		{
			Object.DestroyImmediate(ManagerObject);
		}
	}

	[Test]
	public void AddCrossArea_IncludesOnlySourceAndDirectlyAdjacentCells()
	{
		GameObject ManagerObject = new("VisibilityManagerTests");
		try
		{
			VisibilityManager Manager = ManagerObject.AddComponent<VisibilityManager>();
			MethodInfo AddCrossAreaMethod = typeof(VisibilityManager).GetMethod(
				"AddCrossArea",
				BindingFlags.Instance | BindingFlags.NonPublic);
			FieldInfo VisibleCellsField = typeof(VisibilityManager).GetField(
				"VisibleCells",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(AddCrossAreaMethod, Is.Not.Null);
			Assert.That(VisibleCellsField, Is.Not.Null);

			Vector3Int SourceCell = Vector3Int.zero;
			AddCrossAreaMethod.Invoke(Manager, new object[] { SourceCell, 1 });
			HashSet<Vector3Int> VisibleCells = (HashSet<Vector3Int>)VisibleCellsField.GetValue(Manager);

			Assert.That(VisibleCells, Is.EquivalentTo(new[]
			{
				SourceCell,
				SourceCell + Vector3Int.left,
				SourceCell + Vector3Int.right,
				SourceCell + Vector3Int.up,
				SourceCell + Vector3Int.down,
			}));
		}
		finally
		{
			Object.DestroyImmediate(ManagerObject);
		}
	}

	[TestCase(1, 9)]
	[TestCase(2, 25)]
	public void AddSquareArea_IncludesEveryCellInSquare(int radius, int expectedCellCount)
	{
		GameObject ManagerObject = new("VisibilityManagerTests");
		try
		{
			VisibilityManager Manager = ManagerObject.AddComponent<VisibilityManager>();
			MethodInfo AddSquareAreaMethod = typeof(VisibilityManager).GetMethod(
				"AddSquareArea",
				BindingFlags.Instance | BindingFlags.NonPublic);
			FieldInfo VisibleCellsField = typeof(VisibilityManager).GetField(
				"VisibleCells",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(AddSquareAreaMethod, Is.Not.Null);
			Assert.That(VisibleCellsField, Is.Not.Null);

			Vector3Int SourceCell = Vector3Int.zero;
			AddSquareAreaMethod.Invoke(Manager, new object[] { SourceCell, radius });
			HashSet<Vector3Int> VisibleCells = (HashSet<Vector3Int>)VisibleCellsField.GetValue(Manager);

			Assert.That(VisibleCells, Has.Count.EqualTo(expectedCellCount));
			for (int x = -radius; x <= radius; x++)
			{
				for (int y = -radius; y <= radius; y++)
					Assert.That(VisibleCells, Does.Contain(SourceCell + new Vector3Int(x, y, 0)));
			}
		}
		finally
		{
			Object.DestroyImmediate(ManagerObject);
		}
	}
}
