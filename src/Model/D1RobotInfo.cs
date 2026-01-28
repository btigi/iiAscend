namespace ii.Ascend.Model;

public class D1RobotInfo
{
    public const int MaxGuns = 8;
    public const int NumDifficultyLevels = 5;
    public const int NumAnimStates = 5;

    public int ModelNum { get; set; }
    public int NumGuns { get; set; }
    public VmsVector[] GunPoints { get; set; } = new VmsVector[MaxGuns];
    public byte[] GunSubmodels { get; set; } = new byte[MaxGuns];

    public short Exp1VClipNum { get; set; }
    public short Exp1SoundNum { get; set; }
    public short Exp2VClipNum { get; set; }
    public short Exp2SoundNum { get; set; }

    public short WeaponType { get; set; }

    public sbyte ContainsId { get; set; }
    public sbyte ContainsCount { get; set; }
    public sbyte ContainsProb { get; set; }
    public sbyte ContainsType { get; set; }

    public int ScoreValue { get; set; }

    public int Lighting { get; set; }
    public int Strength { get; set; }

    public int Mass { get; set; }
    public int Drag { get; set; }

    public int[] FieldOfView { get; set; } = new int[NumDifficultyLevels];
    public int[] FiringWait { get; set; } = new int[NumDifficultyLevels];
    public int[] TurnTime { get; set; } = new int[NumDifficultyLevels];

    /// D1 only: damage done by a hit from this robot
    public int[] FirePower { get; set; } = new int[NumDifficultyLevels];

    /// D1 only: shield strength of this robot
    public int[] Shield { get; set; } = new int[NumDifficultyLevels];

    public int[] MaxSpeed { get; set; } = new int[NumDifficultyLevels];
    public int[] CircleDistance { get; set; } = new int[NumDifficultyLevels];

    public sbyte[] RapidfireCount { get; set; } = new sbyte[NumDifficultyLevels];
    public sbyte[] EvadeSpeed { get; set; } = new sbyte[NumDifficultyLevels];

    public sbyte CloakType { get; set; }
    public sbyte AttackType { get; set; }
    public sbyte BossFlag { get; set; }

    public byte SeeSound { get; set; }
    public byte AttackSound { get; set; }
    public byte ClawSound { get; set; }

    /// Animation states for each gun and state combination.
    /// Dimensions: [MaxGuns+1][NumAnimStates]
    public JointList[,] AnimStates { get; set; } = new JointList[MaxGuns + 1, NumAnimStates];

    public int Always0xABCD { get; set; }
}