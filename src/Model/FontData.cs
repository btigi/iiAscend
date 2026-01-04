namespace ii.Ascend.Model;

public class FontData
{
    public short Width { get; set; }
    public short Height { get; set; }
    public short Flags { get; set; }
    public short Baseline { get; set; }
    public byte MinChar { get; set; }
    public byte MaxChar { get; set; }
    public short ByteWidth { get; set; }
    public byte[]? Palette { get; set; } // BGR format, 256*3 bytes, null if not color font
    public List<FontCharData> Characters { get; set; } = new();
    public List<KerningEntry>? KerningData { get; set; } // null if not kerned
}
