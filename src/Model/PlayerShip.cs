namespace ii.Ascend.Model;

public class PlayerShip
{
    public const int NumPlayerGuns = 8;

    public int ModelNum { get; set; }
    public int ExplVClipNum { get; set; }
    public int Mass { get; set; }
    public int Drag { get; set; }
    public int MaxThrust { get; set; }
    public int ReverseThrust { get; set; }
    public int Brakes { get; set; }
    public int Wiggle { get; set; }
    public int MaxRotThrust { get; set; }
    public VmsVector[] GunPoints { get; set; } = new VmsVector[NumPlayerGuns];
}