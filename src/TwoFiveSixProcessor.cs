using ii.Ascend.Model;

namespace ii.Ascend;

public class TwoFiveSixProcessor
{
    private const int PALETTE_SIZE = 256 * 3; // 768 bytes for 256 RGB colors
    private const int FADE_TABLE_SIZE = 34 * 256; // 8704 bytes for fade table
    private const int TOTAL_FILE_SIZE = PALETTE_SIZE + FADE_TABLE_SIZE; // 9472 bytes

    public TwoFiveSixFile Read(string filename, bool bright = true)
    {
        var fileData = File.ReadAllBytes(filename);
        return Read(fileData, bright);
    }

    public TwoFiveSixFile Read(byte[] fileData, bool brighten = true)
    {
        if (fileData.Length != TOTAL_FILE_SIZE)
        {
            throw new InvalidDataException($"Invalid .256 file. Expected at {TOTAL_FILE_SIZE} bytes, got {fileData.Length}.");
        }

        var result = new TwoFiveSixFile();

        // Read palette (first 768 bytes)
        for (int i = 0; i < 256; i++)
        {
            var offset = i * 3;
            var red = fileData[offset];
            var green = fileData[offset + 1];
            var blue = fileData[offset + 2];
            
            if (brighten)
            {
                // Multiply each RGB value by 4, clamping to 255
                red = (byte)Math.Min(255, red * 4);
                green = (byte)Math.Min(255, green * 4);
                blue = (byte)Math.Min(255, blue * 4);
            }
            
            result.Palette[i] = (red, green, blue);
        }

        // Read fade table (next 8704 bytes: 34 levels × 256 bytes)
        for (int level = 0; level < 34; level++)
        {
            var fadeTableOffset = PALETTE_SIZE + (level * 256);
            for (int colorIndex = 0; colorIndex < 256; colorIndex++)
            {
                result.FadeTable[level][colorIndex] = fileData[fadeTableOffset + colorIndex];
            }
        }

        return result;
    }

    public byte[] Write(TwoFiveSixFile file, bool darken = true)
    {
        var result = new byte[TOTAL_FILE_SIZE];

        // Write palette
        for (int i = 0; i < 256; i++)
        {
            var offset = i * 3;
            var (red, green, blue) = file.Palette[i];
            
            if (darken)
            {
                // Divide each RGB value by 4
                red = (byte)(red / 4);
                green = (byte)(green / 4);
                blue = (byte)(blue / 4);
            }
            
            result[offset] = red;
            result[offset + 1] = green;
            result[offset + 2] = blue;
        }

        // Write fade table
        for (int level = 0; level < 34; level++)
        {
            var fadeTableOffset = PALETTE_SIZE + (level * 256);
            for (int colorIndex = 0; colorIndex < 256; colorIndex++)
            {
                result[fadeTableOffset + colorIndex] = file.FadeTable[level][colorIndex];
            }
        }

        return result;
    }
}
