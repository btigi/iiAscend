using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ii.Ascend;

public class RawBbmHandler : IBbmHandler
{
    public bool CanHandle(byte[] fileData)
    {
        if (fileData.Length < 4)
            return true;
        
        return !(fileData[0] == 'F' && fileData[1] == 'O' && fileData[2] == 'R' && fileData[3] == 'M');
    }

    public byte[] Write(Image<Rgba32> image, List<(byte red, byte green, byte blue)> palette, bool useRleCompression = false, byte flags = 0)
    {
        if (palette == null || palette.Count == 0)
            throw new ArgumentException("Palette is required for writing raw BBM format.", nameof(palette));

        int width = image.Width;
        int height = image.Height;
        var pixelData = ImageToIndexed(image, palette, flags);

        if (!useRleCompression)
            return pixelData;

        return CompressRLEForPig(pixelData, width, height);
    }

    public Image<Rgba32> Read(byte[] fileData, int width, int height, List<(byte red, byte green, byte blue)>? palette, bool isRleCompressed = false, byte flags = 0)
    {
        if (palette == null || palette.Count == 0)
            throw new ArgumentException("Palette is required for raw BBM format.", nameof(palette));

        byte[] pixelData;
        
        if (isRleCompressed)
        {
            // RLE compressed: first 4 bytes are size, then compressed data
            if (fileData.Length < 4)
                throw new InvalidDataException("RLE compressed data too short.");
            
            var compressedSize = BitConverter.ToInt32(fileData, 0);
            if (compressedSize < 0 || fileData.Length < 4 + compressedSize - 4)
                throw new InvalidDataException("Invalid RLE compressed data size.");
            
            var compressedData = new byte[compressedSize - 4];
            Array.Copy(fileData, 4, compressedData, 0, compressedData.Length);
            pixelData = DecompressRLEFromPig(compressedData, width, height);
        }
        else
        {
            // Uncompressed: raw pixel data
            if (fileData.Length < width * height)
                throw new InvalidDataException($"Raw pixel data too short. Expected {width * height} bytes, got {fileData.Length}.");
            
            pixelData = new byte[width * height];
            Array.Copy(fileData, 0, pixelData, 0, pixelData.Length);
        }

        var paletteArray = new (byte r, byte g, byte b)[256];
        for (var i = 0; i < Math.Min(256, palette.Count); i++)
        {
            paletteArray[i] = (palette[i].red, palette[i].green, palette[i].blue);
        }

        // Determine transparency
        short transparentColor = 255;
        byte masking = 0;
        if ((flags & 1) != 0)
        {
            masking = 2;
            transparentColor = 255;
        }
        if ((flags & 2) != 0)
        {
            masking = 2;
            transparentColor = 254;
        }

        return CreateImageFromIndexedData(pixelData, (short)width, (short)height, paletteArray, transparentColor, masking);
    }

    private byte[] DecompressRLEFromPig(byte[] compressedData, int width, int height)
    {
        // Note: RLE encoding uses 0xE0 as the code marker (upper 3 bits)
        var pixelData = new byte[width * height];
        
        if (compressedData.Length < height)
            throw new InvalidDataException("RLE data too short for line sizes array.");
        
        int rleDataOffset = height;
        int dstOffset = 0;
        
        for (var y = 0; y < height; y++)
        {
            if (y >= compressedData.Length)
                break;
            
            int lineSize = compressedData[y];
            
            if (lineSize == 0)
            {
                // Empty line - fill with zeros
                for (int px = 0; px < width; px++)
                {
                    if (dstOffset < pixelData.Length)
                        pixelData[dstOffset++] = 0;
                }
                continue;
            }
            
            // Decompress this line as RLE
            int lineEnd = rleDataOffset + lineSize;
            int x = 0;
            
            while (rleDataOffset < lineEnd && rleDataOffset < compressedData.Length && x < width)
            {
                byte rleByte = compressedData[rleDataOffset++];
                
                // Check if it's the end marker (0xE0 with count 0)
                if (rleByte == 0xE0)
                    break;
                
                // Check if it's RLE encoded (upper 3 bits are 111 = 0xE0)
                if ((rleByte & 0xE0) == 0xE0)
                {
                    // RLE: lower 5 bits are count (1-31), next byte is color
                    int count = rleByte & 0x1F;
                    if (count == 0)
                        break; // End marker
                    
                    if (rleDataOffset >= compressedData.Length)
                        break;
                    
                    byte color = compressedData[rleDataOffset++];
                    
                    // Write count pixels
                    for (int i = 0; i < count && x < width; i++)
                    {
                        if (dstOffset < pixelData.Length)
                            pixelData[dstOffset++] = color;
                        x++;
                    }
                }
                else
                {
                    // Unique byte (not compressed)
                    if (dstOffset < pixelData.Length)
                        pixelData[dstOffset++] = rleByte;
                    x++;
                }
            }
            
            // Fill remaining pixels with 0 if line didn't fill entire width
            while (x < width && dstOffset < pixelData.Length)
            {
                pixelData[dstOffset++] = 0;
                x++;
            }
            
            // Advance to next line's RLE data
            rleDataOffset = lineEnd;
        }
        
        return pixelData;
    }

