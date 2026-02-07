using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace ii.Ascend;

public class IffBbmHandler : IBbmHandler
{
    // IFF chunk signatures (big-endian)
    private const uint FormSig = 0x464F524D; // "FORM"
    private const uint IlbmSig = 0x494C424D; // "ILBM"
    private const uint PbmSig  = 0x50424D20; // "PBM "
    private const uint BmhdSig = 0x424D4844; // "BMHD"
    private const uint CmapSig = 0x434D4150; // "CMAP"
    private const uint BodySig = 0x424F4459; // "BODY"

    // Compression types
    private const byte CmpNone = 0;
    private const byte CmpByteRun1 = 1;

    // Masking types
    private const byte MskNone = 0;
    private const byte MskHasMask = 1;
    private const byte MskHasTransparentColor = 2;

    // Type values for bitmaps
    private const int TypePbm = 0;
    private const int TypeIlbm = 1;

    public bool CanHandle(byte[] fileData)
    {
        if (fileData.Length < 4)
            return false;
        
        return fileData[0] == 'F' && fileData[1] == 'O' && fileData[2] == 'R' && fileData[3] == 'M';
    }

	// Note: IFF is big-endian hence the custom reading methods
	// Note: We don't need all these parameters but they are here to provide a simpler interface for reading the more common BBM files
	public Image<Rgba32> Read(byte[] fileData, int width, int height, List<(byte red, byte green, byte blue)>? palette, bool isRleCompressed = false, byte flags = 0)
    {
        return ParseIff(fileData);
    }

    public byte[] Write(Image<Rgba32> image, List<(byte red, byte green, byte blue)> palette, bool useRleCompression = false, byte flags = 0)
    {
        if (palette == null || palette.Count == 0)
            throw new ArgumentException("Palette is required for writing IFF BBM format.", nameof(palette));

        var (pixelData, transparentColor, masking) = ImageToIndexed(image, palette, flags);

        short width = (short)image.Width;
        short height = (short)image.Height;
        byte nPlanes = 8;
        byte compression = useRleCompression ? CmpByteRun1 : CmpNone;

        byte[] bodyData;
        if (useRleCompression)
        {
            bodyData = CompressByteRun1(pixelData, width, height);
        }
        else
        {
            int rowStride = width + (width % 2);
            if (rowStride != width)
            {
                bodyData = new byte[height * rowStride];
                for (int y = 0; y < height; y++)
                    Array.Copy(pixelData, y * width, bodyData, y * rowStride, width);
            }
            else
            {
                bodyData = pixelData;
            }
        }

        // Build CMAP data (256 colors, 768 bytes)
        var cmapData = new byte[256 * 3];
        for (int i = 0; i < 256 && i < palette.Count; i++)
        {
            cmapData[i * 3] = palette[i].red;
            cmapData[i * 3 + 1] = palette[i].green;
            cmapData[i * 3 + 2] = palette[i].blue;
        }

        // Build BMHD data (20 bytes)
        var bmhdData = new byte[20];
        int bp = 0;
        WriteInt16BE(bmhdData, ref bp, width);
        WriteInt16BE(bmhdData, ref bp, height);
        WriteInt16BE(bmhdData, ref bp, 0); // x origin
        WriteInt16BE(bmhdData, ref bp, 0); // y origin
        bmhdData[bp++] = nPlanes;
        bmhdData[bp++] = masking;
        bmhdData[bp++] = compression;
        bmhdData[bp++] = 0; // pad
        WriteInt16BE(bmhdData, ref bp, transparentColor);
        bmhdData[bp++] = 1; // xAspect
        bmhdData[bp++] = 1; // yAspect
        WriteInt16BE(bmhdData, ref bp, width);  // pageWidth
        WriteInt16BE(bmhdData, ref bp, height); // pageHeight

        // Calculate total FORM content length
        // PBM type (4) + BMHD chunk (8 + 20) + CMAP chunk (8 + 768) + BODY chunk (8 + bodyData.Length [+ 1 pad])
        int bodyChunkPad = (bodyData.Length % 2 == 1) ? 1 : 0;
        int formContentLen = 4 + (8 + 20) + (8 + cmapData.Length) + (8 + bodyData.Length + bodyChunkPad);

        var result = new byte[8 + formContentLen]; // FORM sig (4) + length (4) + content
        int pos = 0;

        WriteUInt32BE(result, ref pos, FormSig);
        WriteUInt32BE(result, ref pos, (uint)formContentLen);
        WriteUInt32BE(result, ref pos, PbmSig);

        WriteUInt32BE(result, ref pos, BmhdSig);
        WriteUInt32BE(result, ref pos, 20);
        Array.Copy(bmhdData, 0, result, pos, 20);
        pos += 20;

        WriteUInt32BE(result, ref pos, CmapSig);
        WriteUInt32BE(result, ref pos, (uint)cmapData.Length);
        Array.Copy(cmapData, 0, result, pos, cmapData.Length);
        pos += cmapData.Length;

        WriteUInt32BE(result, ref pos, BodySig);
        WriteUInt32BE(result, ref pos, (uint)bodyData.Length);
        Array.Copy(bodyData, 0, result, pos, bodyData.Length);
        pos += bodyData.Length;
        if (bodyChunkPad > 0)
            result[pos++] = 0;

        return result;
    }

