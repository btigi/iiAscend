using ii.Ascend.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ii.Ascend;

public class FntProcessor
{
    private const int FT_COLOR = 1;
    private const int FT_PROPORTIONAL = 2;
    private const int FT_KERNED = 4;
    private const string Signature = "PSFN";

    public FontData Read(string filename)
    {
        var fileData = File.ReadAllBytes(filename);
        return Read(fileData);
    }

    public FontData Read(byte[] fileData)
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
        var isKerned = (flags & FT_KERNED) != 0;
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
        byte[]? palette = null;
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

        var fontData = new FontData
        {
            Width = width,
            Height = height,
            Flags = flags,
            Baseline = baseline,
            MinChar = minChar,
            MaxChar = maxChar,
            ByteWidth = byteWidth,
            Palette = palette
        };

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

            var charCode = (byte)(minChar + charIndex);
            var charInfo = new FontCharData
            {
                CharacterCode = charCode,
                Width = (short)charWidth,
                Image = image
            };

            fontData.Characters.Add(charInfo);
        }

        // Read kerning data if present
        if (isKerned && kerndataOffset != 0)
        {
            var kerndataPos = headerStartPos + kerndataOffset;
            reader.BaseStream.Position = kerndataPos;
            
            var kerningEntries = new List<KerningEntry>();
            while (true)
            {
                var firstChar = reader.ReadByte();
                if (firstChar == 0xFF) // Terminator
                    break;
                
                var secondChar = reader.ReadByte();
                var newWidth = reader.ReadByte();
                
                kerningEntries.Add(new KerningEntry
                {
                    FirstChar = firstChar,
                    SecondChar = secondChar,
                    NewWidth = newWidth
                });
            }
            
            fontData.KerningData = kerningEntries;
        }

        return fontData;
    }

    public void Write(string filename, FontData fontData)
    {
        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        Write(writer, fontData);
    }

    public byte[] Write(FontData fontData)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        Write(writer, fontData);

        return stream.ToArray();
    }

    private void Write(BinaryWriter writer, FontData fontData)
    {
        var isColor = (fontData.Flags & FT_COLOR) != 0;
        var isProportional = (fontData.Flags & FT_PROPORTIONAL) != 0;
        var isKerned = (fontData.Flags & FT_KERNED) != 0;

        writer.Write(System.Text.Encoding.ASCII.GetBytes(Signature));

        // Calculate data size (we'll write it later, so reserve space)
        var dataSizePos = writer.BaseStream.Position;
        writer.Write(0); // Placeholder for dataSize

        var headerStartPos = writer.BaseStream.Position;

        writer.Write(fontData.Width);
        writer.Write(fontData.Height);
        writer.Write(fontData.Flags);
        writer.Write(fontData.Baseline);
        writer.Write(fontData.MinChar);
        writer.Write(fontData.MaxChar);
        writer.Write(fontData.ByteWidth);

        // Calculate offsets
        var headerSize = 28;
        var widthsTableSize = isProportional ? fontData.Characters.Count * 2 : 0;

        int dataOffset;
        int widthsOffset = 0;
        var kerndataOffsetPos = writer.BaseStream.Position;

        if (isProportional)
        {
            // Proportional: header, then widths table, then data, then kerning
            widthsOffset = headerSize;
            dataOffset = headerSize + widthsTableSize;
        }
        else
        {
            // Fixed-width: header, then data, then kerning
            dataOffset = headerSize;
            widthsOffset = dataOffset; // No widths table
        }

        writer.Write(dataOffset);
        writer.Write(0); // unused
        writer.Write(widthsOffset);
        writer.Write(0); // kerndataOffset placeholder

        // Widths table if proportional
        if (isProportional)
        {
            foreach (var charData in fontData.Characters)
            {
                writer.Write(charData.Width);
            }
        }

        // Character data
        var dataStartPos = writer.BaseStream.Position;
        foreach (var charData in fontData.Characters)
        {
            var image = charData.Image;
            var charWidth = image.Width;
            var charHeight = image.Height;

            if (isColor)
            {
                // Convert RGBA32 image to palette indices
                // We need to find closest palette color or use existing palette
                if (fontData.Palette == null)
                {
                    throw new InvalidOperationException("Color font requires a palette.");
                }

                for (int y = 0; y < charHeight; y++)
                {
                    for (int x = 0; x < charWidth; x++)
                    {
                        var pixel = image[x, y];
                        if (pixel.A == 0)
                        {
                            writer.Write((byte)255); // Transparent
                        }
                        else
                        {
                            // Find closest palette color
                            byte bestIndex = 0;
                            int bestDistance = int.MaxValue;

                            for (byte i = 0; i < 255; i++) // Skip 255 (transparent)
                            {
                                var b = (byte)Math.Min(255, fontData.Palette[i * 3 + 0] * 4);
                                var g = (byte)Math.Min(255, fontData.Palette[i * 3 + 1] * 4);
                                var r = (byte)Math.Min(255, fontData.Palette[i * 3 + 2] * 4);

                                var dr = pixel.R - r;
                                var dg = pixel.G - g;
                                var db = pixel.B - b;
                                var distance = dr * dr + dg * dg + db * db;

                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    bestIndex = i;
                                }
                            }

                            writer.Write(bestIndex);
                        }
                    }
                }
            }
            else
            {
                // Convert RGBA32 image to mono bits
                var bytesPerRow = BitsToBytes(charWidth);
                for (int y = 0; y < charHeight; y++)
                {
                    var rowBytes = new byte[bytesPerRow];
                    for (int x = 0; x < charWidth; x++)
                    {
                        var pixel = image[x, y];
                        var bit = (pixel.A > 128 && (pixel.R > 128 || pixel.G > 128 || pixel.B > 128)) ? 1 : 0;
                        
                        var byteIndex = x / 8;
                        var bitIndex = 7 - (x % 8); // MSB is leftmost
                        rowBytes[byteIndex] |= (byte)(bit << bitIndex);
                    }
                    writer.Write(rowBytes);
                }
            }
        }

        // Write kerning data if present
        var kerndataStartPos = writer.BaseStream.Position;
        int kerndataOffset = 0;
        if (isKerned && fontData.KerningData != null && fontData.KerningData.Count > 0)
        {
            kerndataOffset = (int)(kerndataStartPos - headerStartPos);
            foreach (var entry in fontData.KerningData)
            {
                writer.Write(entry.FirstChar);
                writer.Write(entry.SecondChar);
                writer.Write(entry.NewWidth);
            }
            writer.Write((byte)0xFF); // Terminator
        }

        // Update kerndataOffset in header
        var currentPos = writer.BaseStream.Position;
        writer.BaseStream.Position = kerndataOffsetPos;
        writer.Write(kerndataOffset);
        writer.BaseStream.Position = currentPos;

        // Calculate and write data size
        var dataEndPos = writer.BaseStream.Position;
        var dataSize = (int)(dataEndPos - 8); // 8 = signature (4) + data_size (4)
        writer.BaseStream.Position = dataSizePos;
        writer.Write(dataSize);
        writer.BaseStream.Position = dataEndPos;

        // Write palette if color font
        if (isColor)
        {
            if (fontData.Palette == null)
            {
                throw new InvalidOperationException("Color font requires a palette.");
            }
            writer.Write(fontData.Palette);
        }
    }

    private int BitsToBytes(int bits) => (bits + 7) / 8;
}