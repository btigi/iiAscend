namespace ii.Ascend.Model;

public class WClip
{
    public const int MaxFrames = 50;

    public int PlayTime { get; set; }
    public short NumFrames { get; set; }
    public short[] Frames { get; set; } = new short[MaxFrames];
    public short OpenSound { get; set; }
    public short CloseSound { get; set; }
    public short Flags { get; set; }
    public string Filename { get; set; } = string.Empty;
}

[Flags]
public enum WClipFlags : short
{
    None = 0,
    Explodes = 1,   // Door explodes when opening
    Blastable = 2,  // Wall is blastable
    Tmap1 = 4,      // Uses primary tmap, not tmap2
    Hidden = 8      // Hidden wall clip
}