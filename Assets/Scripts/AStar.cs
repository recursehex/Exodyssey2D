using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

/// <summary>
/// A* pathfinding algorithm utilizing tilemaps
/// </summary>
public class AStar
{
	private readonly Tilemap TilemapGround;
	private readonly Tilemap TilemapWalls;
	private Node Current;
	private Stack<Vector3Int> Path;
	private HashSet<Node> OpenList;
	private HashSet<Node> ClosedList;
	private Dictionary<Vector3Int, Node> AllNodes;
	private Vector3Int StartPosition;
	private Vector3Int GoalPosition;
	private bool allowDiagonal = true;
	public AStar(Tilemap Ground, Tilemap Walls) 
	{
		TilemapGround = Ground;
		TilemapWalls = Walls;
	}
	public void Initialize()
	{
		AllNodes = new();
	}
	public void SetAllowDiagonal(bool flag)
	{
		allowDiagonal = flag;
	}
	public Dictionary<Vector3Int, Node> GetReachableAreaByDistance(Vector3 Start, int distance)
	{
		Vector3Int StartInt = TilemapGround.WorldToCell(Start);
		Dictionary<Vector3Int, Node> ReachableArea = new() { [StartInt] = new Node(StartInt) };
		List<Node> CurrentLayer = new() { new(StartInt) };
		// Expand outward to the specified distance
		for (int d = 0; d < distance; d++)
		{
			List<Node> NextLayer = new();
			foreach (Node Node in CurrentLayer)
			{
				// Get adjacent reachable nodes
				foreach (Node Neighbor in FindNeighbors(Node.Position))
				{
					// Skip if already visited
					if (!ReachableArea.ContainsKey(Neighbor.Position))
					{
						ReachableArea.Add(Neighbor.Position, Neighbor);
						NextLayer.Add(Neighbor);
					}
				}
			}
			// No more nodes to explore
			if (NextLayer.Count == 0)
				break;
			CurrentLayer = NextLayer;
		}
		return ReachableArea;
	}
	public Stack<Vector3Int> ComputePath(Vector3 Start, Vector3 Goal)
	{
		StartPosition = TilemapGround.WorldToCell(Start);
		GoalPosition = TilemapGround.WorldToCell(Goal);
		AllNodes.Clear();
		Current = GetNode(StartPosition);
		// Creates an open list for nodes that could be looked at later
		OpenList = new();
		// Creates a closed list for examined nodes
		ClosedList = new();
		foreach (KeyValuePair<Vector3Int, Node> Node in AllNodes)
		{
			Node.Value.Parent = null;
		}
		AllNodes.Clear();
		// Adds the current node to the open list (has been examined)
		OpenList.Add(Current);
		Path = null;
		while (OpenList.Count > 0 && Path == null)
		{
			List<Node> Neighbors = FindNeighbors(Current.Position);
			ExamineNeighbors(Neighbors, Current);
			UpdateCurrentTile(ref Current);
			Path = GeneratePath(Current);
		}
		if (Path != null)
		{
			return Path;
		}
		return null;
	}
	private List<Node> FindNeighbors(Vector3Int ParentPosition)
	{
		List<Node> Neighbors = new();
		// These two for loops make sure that all nodes are created around the current node
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				Vector3Int Position = ParentPosition - new Vector3Int(x, y, 0);
				Vector3 EntityPosition = ParentPosition - new Vector3(x - 0.5f, y - 0.5f, 0);
				bool IsEntityAtPosition = GameManager.Instance.HasEnemyAtPosition(EntityPosition)
									   || GameManager.Instance.HasVehicleAtPosition(EntityPosition);
				if ((y != 0 || x != 0)
					&& (allowDiagonal || (!allowDiagonal && (y == 0 || x == 0))))
				{
					BoundsInt Size = TilemapGround.cellBounds;
					// If node is within bounds of the grid and if there is no wall tile and no enemy or vehicle there, then add it to the neighbors list
					if (Position.x >= Size.min.x
						&& Position.x < Size.max.x
						&& Position.y >= Size.min.y
						&& Position.y < Size.max.y
						&& !TilemapWalls.HasTile(Position)
						&& !IsEntityAtPosition)
					{
						Node Neighbor = GetNode(Position);
						Neighbors.Add(Neighbor);
					}
				}
			}
		}
		return Neighbors;
	}
	private void ExamineNeighbors(List<Node> Neighbors, Node Current)
	{	
		for (int i = 0; i < Neighbors.Count; i++)
		{
			Node Neighbor = Neighbors[i];
			int gScore = 10;
			if (OpenList.Contains(Neighbor))
			{
				if (Current.G + gScore < Neighbor.G)
				{
					CalcValues(Current, Neighbor, GoalPosition, gScore);
				}
			}
			else if (!ClosedList.Contains(Neighbor))
			{
				CalcValues(Current, Neighbor, GoalPosition, gScore);
				// An extra check for openList containing the neighbor
				if (!OpenList.Contains(Neighbor))
				{
					// Node is added to the openList
					OpenList.Add(Neighbor);
				}
			}
		}
	}
	private void UpdateCurrentTile(ref Node Current)
	{
		// The current node is removed from the openList
		OpenList.Remove(Current);
		// The current node is added to the closedList
		ClosedList.Add(Current);
		// If the openList has nodes in it, then sort them by F value
		if (OpenList.Count > 0)
		{
			// Orders the list by the F value to make it easier to pick the node with the lowest F value
			Current = OpenList.OrderBy(x => x.F).First();
		}
	}
	private Stack<Vector3Int> GeneratePath(Node Current)
	{
		// If the current node is the goal, then a path is found
		if (Current.Position == GoalPosition)
		{
			// Creates a stack to contain the final path
			Stack<Vector3Int> FinalPath = new();
			// Adds the nodes to the final path
			while (Current != null)
			{
				// Adds the current node to the final path
				FinalPath.Push(Current.Position);
				// Find the parent of the node, this retraces the whole path back to the start,
				// by doing so, a complete path is formed
				Current = Current.Parent;
			}
			// Returns the complete path
			return FinalPath;
		}
		return null;
	}
	private void CalcValues(Node Parent, Node Neighbor, Vector3Int GoalPos, int cost)
	{
		// Sets the parent node
		Neighbor.Parent = Parent;
		// Calculates this node's g cost, the parent's g cost + what it costs to move to this node
		Neighbor.G = Parent.G + cost;
		// H is calculated, it is the distance from this node to the goal * 10
		Neighbor.H = (Math.Abs(Neighbor.Position.x - GoalPos.x) + Math.Abs(Neighbor.Position.y - GoalPos.y)) * 10;
		// F is calculated, it is G + H
		Neighbor.F = Neighbor.G + Neighbor.H;
	}
	private Node GetNode(Vector3Int Position)
	{
		if (AllNodes.ContainsKey(Position))
		{
			return AllNodes[Position];
		}
		else
		{
			Node Node = new(Position);
			AllNodes.Add(Position, Node);
			return Node;
		}
	}
}
public class Node
{
	public int G { get; set; }
	public int H { get; set; }
	public int F { get; set; }
	public Node Parent { get; set; }
	public Vector3Int Position { get; set; }
	public Node(Vector3Int Position)
	{
		this.Position = Position;
	}
}