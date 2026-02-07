using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ii.Ascend;

public interface IBbmHandler
{
    bool CanHandle(byte[] fileData);
    Image<Rgba32> Read(byte[] fileData, int width, int height, List<(byte red, byte green, byte blue)>? palette, bool isRleCompressed = false, byte flags = 0);
    byte[] Write(Image<Rgba32> image, List<(byte red, byte green, byte blue)> palette, bool useRleCompression = false, byte flags = 0);
}