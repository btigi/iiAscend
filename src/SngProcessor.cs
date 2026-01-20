using ii.Ascend.Model;

namespace ii.Ascend;

public class SngProcessor
{
    public SngFile Read(string filename)
    {
        var fileData = File.ReadAllBytes(filename);
        return Read(fileData);
    }

    public SngFile Read(byte[] fileData)
    {
        var sngFile = new SngFile();

        var content = System.Text.Encoding.Default.GetString(fileData);

		var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#') || trimmedLine.StartsWith(';'))
                continue;

            var parts = trimmedLine.Split('\t', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 3)
            {
                var songData = new SongData
                {
                    Filename = parts[0].Trim(),
                    MelodicBankFile = parts[1].Trim(),
                    DrumBankFile = parts[2].Trim()
                };

                sngFile.Songs.Add(songData);
            }
        }

        return sngFile;
    }

    public void Write(string filename, SngFile sngFile)
    {
        for (int i = 0; i < sngFile.Songs.Count; i++)
        {
            var song = sngFile.Songs[i];
            if (string.IsNullOrWhiteSpace(song.Filename))
                throw new ArgumentException($"Song entry at index {i} has an empty or null Filename.");
            if (string.IsNullOrWhiteSpace(song.MelodicBankFile))
                throw new ArgumentException($"Song entry at index {i} has an empty or null MelodicBankFile.");
            if (string.IsNullOrWhiteSpace(song.DrumBankFile))
                throw new ArgumentException($"Song entry at index {i} has an empty or null DrumBankFile.");
        }

        using var writer = new StreamWriter(filename, false, System.Text.Encoding.Default);
        
        foreach (var song in sngFile.Songs)
        {
            writer.WriteLine($"{song.Filename}\t{song.MelodicBankFile}\t{song.DrumBankFile}");
        }
    }
}