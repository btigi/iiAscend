namespace ii.Ascend.Model;

public class D1WClip
{
    public const int MaxFrames = 20;

    public int PlayTime { get; set; }
    public short NumFrames { get; set; }
    public short[] Frames { get; set; } = new short[MaxFrames];
    public short OpenSound { get; set; }
    public short CloseSound { get; set; }
    public short Flags { get; set; }
    public string Filename { get; set; } = string.Empty;
}