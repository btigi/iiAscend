namespace ii.Ascend.Model;

public struct DemShortPos
{
	public short X { get; set; }
	public short Y { get; set; }
	public short Z { get; set; }
	public short Segment { get; set; }
	public short VelX { get; set; }
	public short VelY { get; set; }
	public short VelZ { get; set; }
	public short Pitch { get; set; }
	public short Bank { get; set; }
	public short Heading { get; set; }

	public VmsVector ToPosition()
	{
		return new VmsVector(X << 12, Y << 12, Z << 12);
	}

	public VmsVector ToVelocity()
	{
		return new VmsVector(VelX << 12, VelY << 12, VelZ << 12);
	}

	public VmsAngvec ToOrientation()
	{
		return new VmsAngvec(Pitch, Bank, Heading);
	}
}