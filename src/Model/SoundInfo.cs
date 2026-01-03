namespace ii.Ascend;

public class SoundInfo
{
    public string Filename { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public uint UncompressedLength { get; set; }
}