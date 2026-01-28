namespace ii.Ascend.Model;

public class RobotInfo
{
    public const int MaxGuns = 8;
    public const int NumDifficultyLevels = 5;
    public const int NumAnimStates = 5;

    public int ModelNum { get; set; }
    public VmsVector[] GunPoints { get; set; } = new VmsVector[MaxGuns];
    public byte[] GunSubmodels { get; set; } = new byte[MaxGuns];
    public short Exp1VClipNum { get; set; }
    public short Exp1SoundNum { get; set; }
    public short Exp2VClipNum { get; set; }
    public short Exp2SoundNum { get; set; }
    public sbyte WeaponType { get; set; }
    public sbyte WeaponType2 { get; set; }
    public sbyte NumGuns { get; set; }
    public sbyte ContainsId { get; set; }
    public sbyte ContainsCount { get; set; }
    public sbyte ContainsProb { get; set; }
    public sbyte ContainsType { get; set; }
    public sbyte Kamikaze { get; set; }
    public short ScoreValue { get; set; }
    public sbyte Badass { get; set; }
    public sbyte EnergyDrain { get; set; }
    public int Lighting { get; set; }
    public int Strength { get; set; }
    public int Mass { get; set; }
    public int Drag { get; set; }
    public int[] FieldOfView { get; set; } = new int[NumDifficultyLevels];
    public int[] FiringWait { get; set; } = new int[NumDifficultyLevels];
    public int[] FiringWait2 { get; set; } = new int[NumDifficultyLevels];
    public int[] TurnTime { get; set; } = new int[NumDifficultyLevels];
    public int[] MaxSpeed { get; set; } = new int[NumDifficultyLevels];
    public int[] CircleDistance { get; set; } = new int[NumDifficultyLevels];
    public sbyte[] RapidfireCount { get; set; } = new sbyte[NumDifficultyLevels];
    public sbyte[] EvadeSpeed { get; set; } = new sbyte[NumDifficultyLevels];
    // Cloaking behavior (0=never, 1=always, 2=except when firing)
    public sbyte CloakType { get; set; }
    // Attack type (0=firing, 1=charge)
    public sbyte AttackType { get; set; }
    public byte SeeSound { get; set; }
    public byte AttackSound { get; set; }
    public byte ClawSound { get; set; }
    public byte TauntSound { get; set; }
    // Boss flag (0=not boss, !0=boss)
    public sbyte BossFlag { get; set; }
    public sbyte Companion { get; set; }
    public sbyte SmartBlobs { get; set; } // Emitted on death
    public sbyte EnergyBlobs { get; set; } // Emittted when hit by energy weapon
    public sbyte Thief { get; set; } // !0 = can steal on collision
    public sbyte Pursuit { get; set; }
    public sbyte Lightcast { get; set; } // 1=default, 10=very large
	public sbyte DeathRoll { get; set; } // 0=none, larger=faster
	public byte Flags { get; set; }
    public byte[] Pad { get; set; } = new byte[3];
    public byte DeathrollSound { get; set; }
    public byte Glow { get; set; }
    public byte Behavior { get; set; }
    public byte Aim { get; set; } // accuracy 255=perfect, 0=45 degree spread
	public JointList[,] AnimStates { get; set; } = new JointList[MaxGuns + 1, NumAnimStates];
    public int Always0xABCD { get; set; }
}

[Flags]
public enum RobotInfoFlags : byte
{
    None = 0,
    BigRadius = 1,  // Pad the radius to fix robots firing through walls
    Thief = 2
}

public enum RobotAnimState
{
    Rest = 0,
    Alert = 1,
    Fire = 2,
    Recoil = 3,
    Flinch = 4
}

public enum RobotCloakType : sbyte
{
    Never = 0,
    Always = 1,
    ExceptFiring = 2
}