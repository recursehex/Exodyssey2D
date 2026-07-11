using System.Collections.Generic;
using NUnit.Framework;

public class DefinitionLoaderTests
{
	private enum Tags { First, Second }
	private sealed class Entry
	{
		public string Tag;
		public int value;
	}

	[Test]
	public void IndexByTag_IndexesKnownTagsAndIgnoresUnknownTags()
	{
		List<Entry> Entries = new()
		{
			new() { Tag = "First", value = 1 },
			new() { Tag = "Unknown", value = 2 },
			new() { Tag = "Second", value = 3 },
		};

		Dictionary<Tags, Entry> Result = DefinitionLoader.IndexByTag<Tags, Entry>(Entries, Entry => Entry.Tag);

		Assert.That(Result, Has.Count.EqualTo(2));
		Assert.That(Result[Tags.First].value, Is.EqualTo(1));
		Assert.That(Result[Tags.Second].value, Is.EqualTo(3));
	}
}
