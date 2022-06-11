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
    [SerializeField]
    public Tilemap tilemapGround;

    [SerializeField]
    public Tilemap tilemapWalls;

    private Node current;

    private Stack<Vector3Int> path;

    private HashSet<Node> openList;

    private HashSet<Node> closedList;

    private Dictionary<Vector3Int, Node> allNodes;

    private static HashSet<Vector3Int> noDiagonalTiles;

    private Vector3Int startPos, goalPos;

    private bool allowDiagonal = true;

    public static HashSet<Vector3Int> NoDiagonalTiles
    {
        get
        {
            return noDiagonalTiles;
        }
    }

    public void Initialize()
    {
        allNodes = new Dictionary<Vector3Int, Node>();

        noDiagonalTiles = new HashSet<Vector3Int>();
    }

    public void SetAllowDiagonal(bool f)
    {
        allowDiagonal = f;
    }

    public Dictionary<Vector3Int, Node> GetReachableAreaByDistance(Vector3 start, int dst)
    {
        Vector3Int s = tilemapGround.WorldToCell(start);

        Dictionary<Vector3Int, Node> res = new Dictionary<Vector3Int, Node>();

        List<Node> toExamineList = new List<Node>();

        toExamineList.Add(new Node(s));

        // checks within AP distance limit for reachable tiles, pseudo-recursive
        for (int i = 0; i < dst; i++)
        {
            List<Node> newNeighbors = new List<Node>();

            for (int j = 0; j < toExamineList.Count; j++)
            {
                List<Node> neighbors = FindNeighbors(toExamineList[j].Position);
                for (int k = 0; k < neighbors.Count; k++)
                {
                    newNeighbors.Add(neighbors[k]);
                    res[neighbors[k].Position] = neighbors[k];
                }
            }

            toExamineList = newNeighbors;
        }
        return res;
    }

    public Stack<Vector3Int> ComputePath(Vector3 start, Vector3 goal)
    {
        startPos = tilemapGround.WorldToCell(start);
        goalPos = tilemapGround.WorldToCell(goal);

        allNodes.Clear();

        current = GetNode(startPos);

        // creates an open list for nodes that we might want to look at later
        openList = new HashSet<Node>();

        // creates a closed list for nodes that we have examined
        closedList = new HashSet<Node>();

        foreach (KeyValuePair<Vector3Int, Node> node in allNodes)
        {
            node.Value.Parent = null;
        }

        allNodes.Clear();

        // adds the current node to the open list (we have examined it)
        openList.Add(current);

        path = null;

        while (openList.Count > 0 && path == null)
        {
            List<Node> neighbors = FindNeighbors(current.Position);

            ExamineNeighbors(neighbors, current);

            UpdateCurrentTile(ref current);

            path = GeneratePath(current);
        }

        if (path != null)
        {
            return path;
        }

        return null;
    }

    private List<Node> FindNeighbors(Vector3Int parentPosition)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++) // these two for loops makes sure that we make all nodes around our current node
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int p = new Vector3Int(parentPosition.x - x, parentPosition.y - y, parentPosition.z);
                if ((y != 0 || x != 0) && (allowDiagonal || (!allowDiagonal && (y == 0 || x == 0))))
                {
                    BoundsInt size = tilemapGround.cellBounds;

                    // if node is within bounds of the grid & if there is no wall tile there & if player has any AP, add it to the neighbors list
                    if (p.x >= size.min.x && p.x < size.max.x && p.y >= size.min.y && p.y < size.max.y && (!tilemapWalls.HasTile(p)))
                    {
                        Node neighbour = GetNode(p);
                        neighbors.Add(neighbour);
                    }

                }
            }
        }
        return neighbors;
    }

    private void ExamineNeighbors(List<Node> neighbors, Node current)
    {
        for (int i = 0; i < neighbors.Count; i++)
        {
            Node neighbour = neighbors[i];

            if (!ConnectedDiagonally(current, neighbour))
            {
                continue;
            }

            int gScore = DetermineGScore(neighbour.Position, current.Position);

            if (gScore == 14 && NoDiagonalTiles.Contains(neighbour.Position) && NoDiagonalTiles.Contains(current.Position))
            {
                continue;
            }

            if (openList.Contains(neighbour))
            {
                if (current.G + gScore < neighbour.G)
                {
                    CalcValues(current, neighbour, goalPos, gScore);
                }
            }
            else if (!closedList.Contains(neighbour))
            {
                CalcValues(current, neighbour, goalPos, gScore);

                if (!openList.Contains(neighbour)) // an extra check for openlist containing the neighbour
                {
                    openList.Add(neighbour); // then we need to add the node to the openlist
                }
            }
        }
    }

    private bool ConnectedDiagonally(Node currentNode, Node neighbour)
    {
        // gets the direction
        Vector3Int direction = currentNode.Position - neighbour.Position;

        // gets the positions of the nodes
        Vector3Int first = new Vector3Int(currentNode.Position.x + (direction.x * -1), currentNode.Position.y, currentNode.Position.z);
        Vector3Int second = new Vector3Int(currentNode.Position.x, currentNode.Position.y + (direction.y * -1), currentNode.Position.z);

        // the nodes are empty
        return true;
    }

    private int DetermineGScore(Vector3Int neighbour, Vector3Int current)
    {
        int gScore = 0;

        int x = current.x - neighbour.x;
        int y = current.y - neighbour.y;

        if (Math.Abs(x - y) % 2 == 1)
        {
            gScore = 10; // the gscore for a vertical or horizontal node is 10
        }
        else
        {
            gScore = 14;
        }

        return gScore;
    }

    private void UpdateCurrentTile(ref Node current)
    {
        // the current node is removed fromt he open list
        openList.Remove(current);

        // the current node is added to the closed list
        closedList.Add(current);

        if (openList.Count > 0) // if the openlist has nodes on it, then we need to sort them by its F value
        {
            current = openList.OrderBy(x => x.F).First(); // orders the list by the F value, to make it easier to pick the node with the lowest F value
        }
    }

    private Stack<Vector3Int> GeneratePath(Node current)
    {
        if (current.Position == goalPos) // if our current node is the goal, then we found a path
        {
            // creates a stack to contain the final path
            Stack<Vector3Int> finalPath = new Stack<Vector3Int>();

            // adds the nodes to the final path
            while (current != null)
            {
                // adds the current node to the final path
                finalPath.Push(current.Position);
                // find the parent of the node, this is actually retracing the whole path back to start
                // by doing so, we will end up with a complete path
                current = current.Parent;
            }

            // returns the complete path
            return finalPath;
        }

        return null;

    }

    private void CalcValues(Node parent, Node neighbour, Vector3Int goalPos, int cost)
    {
        // sets the parent node
        neighbour.Parent = parent;

        // calculates this nodes g cost, The parents g cost + what it costs to move tot his node
        neighbour.G = parent.G + cost;

        // H is calucalted, it is the distance from this node to the goal * 10
        neighbour.H = ((Math.Abs((neighbour.Position.x - goalPos.x)) + Math.Abs((neighbour.Position.y - goalPos.y))) * 10);

        // F is calcualted 
        neighbour.F = neighbour.G + neighbour.H;
    }

    private Node GetNode(Vector3Int position)
    {
        if (allNodes.ContainsKey(position))
        {
            return allNodes[position];
        }
        else
        {
            Node node = new Node(position);
            allNodes.Add(position, node);
            return node;
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

    //private TextMeshProUGUI MyText { get; set; }

    public Node(Vector3Int position)
    {
        this.Position = position;
    }
}