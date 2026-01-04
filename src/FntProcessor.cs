using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ii.Ascend;

public class FntProcessor
{
    private const int FT_COLOR = 1;
    private const int FT_PROPORTIONAL = 2;
    private const int FT_KERNED = 4;
    private const string Signature = "PSFN";

    public List<Image<Rgba32>> Read(string filename)
    {
        var fileData = File.ReadAllBytes(filename);
        return Read(fileData);
    }

    public List<Image<Rgba32>> Read(byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var reader = new BinaryReader(stream);

        var fileIdBytes = reader.ReadBytes(4);
        var fileId = System.Text.Encoding.ASCII.GetString(fileIdBytes);
        if (fileId != Signature)
        {
            throw new InvalidDataException($"Invalid FNT file. Expected '{Signature}' signature, got '{fileId}'.");
        }

        // Data size
        var dataSize = reader.ReadInt32();

        var headerStartPos = reader.BaseStream.Position;
        var width = reader.ReadInt16();
        var height = reader.ReadInt16();
        var flags = reader.ReadInt16();
        var baseline = reader.ReadInt16();
        var minChar = reader.ReadByte();
        var maxChar = reader.ReadByte();
        var byteWidth = reader.ReadInt16();
        var dataOffset = reader.ReadInt32(); // Relative to header start
        var unused = reader.ReadInt32();
        var widthsOffset = reader.ReadInt32(); // Relative to header start, if proportional
        var kerndataOffset = reader.ReadInt32(); // Relative to header start, if kerned

        var isColor = (flags & FT_COLOR) != 0;
        var isProportional = (flags & FT_PROPORTIONAL) != 0;
        var numChars = maxChar - minChar + 1;

        // Read character widths if proportional
        short[] charWidths = null!;
        if (isProportional)
        {
            var widthsPos = headerStartPos + widthsOffset;
            reader.BaseStream.Position = widthsPos;
            charWidths = new short[numChars];
            for (int i = 0; i < numChars; i++)
            {
                charWidths[i] = reader.ReadInt16();
            }
        }

        // Read palette if color font (palette is after dataSize, not included in dataSize)
        byte[] palette = null!;
        if (isColor)
        {
            var palettePos = 8 + dataSize; // 8 = signature (4) + data_size (4)
            reader.BaseStream.Position = palettePos;
            palette = reader.ReadBytes(256 * 3); // BGR format *shrug*
        }

        // Calculate data start position
        // For proportional fonts: use dataOffset (relative to header start)
        // For fixed-width fonts: data starts right after the header
        long dataStartPos;
        if (isProportional)
        {
            dataStartPos = headerStartPos + dataOffset;
        }
        else
        {
            dataStartPos = headerStartPos + 28;
        }

        var images = new List<Image<Rgba32>>();

        // Calculate fixed character size for fixed-width fonts
        int fixedCharDataSize = 0;
        if (!isProportional)
        {
            if (isColor)
            {
                fixedCharDataSize = width * height;
            }
            else
            {
                fixedCharDataSize = BitsToBytes(width) * height;
            }
        }

        // Read each character
        reader.BaseStream.Position = dataStartPos;
        long currentDataPos = dataStartPos;

        for (int charIndex = 0; charIndex < numChars; charIndex++)
        {
            int charWidth = isProportional ? charWidths[charIndex] : width;
            int charHeight = height;

            // Calculate character data size
            int charDataSize;
            if (isProportional)
            {
                // Proportional: size depends on character width
                if (isColor)
                {
                    charDataSize = charWidth * charHeight;
                }
                else
                {
                    charDataSize = BitsToBytes(charWidth) * charHeight;
                }
            }
            else
            {
                // Fixed-width: use pre-calculated size
                charDataSize = fixedCharDataSize;
            }

            // For fixed-width fonts, calculate position based on character index
            if (!isProportional)
            {
                // Fixed-width: each character is at a fixed offset
                currentDataPos = dataStartPos + (charIndex * fixedCharDataSize);
            }

            // Read character data
            reader.BaseStream.Position = currentDataPos;
            var charData = reader.ReadBytes(charDataSize);
            
            // Advance position for proportional fonts
            if (isProportional)
            {
                currentDataPos += charDataSize;
            }

            // Create image for this character
            var image = new Image<Rgba32>(charWidth, charHeight);

            if (isColor)
            {
                // Color font: each byte is a palette index
                for (int y = 0; y < charHeight; y++)
                {
                    for (int x = 0; x < charWidth; x++)
                    {
                        var paletteIndex = charData[y * charWidth + x];
                        if (paletteIndex == 255)
                        {
                            // Transparent
                            image[x, y] = new Rgba32(0, 0, 0, 0);
                        }
                        else
                        {
                            // Get color from palette (BGR)
                            var b = (byte)Math.Min(255, palette[paletteIndex * 3 + 0] * 4);
                            var g = (byte)Math.Min(255, palette[paletteIndex * 3 + 1] * 4);
                            var r = (byte)Math.Min(255, palette[paletteIndex * 3 + 2] * 4);
                            image[x, y] = new Rgba32(r, g, b, 255);
                        }
                    }
                }
            }
            else
            {
                // Mono font: bits represent pixels
                var bytesPerRow = BitsToBytes(charWidth);
                for (int y = 0; y < charHeight; y++)
                {
                    for (int x = 0; x < charWidth; x++)
                    {
                        var byteIndex = y * bytesPerRow + (x / 8);
                        var bitIndex = 7 - (x % 8); // MSB is leftmost
                        var bit = (charData[byteIndex] >> bitIndex) & 1;
                        
                        if (bit == 1)
                        {
                            // Opaque pixel (white for mono fonts)
                            image[x, y] = new Rgba32(255, 255, 255, 255);
                        }
                        else
                        {
                            // Transparent
                            image[x, y] = new Rgba32(0, 0, 0, 0);
                        }
                    }
                }
            }

            images.Add(image);
        }

        return images;
    }

    private int BitsToBytes(int bits) => (bits + 7) / 8;
}