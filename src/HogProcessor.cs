namespace ii.Ascend;

public class HogProcessor
{
    public List<(string filename, byte[] bytes)> Read(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        var signature = binaryReader.ReadBytes(3);
        if (signature[0] != 'D' || signature[1] != 'H' || signature[2] != 'F')
        {
            throw new InvalidDataException("Invalid HOG file signature. Expected 'DHF'.");
        }

        var result = new List<(string filename, byte[] bytes)>();
        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
        {
            // 8.3 format filename
            var filenameBytes = binaryReader.ReadBytes(13);
            if (filenameBytes.Length < 13)
            {
                break;
            }

            var size = binaryReader.ReadUInt32();

            var fileData = binaryReader.ReadBytes((int)size);
            if (fileData.Length < size)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading file data. Expected {size} bytes, got {fileData.Length}.");
            }

            var filenameString = System.Text.Encoding.ASCII.GetString(filenameBytes).Split('\0')[0];

            result.Add((filenameString, fileData));
        }

        return result;
    }

    public void Write(string filename, List<(string filename, byte[] bytes)> files)
    {
        using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        using var binaryWriter = new BinaryWriter(fileStream);

        binaryWriter.Write('D');
        binaryWriter.Write('H');
        binaryWriter.Write('F');

        foreach (var (entryFilename, fileData) in files)
        {
            // Write filename as 13 bytes (8.3 format, null-padded)
            var filenameBytes = System.Text.Encoding.ASCII.GetBytes(entryFilename);
            var paddedFilename = new byte[13];
            Array.Copy(filenameBytes, paddedFilename, Math.Min(filenameBytes.Length, 13));
            binaryWriter.Write(paddedFilename);
            binaryWriter.Write((uint)fileData.Length);
            binaryWriter.Write(fileData);
        }
    }
}