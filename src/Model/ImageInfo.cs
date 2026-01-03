namespace ii.Ascend;

public class ImageInfo
{
    public string Filename { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public short Width { get; set; }
    public short Height { get; set; }
    public bool IsRleCompressed { get; set; }
    public byte Flags { get; set; }
    public byte AvgColor { get; set; }
}