namespace ii.Ascend.Model;

public class Reactor
{
    public const int MaxGuns = 8;

    public int ModelNum { get; set; }
    public int NumGuns { get; set; }
    public VmsVector[] GunPoints { get; set; } = new VmsVector[MaxGuns];
    public VmsVector[] GunDirs { get; set; } = new VmsVector[MaxGuns];
}