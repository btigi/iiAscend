namespace ii.Ascend.Model;

public class RdlSide
{
    public const int MaxVerticesPerSide = 4;
    // Side type: 1=quad, 2=triangulated 0-2, 3=triangulated 1-3
    public byte Type { get; set; }
    public short WallNum { get; set; } = -1;
    // Primary texture map index
    public short TmapNum { get; set; }
    // Secondary texture map index (overlay texture), lower 14 bits = texture index, upper 2 bits = rotation (0-3)
    public short TmapNum2 { get; set; }
    public RdlUvl[] Uvls { get; set; } = new RdlUvl[MaxVerticesPerSide];
    public VmsVector[] Normals { get; set; } = new VmsVector[2];

    public RdlSide()
    {
        for (int i = 0; i < MaxVerticesPerSide; i++)
        {
            Uvls[i] = new RdlUvl();
        }
    }
}

public struct RdlUvl
{
    public int U { get; set; }
    public int V { get; set; }
    public int L { get; set; }

    public RdlUvl(int u, int v, int l)
    {
        U = u;
        V = v;
        L = l;
    }
}