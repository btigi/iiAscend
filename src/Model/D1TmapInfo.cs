namespace ii.Ascend.Model;

public class D1TmapInfo
{
    /// Texture filename (13 characters, null-terminated)
    public string Filename { get; set; } = string.Empty;

    /// Texture flags (TMI_VOLATILE = 1)
    public byte Flags { get; set; }

    public int Lighting { get; set; }
    public int Damage { get; set; }
    public int EClipNum { get; set; }
}
