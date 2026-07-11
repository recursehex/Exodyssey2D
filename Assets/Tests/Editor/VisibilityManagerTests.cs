using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VisibilityManagerTests
{
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