    private Image<Rgba32> ParseIff(byte[] data)
    {
        int pos = 0;

        uint sig = ReadUInt32BE(data, ref pos);
        if (sig != FormSig)
            throw new InvalidDataException("Not a valid IFF file - missing FORM signature.");

        int formLen = (int)ReadUInt32BE(data, ref pos);
        
        uint formType = ReadUInt32BE(data, ref pos);
        int bitmapType;
        
        if (formType == PbmSig)
            bitmapType = TypePbm;
        else if (formType == IlbmSig)
            bitmapType = TypeIlbm;
        else
            throw new InvalidDataException($"Unknown IFF form type: {GetSigString(formType)}");


        return ParseIlbmPbm(data, ref pos, bitmapType, formLen);
    }

    private Image<Rgba32> ParseIlbmPbm(byte[] data, ref int pos, int bitmapType, int formLen)
    {
        int endPos = pos - 4 + formLen; // -4 because we already read the form type

        // Bitmap header values
        short w = 0, h = 0;
        short x = 0, y = 0;
        byte nPlanes = 0;
        byte masking = MskNone;
        byte compression = CmpNone;
        short transparentColor = 0;
        byte xAspect = 1, yAspect = 1;
        short pageWidth = 0, pageHeight = 0;

        // Palette (256 colors)
        var palette = new (byte r, byte g, byte b)[256];

        // Raw pixel data
        byte[]? rawData = null;

        // Parse chunks
        while (pos < endPos && pos < data.Length - 8)
        {
            uint chunkSig = ReadUInt32BE(data, ref pos);
            int chunkLen = (int)ReadUInt32BE(data, ref pos);

            int chunkEnd = pos + chunkLen;
            if (chunkLen % 2 == 1)
                chunkEnd++; // Pad to even length

            switch (chunkSig)
            {
                case BmhdSig:
                    // Parse bitmap header
                    w = ReadInt16BE(data, ref pos);
                    h = ReadInt16BE(data, ref pos);
                    x = ReadInt16BE(data, ref pos);
                    y = ReadInt16BE(data, ref pos);
                    nPlanes = data[pos++];
                    masking = data[pos++];
                    compression = data[pos++];
                    pos++; // pad byte
                    transparentColor = ReadInt16BE(data, ref pos);
                    xAspect = data[pos++];
                    yAspect = data[pos++];
                    pageWidth = ReadInt16BE(data, ref pos);
                    pageHeight = ReadInt16BE(data, ref pos);
                    break;

                case CmapSig:
                    // Parse color map (palette)
                    int nColors = chunkLen / 3;
                    for (int i = 0; i < nColors && i < 256; i++)
                    {
                        byte r = data[pos++];
                        byte g = data[pos++];
                        byte b = data[pos++];
                        palette[i] = (r, g, b);
                    }
                    break;

                case BodySig:
                    // Parse body (pixel data)
                    rawData = ParseBody(data, ref pos, chunkLen, w, h, nPlanes, bitmapType, compression, masking);
                    break;

                default:
                    // Skip unknown chunks
                    break;
            }

            pos = chunkEnd;
        }

        if (rawData == null)
            throw new InvalidDataException("IFF file missing BODY chunk.");

        if (w <= 0 || h <= 0)
            throw new InvalidDataException("Invalid bitmap dimensions in IFF file.");

        if (bitmapType == TypeIlbm)
        {
            rawData = ConvertIlbmToPbm(rawData, w, h, nPlanes);
        }

        // Create the image
        return CreateImage(rawData, w, h, palette, masking, transparentColor);
    }

