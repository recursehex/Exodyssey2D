using System.Collections;
using UnityEngine;

public static class GridMovement
{
	public static IEnumerator MoveToCell(Transform MovingTransform, Vector3Int Cell, float speed)
	{
		Vector3 Destination = GridCoordinates.GetCellCenter(Cell);
		while (MovingTransform.position != Destination)
		{
			MovingTransform.position = Vector3.MoveTowards(
				MovingTransform.position,
				Destination,
				speed * Time.deltaTime);
			yield return null;
		}
		MovingTransform.position = Destination;
	}
}
