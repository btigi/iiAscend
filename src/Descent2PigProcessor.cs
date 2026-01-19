namespace ii.Ascend;

public class Descent2PigProcessor : IPigProcessor
{
    private const int PIGFILE_ID = 0x47495050;  // 'PPIG' (little-endian 'GIPP')
    private const int PIGFILE_VERSION = 2;
    private const int DISK_BITMAP_HEADER_SIZE = 18;

    public bool CanHandle(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        if (fileStream.Length < 8)
            return false;

        var pigId = binaryReader.ReadInt32();
        var pigVersion = binaryReader.ReadInt32();

        // Descent 2 PIG files start with 'PPIG' signature and version 2
        return pigId == PIGFILE_ID && pigVersion == PIGFILE_VERSION;
    }

    public List<(string filename, byte[] bytes)> Read(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        // Read and verify header
        var pigId = binaryReader.ReadInt32();
        var pigVersion = binaryReader.ReadInt32();

        if (pigId != PIGFILE_ID || pigVersion != PIGFILE_VERSION)
        {
            throw new InvalidDataException($"Invalid Descent 2 PIG file header. ID: 0x{pigId:X8}, Version: {pigVersion}");
        }

        var numImages = binaryReader.ReadInt32();

        var headerSize = numImages * DISK_BITMAP_HEADER_SIZE;
        var dataBaseOffset = (int)binaryReader.BaseStream.Position + headerSize;

        var imageEntries = new List<(string name, byte dflags, short width, short height, byte flags, byte avgColor, uint offset)>();
        for (int i = 0; i < numImages; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var dflags = binaryReader.ReadByte();
            var widthLow = binaryReader.ReadByte();
            var heightLow = binaryReader.ReadByte();
            var whExtra = binaryReader.ReadByte();  // bits 0-3: width high, bits 4-7: height high
            var flags = binaryReader.ReadByte();
            var avgColor = binaryReader.ReadByte();
            var offset = binaryReader.ReadUInt32();

            // Reconstruct full width and height (up to 4096)
            var width = (short)(widthLow + ((whExtra & 0x0F) << 8));
            var height = (short)(heightLow + ((whExtra & 0xF0) << 4));

            imageEntries.Add((name, dflags, width, height, flags, avgColor, offset));
        }

        var result = new List<(string filename, byte[] bytes)>();

        for (int i = 0; i < imageEntries.Count; i++)
        {
            var entry = imageEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            // Determine size: read from this offset to next image offset, or end of file
            uint dataSize;
            if (i + 1 < imageEntries.Count)
            {
                var nextAbsoluteOffset = (long)dataBaseOffset + imageEntries[i + 1].offset;
                dataSize = (uint)(nextAbsoluteOffset - absoluteOffset);
            }
            else
            {
                dataSize = (uint)(binaryReader.BaseStream.Length - absoluteOffset);
            }

            var fileData = binaryReader.ReadBytes((int)dataSize);
            if (fileData.Length < dataSize)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading image data. Expected {dataSize} bytes, got {fileData.Length}.");
            }

            result.Add((entry.name + ".bbm", fileData));
        }

        // Note: Descent 2 PIG files do not have sounds (they are in .S11/.S22 files)

        return result;
    }

    public (List<ImageInfo> images, List<SoundInfo> sounds) ReadDetailed(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        // Read and verify header
        var pigId = binaryReader.ReadInt32();
        var pigVersion = binaryReader.ReadInt32();

        if (pigId != PIGFILE_ID || pigVersion != PIGFILE_VERSION)
        {
            throw new InvalidDataException($"Invalid Descent 2 PIG file header. ID: 0x{pigId:X8}, Version: {pigVersion}");
        }

        var numImages = binaryReader.ReadInt32();

        var headerSize = numImages * DISK_BITMAP_HEADER_SIZE;
        var dataBaseOffset = (int)binaryReader.BaseStream.Position + headerSize;

        var imageEntries = new List<(string name, byte dflags, short width, short height, byte flags, byte avgColor, uint offset)>();
        for (int i = 0; i < numImages; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var dflags = binaryReader.ReadByte();
            var widthLow = binaryReader.ReadByte();
            var heightLow = binaryReader.ReadByte();
            var whExtra = binaryReader.ReadByte();  // bits 0-3: width high, bits 4-7: height high
            var flags = binaryReader.ReadByte();
            var avgColor = binaryReader.ReadByte();
            var offset = binaryReader.ReadUInt32();

            // Reconstruct full width and height (up to 4096)
            var width = (short)(widthLow + ((whExtra & 0x0F) << 8));
            var height = (short)(heightLow + ((whExtra & 0xF0) << 4));

            imageEntries.Add((name, dflags, width, height, flags, avgColor, offset));
        }

        var images = new List<ImageInfo>();

        for (int i = 0; i < imageEntries.Count; i++)
        {
            var entry = imageEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            // Determine size: read from this offset to next image offset, or end of file
            uint dataSize;
            if (i + 1 < imageEntries.Count)
            {
                var nextAbsoluteOffset = (long)dataBaseOffset + imageEntries[i + 1].offset;
                dataSize = (uint)(nextAbsoluteOffset - absoluteOffset);
            }
            else
            {
                dataSize = (uint)(binaryReader.BaseStream.Length - absoluteOffset);
            }

            var fileData = binaryReader.ReadBytes((int)dataSize);
            if (fileData.Length < dataSize)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading image data. Expected {dataSize} bytes, got {fileData.Length}.");
            }

            var isRleCompressed = (entry.flags & 8) != 0;

            images.Add(new ImageInfo
            {
                Filename = entry.name + ".bbm",
                Data = fileData,
                Width = entry.width,
                Height = entry.height,
                IsRleCompressed = isRleCompressed,
                Flags = entry.flags,
                AvgColor = entry.avgColor
            });
        }

		// Note: Descent 2 PIG files do not have sounds (they are in .S11/.S22 files)
		var sounds = new List<SoundInfo>();

        return (images, sounds);
    }
}
