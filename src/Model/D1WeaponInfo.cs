namespace ii.Ascend.Model;

public class D1WeaponInfo
{
    public const int NumDifficultyLevels = 5;

    public sbyte RenderType { get; set; }
    public sbyte ModelNum { get; set; }
    public sbyte ModelNumInner { get; set; }
    public sbyte Persistent { get; set; }

    public sbyte FlashVClip { get; set; }
    public short FlashSound { get; set; }
    public sbyte RobotHitVClip { get; set; }
    public short RobotHitSound { get; set; }

    public sbyte WallHitVClip { get; set; }
    public short WallHitSound { get; set; }
    public sbyte FireCount { get; set; }
    public sbyte AmmoUsage { get; set; }

    public sbyte WeaponVClip { get; set; }
    public sbyte Destroyable { get; set; }
    public sbyte Matter { get; set; }
    public sbyte Bounce { get; set; }

    public sbyte HomingFlag { get; set; }
    public sbyte Dum1 { get; set; }
    public sbyte Dum2 { get; set; }
    public sbyte Dum3 { get; set; }

    public int EnergyUsage { get; set; }
    public int FireWait { get; set; }

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
}
