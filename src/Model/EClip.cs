namespace ii.Ascend.Model;

public class EClip
{
    public VClip Vc { get; set; } = new();
    public int TimeLeft { get; set; }
    public int FrameCount { get; set; }
    public short ChangingWallTexture { get; set; }
    public short ChangingObjectTexture { get; set; }
    public int Flags { get; set; }
    public int CritClip { get; set; }
    public int DestBmNum { get; set; }
    public int DestVClip { get; set; }
    public int DestEClip { get; set; }
    public int DestSize { get; set; }
    public int SoundNum { get; set; }
    public int SegNum { get; set; }
    public int SideNum { get; set; }
}

[Flags]
public enum EClipFlags
{
    None = 0,
    Critical = 1,   // Only plays when mine is critical
    OneShot = 2,    // Special one-time effect
    Stopped = 4     // Effect has been stopped
}