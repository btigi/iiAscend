namespace ii.Ascend;

public class SProcessor
{
    private const int SNDFILE_VERSION = 1;

    public List<SoundInfo> Read(string filename)
    {
        var fileData = File.ReadAllBytes(filename);
        return Read(fileData);
    }

    public List<SoundInfo> Read(byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var reader = new BinaryReader(stream);

        var signatureBytes = reader.ReadBytes(4);
        var signature = System.Text.Encoding.ASCII.GetString(signatureBytes);
        var sndVersion = reader.ReadInt32();

        if (signature != "DSND" || sndVersion != SNDFILE_VERSION)
        {
            throw new InvalidDataException($"Invalid sound file header. Expected 'DSND' v1, got '{signature}' v{sndVersion}.");
        }

        var numSounds = reader.ReadInt32();

        var soundStart = (int)reader.BaseStream.Position;
        var headerSize = numSounds * 20; // DiskSoundHeader is 20 bytes
        var dataBaseOffset = soundStart + headerSize;

        var soundEntries = new List<(string name, int length, int dataLength, int offset)>();
        for (var i = 0; i < numSounds; i++)
        {
            var nameBytes = reader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var length = reader.ReadInt32();
            var dataLength = reader.ReadInt32();
            var offset = reader.ReadInt32();
            soundEntries.Add((name, length, dataLength, offset));
        }

        var sounds = new List<SoundInfo>();

        for (var i = 0; i < soundEntries.Count; i++)
        {
            var entry = soundEntries[i];
            var absoluteOffset = dataBaseOffset + entry.offset;
            reader.BaseStream.Position = absoluteOffset;

            var readLength = entry.dataLength > 0 ? entry.dataLength : entry.length;
            var data = reader.ReadBytes(readLength);

            if (data.Length < readLength)
            {
                throw new EndOfStreamException($"Unexpected end of stream reading '{entry.name}'. Expected {readLength} bytes, got {data.Length}.");
            }

            sounds.Add(new SoundInfo
            {
                Filename = entry.name + ".raw",
                Data = data,
                UncompressedLength = (uint)entry.length
            });
        }

        return sounds;
    }
}