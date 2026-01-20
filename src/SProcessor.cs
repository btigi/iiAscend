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

    public void Write(string filename, List<SoundInfo> sounds)
    {
        if (sounds == null)
            throw new ArgumentNullException(nameof(sounds));

        if (sounds.Count == 0)
            throw new ArgumentException("Sound list cannot be empty.", nameof(sounds));

        for (var i = 0; i < sounds.Count; i++)
        {
            var sound = sounds[i];
            if (string.IsNullOrWhiteSpace(sound.Filename))
                throw new ArgumentException($"Sound entry at index {i} has an empty or null Filename.");
            if (sound.Data == null || sound.Data.Length == 0)
                throw new ArgumentException($"Sound entry at index {i} has null or empty Data.");
        }

        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        writer.Write(System.Text.Encoding.ASCII.GetBytes("DSND"));
        writer.Write(SNDFILE_VERSION);
        writer.Write(sounds.Count);

        var headerStart = (int)stream.Position;
        var headerSize = sounds.Count * 20; // DiskSoundHeader is 20 bytes
        var dataStart = headerStart + headerSize;

        var currentDataOffset = 0;
        for (var i = 0; i < sounds.Count; i++)
        {
            var sound = sounds[i];
            
            var name = Path.GetFileNameWithoutExtension(sound.Filename);
            
            var nameBytes = new byte[8];
            var nameBytesToWrite = System.Text.Encoding.ASCII.GetBytes(name);
            var copyLength = Math.Min(8, nameBytesToWrite.Length);
            Array.Copy(nameBytesToWrite, 0, nameBytes, 0, copyLength);
            
            writer.Write(nameBytes);
            writer.Write((int)sound.UncompressedLength);
            writer.Write(sound.Data.Length);
            writer.Write(currentDataOffset);
            
            currentDataOffset += sound.Data.Length;
        }

        foreach (var sound in sounds)
        {
            writer.Write(sound.Data);
        }
    }
}