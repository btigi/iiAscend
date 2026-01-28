namespace ii.Ascend.Model;

public class WeaponInfo
{
    public const int NumDifficultyLevels = 5;

    // 0=laser, 1=blob, 2=object, 3=vclip
    public sbyte RenderType { get; set; }
    // 0=dies on hit, 1=continues (e.g., fusion cannon)
    public sbyte Persistent { get; set; }
    public short ModelNum { get; set; }
    public short ModelNumInner { get; set; }
    public sbyte FlashVClip { get; set; }
    public sbyte RobotHitVClip { get; set; }
    public short FlashSound { get; set; }
    public sbyte WallHitVClip { get; set; }
    public sbyte FireCount { get; set; }
    public short RobotHitSound { get; set; }
    public sbyte AmmoUsage { get; set; }
    public sbyte WeaponVClip { get; set; }
    public short WallHitSound { get; set; }
    public sbyte Destroyable { get; set; }
    public sbyte Matter { get; set; }
    // Bounce behavior (0=none, 1=always, 2=twice)
    public sbyte Bounce { get; set; }
    public sbyte HomingFlag { get; set; }
    public byte SpeedVar { get; set; }
    public byte Flags { get; set; }
    public sbyte Flash { get; set; }
    public sbyte AfterburnerSize { get; set; }
    public sbyte Children { get; set; }
    public int EnergyUsage { get; set; }
    public int FireWait { get; set; }
    public int MultiDamageScale { get; set; }
    public ushort BitmapIndex { get; set; }
    public int BlobSize { get; set; }
    public int FlashSize { get; set; }
    public int ImpactSize { get; set; }
    public int[] Strength { get; set; } = new int[NumDifficultyLevels];
    public int[] Speed { get; set; } = new int[NumDifficultyLevels];
    public int Mass { get; set; }
    public int Drag { get; set; }
    public int Thrust { get; set; }
    public int PoLenToWidthRatio { get; set; }
    public int Light { get; set; }
    public int Lifetime { get; set; }
    public int DamageRadius { get; set; }
    public ushort Picture { get; set; }
    public ushort HiresPicture { get; set; }
}

public enum WeaponRenderType : sbyte
{
    Laser = 0,
    Blob = 1,
    Polymodel = 2,
    VClip = 3,
    None = -1
}

[Flags]
public enum WeaponFlags : byte
{
    None = 0,
    Placable = 1    // Can be placed by level designer
}