    private byte[] ParseBody(byte[] data, ref int pos, int len, short w, short h, byte nPlanes, int bitmapType, byte compression, byte masking)
    {
        int width, depth;

        if (bitmapType == TypePbm)
        {
            width = w;
            depth = 1;
        }
        else // ILBM
        {
            width = (w + 7) / 8; // Bytes per row per plane
            depth = nPlanes;
        }

        int totalSize = width * h * depth;
        var rawData = new byte[totalSize];
        int dstPos = 0;

        int endPos = pos + len;
        if (len % 2 == 1)
            endPos++; // Padding

        if (compression == CmpNone)
        {
            // No compression - copy directly
            for (int row = 0; row < h; row++)
            {
                for (int plane = 0; plane < depth; plane++)
                {
                    int rowBytes = (bitmapType == TypePbm) ? w : width;
                    for (int x = 0; x < rowBytes && pos < data.Length && dstPos < rawData.Length; x++)
                    {
                        rawData[dstPos++] = data[pos++];
                    }
                }

                // Skip mask plane if present
                if (masking == MskHasMask)
                {
                    pos += width;
                }

                // Handle odd width padding
                if (w % 2 == 1 && bitmapType == TypePbm)
                    pos++;
            }
        }
        else if (compression == CmpByteRun1)
        {
            // ByteRun1 RLE compression
            int widthCnt = width;
            int plane = 0;
            int endCnt = (width % 2 == 1) ? -1 : 0;

            while (pos < endPos && pos < data.Length && dstPos < totalSize)
            {
                if (widthCnt == endCnt)
                {
                    widthCnt = width;
                    plane++;
                    
                    int maxPlane = (masking == MskHasMask) ? depth + 1 : depth;
                    if (plane >= maxPlane)
                        plane = 0;
                }

                sbyte n = (sbyte)data[pos++];

                if (n >= 0)
                {
                    // Copy next n+1 bytes literally
                    int count = n + 1;
                    widthCnt -= count;
                    
                    if (widthCnt == -1)
                    {
                        count--;
                    }

                    if (plane == depth)
                    {
                        // Masking row - skip
                        pos += count;
                    }
                    else
                    {
                        for (int i = 0; i < count && pos < data.Length && dstPos < rawData.Length; i++)
                        {
                            rawData[dstPos++] = data[pos++];
                        }
                    }

                    if (widthCnt == -1)
                        pos++;
                }
                else if (n >= -127)
                {
                    // Replicate next byte -n+1 times
                    if (pos >= data.Length)
                        break;
                        
                    byte c = data[pos++];
                    int count = -n + 1;
                    widthCnt -= count;
                    
                    if (widthCnt == -1)
                    {
                        count--;
                    }

                    if (plane != depth)
                    {
                        // Not masking row
                        for (int i = 0; i < count && dstPos < rawData.Length; i++)
                        {
                            rawData[dstPos++] = c;
                        }
                    }
                }
                // n == -128 is a no-op
            }
        }
        else
        {
            throw new InvalidDataException($"Unsupported IFF compression type: {compression}");
        }

        pos = endPos;
        return rawData;
    }

    private byte[] ConvertIlbmToPbm(byte[] ilbmData, short w, short h, byte nPlanes)
    {
        var pbmData = new byte[w * h];
        int bytesPerRow = (w + 7) / 8;

        for (int y = 0; y < h; y++)
        {
            int rowPtr = y * bytesPerRow * nPlanes;

            for (int x = 0; x < w; x++)
            {
                int byteOfs = x >> 3; // x / 8
                byte checkMask = (byte)(0x80 >> (x & 7)); // bit position within byte

                byte pixel = 0;
                byte setBit = 1;

                for (int p = 0; p < nPlanes; p++)
                {
                    int srcOfs = rowPtr + bytesPerRow * p + byteOfs;
                    if (srcOfs < ilbmData.Length && (ilbmData[srcOfs] & checkMask) != 0)
                    {
                        pixel |= setBit;
                    }
                    setBit <<= 1;
                }

                pbmData[y * w + x] = pixel;
            }
        }

        return pbmData;
    }

