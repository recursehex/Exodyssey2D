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

    private GameManager gm;

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

        // Checks within AP distance limit for reachable tiles, pseudo-recursive
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

    public Stack<Vector3Int> ComputePath(Vector3 start, Vector3 goal, GameManager g)
    {
        gm = g;

        startPos = tilemapGround.WorldToCell(start);
        goalPos = tilemapGround.WorldToCell(goal);

        allNodes.Clear();

        current = GetNode(startPos);

        // Creates an open list for nodes that could be looked at later
        openList = new HashSet<Node>();

        // Creates a closed list for examined nodes
        closedList = new HashSet<Node>();

        foreach (KeyValuePair<Vector3Int, Node> node in allNodes)
        {
            node.Value.Parent = null;
        }

        allNodes.Clear();

        // Adds the current node to the open list (has been examined)
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

        // These two for loops make sure that all nodes are created around the current node
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int p = new Vector3Int(parentPosition.x - x, parentPosition.y - y, parentPosition.z);
               
                bool fEnemy = false;
                if (gm != null)
                {
                    Vector3 pForEnemy = new Vector3(parentPosition.x - x +0.5f, parentPosition.y - y +0.5f, parentPosition.z);
                    fEnemy = gm.HasEnemyAtLoc(pForEnemy);
                }
                if (fEnemy)
                {
                    fEnemy = true;
                }
                if ((y != 0 || x != 0) && (allowDiagonal || (!allowDiagonal && (y == 0 || x == 0))))
                {
                    BoundsInt size = tilemapGround.cellBounds;

                    // If node is within bounds of the grid and if there is no wall tile and no enemy there and if player has any AP, then add it to the neighbors list
                    if (p.x >= size.min.x && p.x < size.max.x && p.y >= size.min.y && p.y < size.max.y && !tilemapWalls.HasTile(p) && !fEnemy)
                    {
                        Node neighbor = GetNode(p);
                        neighbors.Add(neighbor);
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
            Node neighbor = neighbors[i];

            if (!ConnectedDiagonally(current, neighbor))
            {
                continue;
            }

            int gScore = DetermineGScore(neighbor.Position, current.Position);

            if (gScore == 14 && NoDiagonalTiles.Contains(neighbor.Position) && NoDiagonalTiles.Contains(current.Position))
            {
                continue;
            }

            if (openList.Contains(neighbor))
            {
                if (current.G + gScore < neighbor.G)
                {
                    CalcValues(current, neighbor, goalPos, gScore);
                }
            }
            else if (!closedList.Contains(neighbor))
            {
                CalcValues(current, neighbor, goalPos, gScore);

                // An extra check for openList containing the neighbor
                if (!openList.Contains(neighbor))
                {
                    // Node is added to the openList
                    openList.Add(neighbor);
                }
            }
        }
    }

    private bool ConnectedDiagonally(Node currentNode, Node neighbor)
    {
        // Gets the direction
        Vector3Int direction = currentNode.Position - neighbor.Position;

        // Gets the positions of the nodes
        Vector3Int first = new Vector3Int(currentNode.Position.x + (direction.x * -1), currentNode.Position.y, currentNode.Position.z);
        Vector3Int second = new Vector3Int(currentNode.Position.x, currentNode.Position.y + (direction.y * -1), currentNode.Position.z);

        // The nodes are empty
        return true;
    }

    private int DetermineGScore(Vector3Int neighbor, Vector3Int current)
    {
        int gScore = 0;

        int x = current.x - neighbor.x;
        int y = current.y - neighbor.y;

        if (Math.Abs(x - y) % 2 == 1)
        {
            // The gScore for a vertical or horizontal node is 10
            gScore = 10;
        }
        else
        {
            gScore = 14;
        }

        return gScore;
    }

    private void UpdateCurrentTile(ref Node current)
    {
        // The current node is removed from the openList
        openList.Remove(current);

        // The current node is added to the closedList
        closedList.Add(current);

        // If the openList has nodes in it, then sort them by F value
        if (openList.Count > 0)
        {
            // Orders the list by the F value to make it easier to pick the node with the lowest F value
            current = openList.OrderBy(x => x.F).First();
        }
    }

    private Stack<Vector3Int> GeneratePath(Node current)
    {
        // If the current node is the goal, then a path is found
        if (current.Position == goalPos)
        {
            // Creates a stack to contain the final path
            Stack<Vector3Int> finalPath = new Stack<Vector3Int>();

            // Adds the nodes to the final path
            while (current != null)
            {
                // Adds the current node to the final path
                finalPath.Push(current.Position);
                // Find the parent of the node, this retraces the whole path back to the start,
                // by doing so, a complete path is formed
                current = current.Parent;
            }

            // Returns the complete path
            return finalPath;
        }

        return null;

    }

    private void CalcValues(Node parent, Node neighbor, Vector3Int goalPos, int cost)
    {
        // Sets the parent node
        neighbor.Parent = parent;

        // Calculates this nodes g cost, the parents g cost + what it costs to move to this node
        neighbor.G = parent.G + cost;

        // H is calucalted, it is the distance from this node to the goal * 10
        neighbor.H = ((Math.Abs((neighbor.Position.x - goalPos.x)) + Math.Abs((neighbor.Position.y - goalPos.y))) * 10);

        // F is calcualted, it is G + H
        neighbor.F = neighbor.G + neighbor.H;
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