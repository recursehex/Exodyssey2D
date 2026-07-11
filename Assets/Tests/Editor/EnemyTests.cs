using NUnit.Framework;
using UnityEngine;

public class EnemyTests
{
	[TestCase(0, 0, 1, 0, true)]
	[TestCase(0, 0, 0, -1, true)]
	[TestCase(0, 0, 1, 1, false)]
	[TestCase(0, 0, 2, 0, false)]
	[TestCase(0, 0, 0, 0, false)]
	public void AreCellsAdjacentForAttack_RequiresOneOrthogonalStep(
		int enemyX,
		int enemyY,
		int targetX,
		int targetY,
		bool expected)
	{
		Vector3Int EnemyCell = new(enemyX, enemyY, 0);
		Vector3Int TargetCell = new(targetX, targetY, 0);

		Assert.That(GridCoordinates.AreOrthogonallyAdjacent(EnemyCell, TargetCell), Is.EqualTo(expected));
	}
}
