using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A* pathfinding algorithm utilizing tilemaps
/// </summary>
public class AStar
{
	private readonly Tilemap TilemapGround;
	private readonly Tilemap TilemapWalls;
	private Node Current;
	private readonly HashSet<Node> OpenList = new();
	private readonly HashSet<Node> ClosedList = new();
	private readonly NodePriorityQueue OpenQueue = new();
	private readonly List<Node> NeighborScratch = new(8);
	private readonly HashSet<Vector3Int> EnemyCells = new();
	private readonly HashSet<Vector3Int> VehicleCells = new();
	private readonly HashSet<Vector3Int> StructureCells = new();
	private Dictionary<Vector3Int, Node> AllNodes;
	private Vector3Int StartPosition;
	private Vector3Int GoalPosition;
	private bool allowDiagonal = true;
	private Func<Vector3, bool> IsEnemyPassable = null;
	private const int MoveCostPerTile = 10;
	public AStar(Tilemap Ground, Tilemap Walls) 
	{
		TilemapGround = Ground;
		TilemapWalls = Walls;
	}
	/// <summary>
	/// Initializes A* algorithm with empty node dictionary
	/// </summary>
	public void Initialize()
	{
		AllNodes = new();
		GameManager.Instance.FillPathOccupancy(EnemyCells, VehicleCells, StructureCells);
	}
	/// <summary>
	/// Sets whether diagonal movement is allowed
	/// </summary>
	public void SetAllowDiagonal(bool flag) => allowDiagonal = flag;
	/// <summary>
	/// Sets a predicate that decides whether an enemy at a given world position should be
	/// treated as passable (e.g. a vehicle able to run it over). Null means all enemies block.
	/// </summary>
	public void SetEnemyPassabilityCheck(Func<Vector3, bool> Check) => IsEnemyPassable = Check;
	/// <summary>
	/// Gets all reachable positions within specified distance from start position
	/// </summary>
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
		Stack<Vector3Int> Path = ComputePath(Start, TilemapGround.CellToWorld(RandomGoal) + new Vector3(0.5f, 0.5f));
		// Verify the path actually moves the entity (more than just the starting position)
		return Path?.Count > 1 ? Path : null;
	}
	/// <summary>
	/// Computes path from Start to Goal position.
	/// Contains main loop for A* algorithm.
	/// Continues until OpenList is empty or valid Path is found.
	/// 1. FindNeighbors: 		Retrieves neighboring nodes of current node
	/// 2. ExamineNeighbors: 	Evaluates and updates neighbors based on algorithm's criteria
	/// 3. OpenQueue: 		Selects the next lowest-cost node
	/// 4. BuildPath: 		Constructs the final or closest partial path
	/// </summary>
    public Stack<Vector3Int> ComputePath(Vector3 Start, Vector3 Goal, bool allowPartialPath = false)
	{
		// Convert world positions to tilemap cell positions
		StartPosition = TilemapGround.WorldToCell(Start);
		GoalPosition = TilemapGround.WorldToCell(Goal);
		AllNodes ??= new();
		// Reset all nodes and lists
		AllNodes.Clear();
		Current = GetNode(StartPosition);
		OpenList.Clear();
		ClosedList.Clear();
		OpenQueue.Clear();
		Current.G = 0;
		Current.H = GetHeuristicCost(StartPosition, GoalPosition);
		Current.F = Current.H;
		OpenList.Add(Current);
		OpenQueue.Enqueue(Current);
		Node Closest = Current;
		while (OpenQueue.TryDequeue(OpenList, ClosedList, out Current))
		{
			OpenList.Remove(Current);
			if (Current.Position == GoalPosition)
				return BuildPath(Current);
			if (Current.H < Closest.H || (Current.H == Closest.H && Current.G < Closest.G))
				Closest = Current;
			ClosedList.Add(Current);
			List<Node> Neighbors = FindNeighbors(Current.Position, allowPartialPath);
			ExamineNeighbors(Neighbors, Current);
		}
		return allowPartialPath && Closest.Parent != null ? BuildPath(Closest) : null;
	}
	/// <summary>
	/// Finds all neighbors of current node
	/// </summary>
	private List<Node> FindNeighbors(Vector3Int ParentPosition, bool allowPartialPath = false)
	{
		// Reused across calls; consumers finish with the list before the next call
		List<Node> Neighbors = NeighborScratch;
		Neighbors.Clear();
		BoundsInt Size = TilemapGround.cellBounds;
		// These two for loops ensure all nodes are created around current node
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				if (x == 0 && y == 0)
					continue;
				if (!allowDiagonal && x != 0 && y != 0)
					continue;
				Vector3Int Position = ParentPosition - new Vector3Int(x, y);
				// Cheap tile checks first so entity scans only run for candidate cells
				if (Position.x < Size.min.x
					|| Position.x >= Size.max.x
					|| Position.y < Size.min.y
					|| Position.y >= Size.max.y
					|| TilemapWalls.HasTile(Position)
					|| GameManager.Instance.HasFireAtPosition(Position)
					|| StructureCells.Contains(Position))
					continue;
				Vector3 EntityPosition = ParentPosition - new Vector3(x - 0.5f, y - 0.5f);
				bool HasEnemy = EnemyCells.Contains(Position);
				bool HasVehicle = VehicleCells.Contains(Position);
				// Enemies the current mover can run over do not block the path
				bool EnemyBlocks = HasEnemy
								&& (IsEnemyPassable == null || !IsEnemyPassable(EntityPosition));
				bool IsEntityAtPosition = EnemyBlocks || HasVehicle;
				// For partial paths, allow movement through entity positions but mark them for stopping before
				if (IsEntityAtPosition && !allowPartialPath)
					continue;
				Node Neighbor = GetNode(Position);
				if (allowPartialPath && IsEntityAtPosition)
					Neighbor.HasEntity = true;
				Neighbors.Add(Neighbor);
			}
		}
		return Neighbors;
	}
	/// <summary>
	/// Examines neighbors of current node and updates their values
	/// </summary>
	private void ExamineNeighbors(List<Node> Neighbors, Node Current)
	{
		for (int i = 0; i < Neighbors.Count; i++)
		{
			Node Neighbor = Neighbors[i];
			int gScore = MoveCostPerTile;
			int candidateG = Current.G + gScore;
			if (ClosedList.Contains(Neighbor))
				continue;
			if (OpenList.Contains(Neighbor))
			{
				// Prefer straighter/tighter paths when the total move cost is the same
				if (IsBetterPath(Current, Neighbor, candidateG))
				{
					CalculateNodeValues(Current, Neighbor, GoalPosition, gScore);
					OpenQueue.Enqueue(Neighbor);
				}
			}
			else
			{
				CalculateNodeValues(Current, Neighbor, GoalPosition, gScore);
				OpenList.Add(Neighbor);
				OpenQueue.Enqueue(Neighbor);
			}
		}
	}
	/// <summary>
	/// Determines whether the new path to a node is better than its existing one
	/// </summary>
	private bool IsBetterPath(Node Parent, Node Neighbor, int candidateG)
	{
		if (candidateG < Neighbor.G)
			return true;
		if (candidateG > Neighbor.G)
			return false;
		int candidateAlignmentCost = Parent.AlignmentCost + GetAlignmentCost(Neighbor.Position, StartPosition, GoalPosition);
		if (candidateAlignmentCost < Neighbor.AlignmentCost)
			return true;
		if (candidateAlignmentCost > Neighbor.AlignmentCost)
			return false;
		int candidateTurns = GetTurnCount(Parent, Neighbor);
		if (candidateTurns < Neighbor.Turns)
			return true;
		if (candidateTurns > Neighbor.Turns)
			return false;
		return false;
	}
	private static Stack<Vector3Int> BuildPath(Node Current)
	{
		Stack<Vector3Int> FinalPath = new();
		while (Current != null)
		{
			FinalPath.Push(Current.Position);
			Current = Current.Parent;
		}
		return FinalPath;
	}
	/// <summary>
	/// Calculates G, H, and F values for neighbor node
	/// </summary>
	private void CalculateNodeValues(Node Parent, Node Neighbor, Vector3Int GoalPosition, int cost)
	{
		// Sets the parent node
		Neighbor.Parent = Parent;
		// Calculates this node's G cost, the parent's G cost + what it costs to move to this node
		Neighbor.G = Parent.G + cost;
		// H is calculated, it is the distance from this node to the goal * 10
		Neighbor.H = GetHeuristicCost(Neighbor.Position, GoalPosition);
		// F is calculated, it is G + H
		Neighbor.F = Neighbor.G + Neighbor.H;
		// Track turn count to prefer straighter paths when costs are tied
		Neighbor.Turns = GetTurnCount(Parent, Neighbor);
		// Prefer paths that stay closer to the ideal line from start to goal when costs are tied
		Neighbor.AlignmentCost = Parent.AlignmentCost + GetAlignmentCost(Neighbor.Position, StartPosition, GoalPosition);
	}
	/// <summary>
	/// Gets heuristic cost for the current movement rules
	/// </summary>
	private int GetHeuristicCost(Vector3Int position, Vector3Int GoalPosition)
	{
		int dx = Math.Abs(position.x - GoalPosition.x);
		int dy = Math.Abs(position.y - GoalPosition.y);
		int distance = allowDiagonal ? Math.Max(dx, dy) : dx + dy;
		return distance * MoveCostPerTile;
	}
	/// <summary>
	/// Counts turns along the current path to prefer straighter routes
	/// </summary>
	private int GetTurnCount(Node Parent, Node Neighbor)
	{
		if (Parent == null || Parent.Parent == null)
			return 0;
		Vector3Int PreviousDirection = Parent.Position - Parent.Parent.Position;
		Vector3Int CurrentDirection = Neighbor.Position - Parent.Position;
		int turn = PreviousDirection == CurrentDirection ? 0 : 1;
		return Parent.Turns + turn;
	}
	/// <summary>
	/// Gets a lower score for positions closer to the start-goal line
	/// </summary>
	private int GetAlignmentCost(Vector3Int position, Vector3Int StartPosition, Vector3Int GoalPosition)
	{
		Vector3Int LineVector = GoalPosition - StartPosition;
		if (LineVector == Vector3Int.zero)
			return 0;
		Vector3Int PositionVector = position - StartPosition;
		int cross = Math.Abs(PositionVector.x * LineVector.y - PositionVector.y * LineVector.x);
		return cross;
	}
	/// <summary>
	/// Gets or creates node at specified position
	/// </summary>
	private Node GetNode(Vector3Int Position)
	{
		if (AllNodes.TryGetValue(Position, out Node Node))
			return Node;
		Node = new(Position);
		AllNodes.Add(Position, Node);
		return Node;
	}
}
internal sealed class NodePriorityQueue
{
	private readonly struct Entry
	{
		public readonly Node Node;
		public readonly int F;
		public readonly int AlignmentCost;
		public readonly int Turns;
		public readonly int H;
		public Entry(Node Node)
		{
			this.Node = Node;
			F = Node.F;
			AlignmentCost = Node.AlignmentCost;
			Turns = Node.Turns;
			H = Node.H;
		}
	}
	private readonly List<Entry> Heap = new();
	public void Clear() => Heap.Clear();
	public void Enqueue(Node Node)
	{
		Entry Added = new(Node);
		Heap.Add(Added);
		int index = Heap.Count - 1;
		while (index > 0)
		{
			int parent = (index - 1) / 2;
			if (Compare(Heap[parent], Added) <= 0)
				break;
			Heap[index] = Heap[parent];
			index = parent;
		}
		Heap[index] = Added;
	}
	public bool TryDequeue(HashSet<Node> OpenNodes, HashSet<Node> ClosedNodes, out Node Node)
	{
		while (Heap.Count > 0)
		{
			Entry First = RemoveFirst();
			Node = First.Node;
			if (!OpenNodes.Contains(Node) || ClosedNodes.Contains(Node))
				continue;
			if (First.F != Node.F
				|| First.AlignmentCost != Node.AlignmentCost
				|| First.Turns != Node.Turns
				|| First.H != Node.H)
				continue;
			return true;
		}
		Node = null;
		return false;
	}
	private Entry RemoveFirst()
	{
		Entry First = Heap[0];
		int lastIndex = Heap.Count - 1;
		Entry Last = Heap[lastIndex];
		Heap.RemoveAt(lastIndex);
		if (Heap.Count == 0)
			return First;
		int index = 0;
		while (true)
		{
			int left = index * 2 + 1;
			if (left >= Heap.Count)
				break;
			int right = left + 1;
			int child = right < Heap.Count && Compare(Heap[right], Heap[left]) < 0 ? right : left;
			if (Compare(Last, Heap[child]) <= 0)
				break;
			Heap[index] = Heap[child];
			index = child;
		}
		Heap[index] = Last;
		return First;
	}
	private static int Compare(Entry Left, Entry Right)
	{
		int comparison = Left.F.CompareTo(Right.F);
		if (comparison != 0) return comparison;
		comparison = Left.AlignmentCost.CompareTo(Right.AlignmentCost);
		if (comparison != 0) return comparison;
		comparison = Left.Turns.CompareTo(Right.Turns);
		if (comparison != 0) return comparison;
		comparison = Left.H.CompareTo(Right.H);
		if (comparison != 0) return comparison;
		comparison = Left.Node.Position.x.CompareTo(Right.Node.Position.x);
		return comparison != 0 ? comparison : Left.Node.Position.y.CompareTo(Right.Node.Position.y);
	}
}
public class Node
{
	public int G { get; set; }
	public int H { get; set; }
	public int F { get; set; }
	public int Turns { get; set; }
	public int AlignmentCost { get; set; }
	public Node Parent { get; set; }
	public Vector3Int Position { get; set; }
	public bool HasEntity { get; set; } = false;
	public Node(Vector3Int Position) => this.Position = Position;
}
