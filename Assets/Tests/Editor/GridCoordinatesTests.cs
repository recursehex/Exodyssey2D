using NUnit.Framework;
using UnityEngine;

public class GridCoordinatesTests
{
	[Test]
	public void GetCellCenter_OffsetsToMiddleOfUnitCell()
	{
		Assert.That(GridCoordinates.GetCellCenter(new Vector3Int(-2, 3, 0)),
			Is.EqualTo(new Vector3(-1.5f, 3.5f, 0f)));
	}

	[TestCase(-4, -4, true)]
	[TestCase(4, 4, true)]
	[TestCase(-5, 0, false)]
	[TestCase(0, 5, false)]
	public void IsInsideGrid_UsesConfiguredInclusiveBounds(int x, int y, bool expected)
	{
		Assert.That(GridCoordinates.IsInsideGrid(new Vector3Int(x, y, 0)), Is.EqualTo(expected));
	}

	[TestCase(0, 0, 1, 0, true)]
	[TestCase(0, 0, 1, 1, false)]
	[TestCase(0, 0, 0, 0, false)]
	public void AreOrthogonallyAdjacent_RequiresExactlyOneCardinalStep(
		int firstX,
		int firstY,
		int secondX,
		int secondY,
		bool expected)
	{
		Vector3Int First = new(firstX, firstY, 0);
		Vector3Int Second = new(secondX, secondY, 0);
		Assert.That(GridCoordinates.AreOrthogonallyAdjacent(First, Second), Is.EqualTo(expected));
	}
}
