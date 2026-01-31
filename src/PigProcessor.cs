namespace ii.Ascend;

public class PigProcessor : IPigProcessor
{
    private readonly IPigProcessor[] _processors =
    [
        new Descent2PigProcessor(),
        new Descent1PigProcessor()
    ];

    public bool CanHandle(string filename)
    {
        return _processors.Any(p => p.CanHandle(filename));
    }

    public List<(string filename, byte[] bytes)> Read(string filename)
    {
        var processor = GetProcessor(filename);
        return processor.Read(filename);
    }

    public (List<ImageInfo> images, List<SoundInfo> sounds, List<(string filename, byte[] data)> pofFiles, Model.D1PigGameData? gameData) ReadDetailed(string filename)
    {
        var processor = GetProcessor(filename);
        return processor.ReadDetailed(filename);
    }

    private IPigProcessor GetProcessor(string filename)
    {
        foreach (var processor in _processors)
        {
            if (processor.CanHandle(filename))
                return processor;
        }

		throw new InvalidDataException("No suitable handler found for PIG data.");
	}
}