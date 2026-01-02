namespace ii.Ascend;

public class TwoFiveSixProcessor
{
    private const int PALETTE_SIZE = 256 * 3; // 768 bytes for 256 RGB colors

    public List<(byte red, byte green, byte blue)> Read(string filename)
    {
        var fileData = File.ReadAllBytes(filename);
        return Read(fileData);
    }

    public List<(byte red, byte green, byte blue)> Read(byte[] fileData)
    {
        if (fileData.Length < PALETTE_SIZE)
        {
            throw new InvalidDataException($"Invalid .256 file. Expected at least {PALETTE_SIZE} bytes, got {fileData.Length}.");
        }

        var palette = new List<(byte red, byte green, byte blue)>();

        for (int i = 0; i < 256; i++)
        {
            var offset = i * 3;
            var red = fileData[offset];
            var green = fileData[offset + 1];
            var blue = fileData[offset + 2];
            palette.Add((red, green, blue));
        }

        return palette;
    }
}