    private Image<Rgba32> CreateImageFromIndexedData(byte[] pixelData, short width, short height, (byte r, byte g, byte b)[] palette, short transparentColor, byte masking)
    {
        var image = new Image<Rgba32>(width, height);
        bool hasTransparency = (masking == 2); // TransparentColor mask

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                if (pixelIndex >= pixelData.Length)
                    continue;

                byte colorIndex = pixelData[pixelIndex];
                var color = palette[colorIndex];

                bool isTransparent = hasTransparency && colorIndex == transparentColor;
                byte alpha = isTransparent ? (byte)0 : (byte)255;

                image[x, y] = new Rgba32(color.r, color.g, color.b, alpha);
            }
        }

        return image;
    }

    private static byte[] ImageToIndexed(Image<Rgba32> image, List<(byte red, byte green, byte blue)> palette, byte flags)
    {
        int width = image.Width;
        int height = image.Height;
        var pixelData = new byte[width * height];

        short transparentColor = 255;
        bool hasTransparency = false;
        if ((flags & 1) != 0)
        {
            hasTransparency = true;
            transparentColor = 255;
        }
        if ((flags & 2) != 0)
        {
            hasTransparency = true;
            transparentColor = 254;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = image[x, y];

                if (hasTransparency && pixel.A < 128)
                {
                    pixelData[y * width + x] = (byte)transparentColor;
                }
                else
                {
                    pixelData[y * width + x] = FindClosestColorIndex(pixel.R, pixel.G, pixel.B, palette);
                }
            }
        }

        return pixelData;
    }

    private static byte FindClosestColorIndex(byte r, byte g, byte b, List<(byte red, byte green, byte blue)> palette)
    {
        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < palette.Count; i++)
        {
            int dr = r - palette[i].red;
            int dg = g - palette[i].green;
            int db = b - palette[i].blue;
            int distance = dr * dr + dg * dg + db * db;

            if (distance == 0) return (byte)i;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return (byte)bestIndex;
    }

    private static byte[] CompressRLEForPig(byte[] pixelData, int width, int height)
    {
        var lineSizes = new byte[height];
        var allLineData = new List<byte>();

        for (int y = 0; y < height; y++)
        {
            var lineData = new List<byte>();
            int rowStart = y * width;
            int x = 0;

            while (x < width)
            {
                byte pixel = pixelData[rowStart + x];

                // Count run of identical pixels (max 31 due to 5-bit count field)
                int runLen = 1;
                while (x + runLen < width && pixelData[rowStart + x + runLen] == pixel && runLen < 31)
                    runLen++;

                // Must RLE-encode if pixel value >= 0xE0 (would be misread as RLE marker),
                // or if a run of 3+ is worth compressing
                if (runLen >= 3 || pixel >= 0xE0)
                {
                    lineData.Add((byte)(0xE0 | runLen));
                    lineData.Add(pixel);
                    x += runLen;
                }
                else
                {
                    // Literal byte (safe because pixel < 0xE0)
                    lineData.Add(pixel);
                    x++;
                }
            }

            if (lineData.Count > 255)
                throw new InvalidDataException($"Compressed line {y} is {lineData.Count} bytes, exceeding the 255-byte PIG RLE line size limit.");

            lineSizes[y] = (byte)lineData.Count;
            allLineData.AddRange(lineData);
        }

        // Build result: 4-byte total size + line sizes array + compressed data
        var compressedData = new byte[height + allLineData.Count];
        Array.Copy(lineSizes, 0, compressedData, 0, height);
        allLineData.CopyTo(compressedData, height);

        int totalSize = 4 + compressedData.Length;
        var result = new byte[totalSize];
        BitConverter.GetBytes(totalSize).CopyTo(result, 0);
        Array.Copy(compressedData, 0, result, 4, compressedData.Length);

        return result;
    }
}