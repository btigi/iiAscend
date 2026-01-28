namespace ii.Ascend.Model;

public class VClip
{
    public const int MaxFrames = 30;

    public int PlayTime { get; set; }
    public int NumFrames { get; set; }
    public int FrameTime { get; set; }
    public int Flags { get; set; }
    public short SoundNum { get; set; } // -1 if none
	public ushort[] Frames { get; set; } = new ushort[MaxFrames];
    public int LightValue { get; set; }
}

[Flags]
public enum VClipFlags
{
    None = 0,
    Rod = 1     // Draw as a rod, not a blob
}