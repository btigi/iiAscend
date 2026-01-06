namespace ii.Ascend;

public class MsnFile
{
    public List<MsnProperty> Properties { get; set; } = new();
    public List<string> LevelFilenames { get; set; } = new();
    public List<string> SecretEntries { get; set; } = new();
}
