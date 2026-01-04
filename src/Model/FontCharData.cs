using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ii.Ascend.Model;

public class FontCharData
{
    public byte CharacterCode { get; set; }
    public short Width { get; set; }
    public Image<Rgba32> Image { get; set; } = null!;
}
