using NUnit.Framework;
using UnityEngine;

public class GridEntityRegistryTests
{
	[Test]
	public void UpdateCell_MovesEntityBetweenBuckets()
	{
		GameObject Object = new("RegistryEntity");
		try
		{
			Transform Entity = Object.transform;
			GridEntityRegistry<Transform> Registry = new(Entity => GridCoordinates.GetCell(Entity.position));
			Vector3Int Start = Vector3Int.zero;
			Vector3Int End = Vector3Int.right;
			Registry.Add(Entity, Start);

			Entity.position = GridCoordinates.GetCellCenter(End);
			Registry.UpdateCell(Entity, End);

			Assert.That(Registry.Contains(Start), Is.False);
			Assert.That(Registry.GetFirst(End), Is.SameAs(Entity));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(Object);
		}
	}

	[Test]
	public void GetFirst_ReindexesEntityMovedWithoutNotification()
	{
		GameObject Object = new("RegistryEntity");
		try
		{
			Transform Entity = Object.transform;
			GridEntityRegistry<Transform> Registry = new(Tracked => GridCoordinates.GetCell(Tracked.position));
			Registry.Add(Entity, Vector3Int.zero);
			Entity.position = GridCoordinates.GetCellCenter(Vector3Int.right);

			Assert.That(Registry.GetFirst(Vector3Int.zero), Is.Null);
			Assert.That(Registry.GetFirst(Vector3Int.right), Is.SameAs(Entity));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(Object);
		}
	}

	[Test]
	public void Registry_AllowsMultipleEntitiesInOneCell()
	{
		GameObject FirstObject = new("FirstRegistryEntity");
		GameObject SecondObject = new("SecondRegistryEntity");
		try
		{
			GridEntityRegistry<Transform> Registry = new(Entity => GridCoordinates.GetCell(Entity.position));
			Registry.Add(FirstObject.transform, Vector3Int.zero);
			Registry.Add(SecondObject.transform, Vector3Int.zero);

			Assert.That(Registry.Entities, Has.Count.EqualTo(2));
			Assert.That(Registry.Contains(Vector3Int.zero), Is.True);
		}
		finally
		{
			Object.DestroyImmediate(FirstObject);
			Object.DestroyImmediate(SecondObject);
		}
	}
}
