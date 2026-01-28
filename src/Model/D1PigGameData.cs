namespace ii.Ascend.Model;

public class D1PigGameData
{
    public const int MaxTextures = 800;
    public const int MaxSounds = 250;
    public const int VClipMaxNum = 70;
    public const int MaxEffects = 60;
    public const int MaxWallAnims = 30;
    public const int MaxRobotTypes = 30;
    public const int MaxRobotJoints = 600;
    public const int MaxWeaponTypes = 30;
    public const int MaxPowerupTypes = 29;
    public const int MaxPolygonModels = 85;
    public const int MaxGaugeBms = 80;
    public const int MaxObjBitmaps = 210;
    public const int NumCockpitBitmaps = 4;
    public const int MaxObjType = 100;
    public const int MaxControlCenGuns = 4;

    public int NumTextures { get; set; }
    public List<ushort> TextureBitmapIndices { get; set; } = [];
    public List<D1TmapInfo> TmapInfos { get; set; } = [];

    public byte[] Sounds { get; set; } = [];
    public byte[] AltSounds { get; set; } = [];

    public int NumVClips { get; set; }
    public List<VClip> VClips { get; set; } = [];

    public int NumEffects { get; set; }
    public List<EClip> EClips { get; set; } = [];

    public int NumWallAnims { get; set; }
    public List<D1WClip> WClips { get; set; } = [];

    public int NumRobotTypes { get; set; }
    public List<D1RobotInfo> Robots { get; set; } = [];

    public int NumRobotJoints { get; set; }
    public List<JointPos> RobotJoints { get; set; } = [];

    public int NumWeaponTypes { get; set; }
    public List<D1WeaponInfo> Weapons { get; set; } = [];

    public int NumPowerupTypes { get; set; }
    public List<PowerupInfo> Powerups { get; set; } = [];

    public int NumPolygonModels { get; set; }
    public List<PolyModel> PolygonModels { get; set; } = [];

    public List<ushort> Gauges { get; set; } = [];

    public List<int> DyingModelNums { get; set; } = [];
    public List<int> DeadModelNums { get; set; } = [];

    public List<ushort> ObjBitmaps { get; set; } = [];
    public List<ushort> ObjBitmapPtrs { get; set; } = [];

    public PlayerShip PlayerShip { get; set; } = new();

    public int NumCockpits { get; set; }
    public List<ushort> CockpitBitmaps { get; set; } = [];

    // Sounds are stored twice in D1 PIG
    public byte[] Sounds2 { get; set; } = [];
    public byte[] AltSounds2 { get; set; } = [];

    public int NumTotalObjectTypes { get; set; }
    public byte[] ObjType { get; set; } = [];
    public byte[] ObjId { get; set; } = [];
    public int[] ObjStrength { get; set; } = [];

    public int FirstMultiBitmapNum { get; set; }

    public int NumControlCenGuns { get; set; }
    public VmsVector[] ControlCenGunPoints { get; set; } = new VmsVector[MaxControlCenGuns];
    public VmsVector[] ControlCenGunDirs { get; set; } = new VmsVector[MaxControlCenGuns];

    public int ExitModelNum { get; set; }
    public int DestroyedExitModelNum { get; set; }
}
