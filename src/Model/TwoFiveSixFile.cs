namespace ii.Ascend.Model;

public class TwoFiveSixFile
{
    public (byte red, byte green, byte blue)[] Palette { get; set; }
    public byte[][] FadeTable { get; set; }

    public TwoFiveSixFile()
    {
        Palette = new (byte red, byte green, byte blue)[256];
        FadeTable = new byte[34][];
        for (int i = 0; i < 34; i++)
        {
            FadeTable[i] = new byte[256];
        }
    }
}
