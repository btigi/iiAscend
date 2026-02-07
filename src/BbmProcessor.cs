using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ii.Ascend;

public class BbmProcessor
{
    private readonly IBbmHandler[] _handlers;

    public BbmProcessor()
    {
        // We need to check the IFF handler first since it has explicit signature detection
        _handlers =
        [
            new IffBbmHandler(),
            new RawBbmHandler()
        ];
    }

    // We don't need most of these parameters for the IFF format but we'll keep them for method signature consistency
    public Image<Rgba32> Read(byte[] fileData, int width, int height, List<(byte red, byte green, byte blue)> palette, bool isRleCompressed = false, byte flags = 0)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(fileData))
            {
                return handler.Read(fileData, width, height, palette, isRleCompressed, flags);
            }
        }

        throw new InvalidDataException("No suitable handler found for BBM data.");
    }

    public byte[] Write(Image<Rgba32> image, List<(byte red, byte green, byte blue)> palette, bool useRleCompression = false, byte flags = 0)
    {
        return _handlers.OfType<RawBbmHandler>().First()
            .Write(image, palette, useRleCompression, flags);
    }

    public byte[] WriteIff(Image<Rgba32> image, List<(byte red, byte green, byte blue)> palette, bool useRleCompression = false, byte flags = 0)
    {
        return _handlers.OfType<IffBbmHandler>().First()
            .Write(image, palette, useRleCompression, flags);
    }

    public static bool IsIffFormat(byte[] fileData)
    {
        if (fileData.Length < 4)
            return false;
        
        return fileData[0] == 'F' && fileData[1] == 'O' && fileData[2] == 'R' && fileData[3] == 'M';
    }
}