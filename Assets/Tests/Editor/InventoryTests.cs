using NUnit.Framework;

public class InventoryTests
{
    private Inventory inventory;

    [SetUp]
    public void SetUp()
    {
        inventory = new Inventory(2);
    }

    [Test]
    public void NewInventory_IsEmpty()
    {
        Assert.IsTrue(inventory.IsEmpty);
        Assert.AreEqual(0, inventory.Count);
    }

    [Test]
    public void Size_MatchesConstructorParameter()
    {
        Assert.AreEqual(2, inventory.Size);

        Inventory large = new Inventory(5);
        Assert.AreEqual(5, large.Size);
    }

    [Test]
    public void TryAddItem_ToEmptyInventory_Succeeds()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Branch);
        bool added = inventory.TryAddItem(item);

        Assert.IsTrue(added);
        Assert.AreEqual(1, inventory.Count);
        Assert.IsFalse(inventory.IsEmpty);
    }

    [Test]
    public void TryAddItem_ReturnsAddedIndex()
    {
        ItemInfo item1 = new ItemInfo((int)ItemInfo.Tags.Branch);
        ItemInfo item2 = new ItemInfo((int)ItemInfo.Tags.Knife);

        inventory.TryAddItem(item1, out int index1);
        inventory.TryAddItem(item2, out int index2);

        Assert.AreEqual(0, index1);
        Assert.AreEqual(1, index2);
    }

    [Test]
    public void TryAddItem_ToFullInventory_Fails()
    {
        inventory.TryAddItem(new ItemInfo((int)ItemInfo.Tags.Branch));
        inventory.TryAddItem(new ItemInfo((int)ItemInfo.Tags.Knife));

        bool added = inventory.TryAddItem(new ItemInfo((int)ItemInfo.Tags.Rock));
        Assert.IsFalse(added);
        Assert.AreEqual(2, inventory.Count);
    }

    [Test]
    public void RemoveItem_DecreasesCount()
    {
        inventory.TryAddItem(new ItemInfo((int)ItemInfo.Tags.Branch));
        Assert.AreEqual(1, inventory.Count);

        inventory.RemoveItem(0);
        Assert.AreEqual(0, inventory.Count);
        Assert.IsTrue(inventory.IsEmpty);
    }

    [Test]
    public void RemoveItem_InvalidIndex_DoesNothing()
    {
        inventory.TryAddItem(new ItemInfo((int)ItemInfo.Tags.Branch));
        inventory.RemoveItem(-1);
        inventory.RemoveItem(99);
        Assert.AreEqual(1, inventory.Count);
    }

    [Test]
    public void HasItemAt_ReturnsTrueForOccupiedSlot()
    {
        inventory.TryAddItem(new ItemInfo((int)ItemInfo.Tags.Branch));
        Assert.IsTrue(inventory.HasItemAt(0));
        Assert.IsFalse(inventory.HasItemAt(1));
    }

    [Test]
    public void HasItemAt_InvalidIndex_ReturnsFalse()
    {
        Assert.IsFalse(inventory.HasItemAt(-1));
        Assert.IsFalse(inventory.HasItemAt(99));
    }

    [Test]
    public void Indexer_ReturnsCorrectItem()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Branch);
        inventory.TryAddItem(item);
        Assert.AreEqual(item, inventory[0]);
        Assert.IsNull(inventory[1]);
    }

    [Test]
    public void Indexer_SetOverwritesSlot()
    {
        ItemInfo item1 = new ItemInfo((int)ItemInfo.Tags.Branch);
        ItemInfo item2 = new ItemInfo((int)ItemInfo.Tags.Knife);
        inventory.TryAddItem(item1);

        inventory[0] = item2;
        Assert.AreEqual(item2, inventory[0]);
    }

    [Test]
    public void TryAddItem_FillsFirstEmptySlot()
    {
        ItemInfo item1 = new ItemInfo((int)ItemInfo.Tags.Branch);
        ItemInfo item2 = new ItemInfo((int)ItemInfo.Tags.Knife);
        ItemInfo item3 = new ItemInfo((int)ItemInfo.Tags.Rock);

        inventory.TryAddItem(item1);
        inventory.TryAddItem(item2);
        // Remove first slot
        inventory.RemoveItem(0);
        // Next add should fill slot 0
        inventory.TryAddItem(item3, out int addedIndex);

        Assert.AreEqual(0, addedIndex);
        Assert.AreEqual(item3, inventory[0]);
    }

    [Test]
    public void ZeroSizeInventory_IsAlwaysFull()
    {
        Inventory empty = new Inventory(0);
        Assert.IsTrue(empty.IsEmpty);
        Assert.AreEqual(0, empty.Size);
        Assert.IsFalse(empty.TryAddItem(new ItemInfo((int)ItemInfo.Tags.Branch)));
    }
}
