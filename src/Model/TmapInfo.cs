namespace ii.Ascend.Model;

public class TmapInfo
{
    public byte Flags { get; set; }
    public byte[] Pad { get; set; } = new byte[3];
    public int Lighting { get; set; }
    public int Damage { get; set; }
    public short EClipNum { get; set; } // -1 if none
    public short DestroyedBitmap { get; set; } // -1 if none
	public short SlideU { get; set; }
    public short SlideV { get; set; }
}

[Flags]
public enum TmapInfoFlags : byte
{
    None = 0,
    Volatile = 1,       // Texture is volatile (lava)
    Water = 2,          // Texture is water
    ForceField = 4,     // Texture is a force field
    GoalBlue = 8,       // Blue goal texture (CTF)
    GoalRed = 16        // Red goal texture (CTF)
}