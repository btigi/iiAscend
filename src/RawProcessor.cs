namespace ii.Ascend;

public class RawProcessor
{
    private const int SAMPLE_RATE = 11025;
    private const int CHANNELS = 1; // Mono
    private const int BITS_PER_SAMPLE = 8;

    public byte[] Convert(string filename)
    {
        var rawData = File.ReadAllBytes(filename);
        return Convert(rawData);
    }

    public byte[] Convert(byte[] rawData)
    {
        return ConvertToWav(rawData);
    }

    private byte[] ConvertToWav(byte[] rawData)
    {
        // Calculate WAV file parameters
        var dataSize = rawData.Length;
        var byteRate = SAMPLE_RATE * CHANNELS * BITS_PER_SAMPLE / 8;
        var blockAlign = CHANNELS * BITS_PER_SAMPLE / 8;
        var fileSize = 36 + dataSize; // 36 = header size, dataSize = audio data
        
        // Create WAV file in memory
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);
        
        // RIFF header
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(fileSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        
        // fmt chunk
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // Chunk size (16 for PCM)
        writer.Write((ushort)1); // Audio format (1 = PCM)
        writer.Write((ushort)CHANNELS);
        writer.Write(SAMPLE_RATE);
        writer.Write(byteRate);
        writer.Write((ushort)blockAlign);
        writer.Write((ushort)BITS_PER_SAMPLE);
        
        // data chunk
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        writer.Write(rawData);
        
        return memoryStream.ToArray();
    }
}