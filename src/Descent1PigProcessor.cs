namespace ii.Ascend;

public class Descent1PigProcessor : IPigProcessor
{
    private const int DISK_BITMAP_HEADER_SIZE = 17;
    private const int DISK_SOUND_HEADER_SIZE = 20;
    private const byte DBM_FLAG_LARGE = 128;  // Width + 256

    public bool CanHandle(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        if (fileStream.Length < 4)
            return false;

        var firstInt = binaryReader.ReadInt32();

        // Descent 2 PIG files start with 'PPIG' signature (0x47495050)
        // If it's NOT that signature, assume Descent 1 format
        return firstInt != 0x47495050;
    }

    public List<(string filename, byte[] bytes)> Read(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        var pigdataStart = binaryReader.ReadInt32();

        //TODO: Descent V1.4 has model data starting at offset 0, then the usual images and sounds
        //      Support pre v1.4, support model data
        binaryReader.BaseStream.Position = pigdataStart;

        var numImages = binaryReader.ReadInt32();
        var numSounds = binaryReader.ReadInt32();

        var headerSize = (numImages * DISK_BITMAP_HEADER_SIZE) + (numSounds * DISK_SOUND_HEADER_SIZE);

        // Base offset for data: pigdataStart + numImages size + numSounds size ints (counts) + all headers
        var dataBaseOffset = pigdataStart + 4 + 4 + headerSize;

        // Read image structs
        var imageEntries = new List<(string name, byte dflags, byte width, byte height, byte flags, byte avgColor, uint offset)>();
        for (int i = 0; i < numImages; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var dflags = binaryReader.ReadByte();
            var width = binaryReader.ReadByte();
            var height = binaryReader.ReadByte();
            var flags = binaryReader.ReadByte();
            var avgColor = binaryReader.ReadByte();
            var offset = binaryReader.ReadUInt32();
            imageEntries.Add((name, dflags, width, height, flags, avgColor, offset));
        }

        var soundEntries = new List<(string name, uint length, uint offset)>();
        for (int i = 0; i < numSounds; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var length = binaryReader.ReadUInt32(); // uncompressed length
            var dataLength = binaryReader.ReadUInt32(); // compressed length
            var offset = binaryReader.ReadUInt32();
            soundEntries.Add((name, length, offset));
        }

        var result = new List<(string filename, byte[] bytes)>();

        for (int i = 0; i < imageEntries.Count; i++)
        {
            var entry = imageEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            // Determine size: read from this offset to next image offset, or first sound offset, or end of file
            uint dataSize;
            if (i + 1 < imageEntries.Count)
            {
                var nextAbsoluteOffset = (long)dataBaseOffset + imageEntries[i + 1].offset;
                dataSize = (uint)(nextAbsoluteOffset - absoluteOffset);
            }
            else if (soundEntries.Count > 0)
            {
                var firstSoundOffset = (long)dataBaseOffset + soundEntries[0].offset;
                dataSize = (uint)(firstSoundOffset - absoluteOffset);
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

        for (int i = 0; i < soundEntries.Count; i++)
        {
            var entry = soundEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            var fileData = binaryReader.ReadBytes((int)entry.length);
            if (fileData.Length < entry.length)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading sound data. Expected {entry.length} bytes, got {fileData.Length}.");
            }

            result.Add((entry.name + ".raw", fileData));
        }

        return result;
    }

    public (List<ImageInfo> images, List<SoundInfo> sounds) ReadDetailed(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        var pigdataStart = binaryReader.ReadInt32();

        binaryReader.BaseStream.Position = pigdataStart;

        var numImages = binaryReader.ReadInt32();
        var numSounds = binaryReader.ReadInt32();

        var headerSize = (numImages * DISK_BITMAP_HEADER_SIZE) + (numSounds * DISK_SOUND_HEADER_SIZE);

        var dataBaseOffset = pigdataStart + 4 + 4 + headerSize;

        var imageEntries = new List<(string name, byte dflags, byte width, byte height, byte flags, byte avgColor, uint offset)>();
        for (var i = 0; i < numImages; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var dflags = binaryReader.ReadByte();
            var width = binaryReader.ReadByte();
            var height = binaryReader.ReadByte();
            var flags = binaryReader.ReadByte();
            var avgColor = binaryReader.ReadByte();
            var offset = binaryReader.ReadUInt32();
            imageEntries.Add((name, dflags, width, height, flags, avgColor, offset));
        }

        var soundEntries = new List<(string name, uint length, uint offset)>();
        for (var i = 0; i < numSounds; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var length = binaryReader.ReadUInt32();
            var dataLength = binaryReader.ReadUInt32();
            var offset = binaryReader.ReadUInt32();
            soundEntries.Add((name, length, offset));
        }

        var images = new List<ImageInfo>();
        var sounds = new List<SoundInfo>();

        for (var i = 0; i < imageEntries.Count; i++)
        {
            var entry = imageEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            uint dataSize;
            if (i + 1 < imageEntries.Count)
            {
                var nextAbsoluteOffset = (long)dataBaseOffset + imageEntries[i + 1].offset;
                dataSize = (uint)(nextAbsoluteOffset - absoluteOffset);
            }
            else if (soundEntries.Count > 0)
            {
                var firstSoundOffset = (long)dataBaseOffset + soundEntries[0].offset;
                dataSize = (uint)(firstSoundOffset - absoluteOffset);
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

            // Descent 1: bit 7 of dflags adds 256 to width
            var actualWidth = (entry.dflags & DBM_FLAG_LARGE) != 0 ? (short)(entry.width + 256) : entry.width;
            var actualHeight = (short)entry.height;
            var isRleCompressed = (entry.flags & 8) != 0;

            images.Add(new ImageInfo
            {
                Filename = entry.name + ".bbm",
                Data = fileData,
                Width = actualWidth,
                Height = actualHeight,
                IsRleCompressed = isRleCompressed,
                Flags = entry.flags,
                AvgColor = entry.avgColor
            });
        }

        for (var i = 0; i < soundEntries.Count; i++)
        {
            var entry = soundEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            var fileData = binaryReader.ReadBytes((int)entry.length);
            if (fileData.Length < entry.length)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading sound data. Expected {entry.length} bytes, got {fileData.Length}.");
            }

            sounds.Add(new SoundInfo
            {
                Filename = entry.name + ".raw",
                Data = fileData,
                UncompressedLength = entry.length
            });
        }

        return (images, sounds);
    }
}
