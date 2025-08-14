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
	/// <summary>
	/// Computes a path to a random reachable position within specified distance
	/// Used when enemy cannot path to player
	/// </summary>
	public Stack<Vector3Int> ComputeRandomPath(Vector3 Start, int maxDistance)
	{
		// Get all reachable positions within the specified distance
		Dictionary<Vector3Int, Node> ReachableArea = GetReachableAreaByDistance(Start, maxDistance);
		// Remove the starting position from candidates
		Vector3Int StartPosition = TilemapGround.WorldToCell(Start);
		ReachableArea.Remove(StartPosition);
		// If no reachable positions, return null
		if (ReachableArea.Count == 0)
			return null;
		// Convert to list and pick a random position
		List<Vector3Int> Positions = new(ReachableArea.Keys);
		int randomIndex = UnityEngine.Random.Range(0, Positions.Count);
		Vector3Int RandomGoal = Positions[randomIndex];
		// Compute path to the random goal
		Stack<Vector3Int> path = ComputePath(Start, TilemapGround.CellToWorld(RandomGoal) + new Vector3(0.5f, 0.5f));
		// Verify the path actually moves the entity (more than just the starting position)
		if (path != null && path.Count <= 1)
			return null;
		return path;
	}
    public Stack<Vector3Int> ComputePath(Vector3 Start, Vector3 Goal, bool allowPartialPath = false)
    {
        StartPosition = TilemapGround.WorldToCell(Start);
        GoalPosition = TilemapGround.WorldToCell(Goal);
        AllNodes.Clear();
        Current = GetNode(StartPosition);
        // For nodes to be looked at later
        OpenList = new();
        // For examined nodes
        ClosedList = new();
        // Adds the current node to OpenList (has been examined)
        OpenList.Add(Current);
        Path = null;
        while (OpenList.Count > 0 && Path == null)
        {
			List<Node> Neighbors = FindNeighbors(Current.Position, allowPartialPath);
			ExamineNeighbors(Neighbors, Current);
			UpdateCurrentTile(ref Current);
			Path = GeneratePath(Current, allowPartialPath);
		}
		if (Path != null)
		{
			return Path;
		}
		return null;
	}
	private List<Node> FindNeighbors(Vector3Int ParentPosition, bool allowPartialPath = false)
	{
		List<Node> Neighbors = new();
		// These two for loops ensure all nodes are created around current node
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				Vector3Int Position = ParentPosition - new Vector3Int(x, y);
				Vector3 EntityPosition = ParentPosition - new Vector3(x - 0.5f, y - 0.5f);
				bool IsEntityAtPosition = GameManager.Instance.HasEnemyAtPosition(EntityPosition)
									   || GameManager.Instance.HasVehicleAtPosition(EntityPosition);
				if ((y != 0 || x != 0)
					&& (allowDiagonal || (!allowDiagonal && (y == 0 || x == 0))))
				{
					BoundsInt Size = TilemapGround.cellBounds;
					// If node is within bounds of the grid and if there is no wall tile, then add it to the neighbors list
					// For partial paths, allow movement through enemy positions but mark them for stopping before
					if (Position.x >= Size.min.x
						&& Position.x < Size.max.x
						&& Position.y >= Size.min.y
						&& Position.y < Size.max.y
						&& !TilemapWalls.HasTile(Position)
						&& (!IsEntityAtPosition || allowPartialPath))
					{
						Node Neighbor = GetNode(Position);
						// Mark if this node has an entity for partial path logic
						if (allowPartialPath && IsEntityAtPosition)
						{
							Neighbor.HasEntity = true;
						}
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
				// An extra check for OpenList containing the neighbor
				if (!OpenList.Contains(Neighbor))
				{
					OpenList.Add(Neighbor);
				}
			}
		}
	}
	private void UpdateCurrentTile(ref Node Current)
	{
		// The current node is removed from OpenList
		OpenList.Remove(Current);
		// The current node is added to ClosedList
		ClosedList.Add(Current);
		// If the OpenList has nodes in it, then sort them by F value
		if (OpenList.Count > 0)
		{
			// Orders the list by F value to make it easier to pick node with lowest F value
			Current = OpenList.OrderBy(x => x.F).First();
		}
	}
	private Stack<Vector3Int> GeneratePath(Node Current, bool allowPartialPath = false)
	{
		// If the current node is goal, then path is found
		if (Current.Position == GoalPosition)
		{
			Stack<Vector3Int> FinalPath = new();
			// Adds the nodes to the final path
			while (Current != null)
			{
				// Adds the current node to the final path
				FinalPath.Push(Current.Position);
				// Find node's parent to retrace the path back to the start, so a complete path is formed
				Current = Current.Parent;
			}
			return FinalPath;
		}
		// For partial paths, if no more nodes to explore but found path closer to the goal
		else if (allowPartialPath && OpenList.Count == 0 && Current.Parent != null)
		{
			// Find the node that got closest to the goal
			Node ClosestNode = Current;
			Node TempNode = Current;
			while (TempNode != null)
			{
				if (TempNode.H < ClosestNode.H)
				{
					ClosestNode = TempNode;
				}
				TempNode = TempNode.Parent;
			}
			// Generate partial path to closest reachable point, checking for entities
			Stack<Vector3Int> PartialPath = new();
			Node PathNode = ClosestNode;
			while (PathNode != null)
			{
				// Add current node to path, validate movement at execution time
				PartialPath.Push(PathNode.Position);
				PathNode = PathNode.Parent;
			}
			// Only return partial path if it has meaningful progress
			if (PartialPath.Count > 1)
			{
				return PartialPath;
			}
		}
		return null;
	}
	private void CalcValues(Node Parent, Node Neighbor, Vector3Int GoalPos, int cost)
	{
		// Sets the parent node
		Neighbor.Parent = Parent;
		// Calculates this node's G cost, the parent's G cost + what it costs to move to this node
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
	public bool HasEntity { get; set; } = false;
	public Node(Vector3Int Position)
	{
		this.Position = Position;
	}
}