namespace ii.Ascend.Model;

public struct DemShortPos
{
	public byte[]? ByteMat { get; set; }

	public short X { get; set; }
	public short Y { get; set; }
	public short Z { get; set; }
	public short Segment { get; set; }
	public short VelX { get; set; }
	public short VelY { get; set; }
	public short VelZ { get; set; }

	public VmsVector ToPosition()
	{
		return new VmsVector(X << 12, Y << 12, Z << 12);
	}

	public VmsVector ToVelocity()
	{
		return new VmsVector(VelX << 12, VelY << 12, VelZ << 12);
	}
}
