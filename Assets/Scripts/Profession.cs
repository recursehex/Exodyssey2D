public struct Profession
{
	public enum Tags
	{
		Medic = 0,
		Mechanic,
		Hunter,
		Hiker,
		Navigator,
		Ranger,
		Unknown
	}
	public Tags Tag { get; set; }
	public int Level { get; set; }
	public Profession(Tags Tag, int Level)
	{
		this.Tag = Tag;
		this.Level = Level;
	}
}