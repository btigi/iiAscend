namespace ii.Ascend;

public class RawProcessor
{
    private const int CHANNELS = 1; // Mono
    private const int BITS_PER_SAMPLE = 8;

    public byte[] Convert(string filename, int sampleRate = 11025)
    {
        var rawData = File.ReadAllBytes(filename);
        return Convert(rawData, sampleRate);
    }

    public byte[] Convert(byte[] rawData, int sampleRate = 11025)
    {
        return ConvertToWav(rawData, sampleRate);
    }

    private byte[] ConvertToWav(byte[] rawData, int sampleRate = 11025)
    {
        // Calculate WAV file parameters
        var dataSize = rawData.Length;
        var byteRate = sampleRate * CHANNELS * BITS_PER_SAMPLE / 8;
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
        writer.Write(sampleRate);
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