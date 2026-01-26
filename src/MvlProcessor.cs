namespace ii.Ascend;

public class MvlProcessor
{
    public List<(string filename, byte[] bytes)> Read(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        var signature = binaryReader.ReadBytes(4);
        if (signature.Length < 4 || signature[0] != 'D' || signature[1] != 'M' ||  signature[2] != 'V' || signature[3] != 'L')
        {
            throw new InvalidDataException("Invalid MVL file signature. Expected 'DMVL'.");
        }

        var numFiles = binaryReader.ReadInt32();

        var directoryEntries = new List<(string filename, int fileSize)>();
        for (int i = 0; i < numFiles; i++)
        {
            // Read filename (13 bytes, null-padded)
            var filenameBytes = binaryReader.ReadBytes(13);
            if (filenameBytes.Length < 13)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading filename for file {i + 1}.");
            }

            var filenameString = System.Text.Encoding.ASCII.GetString(filenameBytes).Split('\0')[0];
            var fileSize = binaryReader.ReadInt32();

            directoryEntries.Add((filenameString, fileSize));
        }

        var result = new List<(string filename, byte[] bytes)>();
        foreach (var (entryFilename, fileSize) in directoryEntries)
        {
            var fileData = binaryReader.ReadBytes(fileSize);
            if (fileData.Length < fileSize)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading file data for '{entryFilename}'. Expected {fileSize} bytes, got {fileData.Length}.");
            }

            result.Add((entryFilename, fileData));
        }

        return result;
    }

    public void Write(string filename, List<(string filename, byte[] bytes)> files)
    {
        using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        using var binaryWriter = new BinaryWriter(fileStream);

        binaryWriter.Write('D');
        binaryWriter.Write('M');
        binaryWriter.Write('V');
        binaryWriter.Write('L');

        binaryWriter.Write(files.Count);

        foreach (var (entryFilename, fileData) in files)
        {
            // Write filename as 13 bytes (null-padded)
            var filenameBytes = System.Text.Encoding.ASCII.GetBytes(entryFilename);
            var paddedFilename = new byte[13];
            Array.Copy(filenameBytes, paddedFilename, Math.Min(filenameBytes.Length, 13));
            binaryWriter.Write(paddedFilename);
            binaryWriter.Write(fileData.Length);
        }

        foreach (var (entryFilename, fileData) in files)
        {
            binaryWriter.Write(fileData);
        }
    }
}