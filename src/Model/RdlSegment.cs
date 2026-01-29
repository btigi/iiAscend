namespace ii.Ascend.Model;

// Represents a segment (cube-like room) - each segment has 6 sides and 8 vertices
public class RdlSegment
{
    public const int MaxSidesPerSegment = 6;
    public const int MaxVerticesPerSegment = 8;

    // Indices of the 6 child segments connected to each side.
    // -1 means no connection (solid wall), -2 means external (exit).
    // Order: Left, Top, Right, Bottom, Back, Front
    public short[] Children { get; set; } = new short[MaxSidesPerSegment];
    public short[] Verts { get; set; } = new short[MaxVerticesPerSegment];
    public RdlSide[] Sides { get; set; } = new RdlSide[MaxSidesPerSegment];
    // Special segment type (0=normal, 1=fuel center, 2=repair, 3=control center, 4=robot maker)
    public byte Special { get; set; }
    public sbyte MatcenNum { get; set; } = -1;
    public short Value { get; set; }
    public int StaticLight { get; set; }
    public short Objects { get; set; } = -1;

    public RdlSegment()
    {
        for (int i = 0; i < MaxSidesPerSegment; i++)
        {
            Children[i] = -1;
            Sides[i] = new RdlSide();
        }
    }
}