using NUnit.Framework;
using UnityEngine;

public class NodeTests
{
    [Test]
    public void Constructor_SetsPosition()
    {
        Vector3Int pos = new(3, -2, 0);
        Node node = new Node(pos);
        Assert.AreEqual(pos, node.Position);
    }

    [Test]
    public void DefaultValues_AreZero()
    {
        Node node = new Node(Vector3Int.zero);
        Assert.AreEqual(0, node.G);
        Assert.AreEqual(0, node.H);
        Assert.AreEqual(0, node.F);
        Assert.AreEqual(0, node.Turns);
        Assert.AreEqual(0, node.AlignmentCost);
        Assert.IsNull(node.Parent);
        Assert.IsFalse(node.HasEntity);
    }

    [Test]
    public void Properties_CanBeSet()
    {
        Node node = new Node(new Vector3Int(1, 1, 0));
        node.G = 10;
        node.H = 20;
        node.F = 30;
        node.Turns = 2;
        node.AlignmentCost = 5;

        Assert.AreEqual(10, node.G);
        Assert.AreEqual(20, node.H);
        Assert.AreEqual(30, node.F);
        Assert.AreEqual(2, node.Turns);
        Assert.AreEqual(5, node.AlignmentCost);
    }

    [Test]
    public void Parent_CanBeLinked()
    {
        Node parent = new Node(new Vector3Int(0, 0, 0));
        Node child = new Node(new Vector3Int(1, 0, 0));
        child.Parent = parent;

        Assert.AreEqual(parent, child.Parent);
        Assert.IsNull(parent.Parent);
    }

    [Test]
    public void HasEntity_CanBeToggled()
    {
        Node node = new Node(Vector3Int.zero);
        Assert.IsFalse(node.HasEntity);
        node.HasEntity = true;
        Assert.IsTrue(node.HasEntity);
    }

    [Test]
    public void ParentChain_CanBeTraversed()
    {
        Node root = new Node(new Vector3Int(0, 0, 0));
        Node mid = new Node(new Vector3Int(1, 0, 0)) { Parent = root };
        Node leaf = new Node(new Vector3Int(2, 0, 0)) { Parent = mid };

        // Traverse from leaf to root
        int depth = 0;
        Node current = leaf;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }
        Assert.AreEqual(3, depth);
    }
}