    private Image<Rgba32> CreateImage(byte[] pixelData, short w, short h, (byte r, byte g, byte b)[] palette, byte masking, short transparentColor)
    {
        var image = new Image<Rgba32>(w, h);
        bool hasTransparency = (masking == MskHasTransparentColor);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int pixelIndex = y * w + x;
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

	private static uint ReadUInt32BE(byte[] data, ref int pos)
    {
        if (pos + 4 > data.Length)
            throw new InvalidDataException("Unexpected end of IFF data.");
        
        uint value = (uint)((data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3]);
        pos += 4;
        return value;
    }

    private static short ReadInt16BE(byte[] data, ref int pos)
    {
        if (pos + 2 > data.Length)
            throw new InvalidDataException("Unexpected end of IFF data.");
        
        short value = (short)((data[pos] << 8) | data[pos + 1]);
        pos += 2;
        return value;
    }

    private static string GetSigString(uint sig)
    {
        return Encoding.ASCII.GetString(new byte[]
        {
            (byte)((sig >> 24) & 0xFF),
            (byte)((sig >> 16) & 0xFF),
            (byte)((sig >> 8) & 0xFF),
            (byte)(sig & 0xFF)
        });
    }

    private static (byte[] pixelData, short transparentColor, byte masking) ImageToIndexed(
        Image<Rgba32> image, List<(byte red, byte green, byte blue)> palette, byte flags)
    {
        int width = image.Width;
        int height = image.Height;
        var pixelData = new byte[width * height];

        short transparentColor = 255;
        byte masking = MskNone;
        if ((flags & 1) != 0)
        {
            masking = MskHasTransparentColor;
            transparentColor = 255;
        }
        if ((flags & 2) != 0)
        {
            masking = MskHasTransparentColor;
            transparentColor = 254;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = image[x, y];

                if (masking == MskHasTransparentColor && pixel.A < 128)
                {
                    pixelData[y * width + x] = (byte)transparentColor;
                }
                else
                {
                    pixelData[y * width + x] = FindClosestColorIndex(pixel.R, pixel.G, pixel.B, palette);
                }
            }
        }

        return (pixelData, transparentColor, masking);
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

    private static byte[] CompressByteRun1(byte[] pixelData, short width, short height)
    {
        int rowStride = width + (width % 2); // Pad rows to even length
        var result = new List<byte>();

        for (int y = 0; y < height; y++)
        {
            // Build padded row
            var row = new byte[rowStride];
            Array.Copy(pixelData, y * width, row, 0, width);

            CompressRowByteRun1(row, result);
        }

        return result.ToArray();
    }

    private static void CompressRowByteRun1(byte[] row, List<byte> output)
    {
        int len = row.Length;
        int i = 0;

        while (i < len)
        {
            int runStart = i;
            byte runByte = row[i];
            int runLen = 1;

            while (i + runLen < len && row[i + runLen] == runByte && runLen < 128)
                runLen++;

            if (runLen >= 3)
            {
                // Encode as run: -(count-1), byte
                output.Add((byte)(sbyte)(-(runLen - 1)));
                output.Add(runByte);
                i += runLen;
            }
            else
            {
                int literalStart = i;
                int literalCount = 0;

                while (i < len && literalCount < 128)
                {
                    if (i + 2 < len && row[i] == row[i + 1] && row[i] == row[i + 2])
                        break;

                    literalCount++;
                    i++;
                }

                if (literalCount > 0)
                {
                    output.Add((byte)(literalCount - 1));
                    for (int j = 0; j < literalCount; j++)
                        output.Add(row[literalStart + j]);
                }
            }
        }
    }

    private static void WriteUInt32BE(byte[] data, ref int pos, uint value)
    {
        data[pos++] = (byte)((value >> 24) & 0xFF);
        data[pos++] = (byte)((value >> 16) & 0xFF);
        data[pos++] = (byte)((value >> 8) & 0xFF);
        data[pos++] = (byte)(value & 0xFF);
    }

    private static void WriteInt16BE(byte[] data, ref int pos, short value)
    {
        data[pos++] = (byte)((value >> 8) & 0xFF);
        data[pos++] = (byte)(value & 0xFF);
    }
}