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
}