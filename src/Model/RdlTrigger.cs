namespace ii.Ascend.Model;

public class RdlTrigger
{
    public const int MaxWallsPerLink = 10;

    // Trigger flags
    public const short FlagControlDoors = 1;
    public const short FlagShieldDamage = 2;
    public const short FlagEnergyDrain = 4;
    public const short FlagExit = 8;
    public const short FlagOn = 16;
    public const short FlagOneShot = 32;
    public const short FlagMatcen = 64;
    public const short FlagIllusionOff = 128;
    public const short FlagSecretExit = 256;
    public const short FlagIllusionOn = 512;

    public byte Type { get; set; }
    public short Flags { get; set; }
    public int Value { get; set; }
    public int Time { get; set; }
    public byte LinkNum { get; set; }
    public short NumLinks { get; set; }
    public short[] Seg { get; set; } = new short[MaxWallsPerLink];
    public short[] Side { get; set; } = new short[MaxWallsPerLink];
}

public class RdlControlCenterTrigger
{
    public const int MaxWallsPerLink = 10;

    public short NumLinks { get; set; }
    public short[] Seg { get; set; } = new short[MaxWallsPerLink];
    public short[] Side { get; set; } = new short[MaxWallsPerLink];
}