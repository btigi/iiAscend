namespace ii.Ascend.Model;

public class HamFile
{
    public const uint Signature = 0x214D4148; // "HAM!" in little-endian
    public const int Version = 3;

    public List<ushort> TextureBitmapIndices { get; set; } = [];
    public List<TmapInfo> TmapInfos { get; set; } = [];

    public byte[] Sounds { get; set; } = [];
    public byte[] AltSounds { get; set; } = [];

    public List<VClip> VClips { get; set; } = [];
    public List<EClip> EClips { get; set; } = [];
    public List<WClip> WClips { get; set; } = [];

    public List<RobotInfo> Robots { get; set; } = [];
    public List<JointPos> RobotJoints { get; set; } = [];

    public List<WeaponInfo> Weapons { get; set; } = [];

    public List<PowerupInfo> Powerups { get; set; } = [];

    public List<PolyModel> PolygonModels { get; set; } = [];
    public List<int> DyingModelNums { get; set; } = [];
    public List<int> DeadModelNums { get; set; } = [];

    public List<ushort> GaugesLores { get; set; } = [];
    public List<ushort> GaugesHires { get; set; } = [];

    public List<ushort> ObjBitmaps { get; set; } = [];
    public List<ushort> ObjBitmapPtrs { get; set; } = [];

    public PlayerShip PlayerShip { get; set; } = new();

    public List<ushort> CockpitBitmaps { get; set; } = [];

    public int FirstMultiBitmapNum { get; set; }

    public List<Reactor> Reactors { get; set; } = [];

    public int MarkerModelNum { get; set; }

    public ushort[] GameBitmapXlat { get; set; } = [];
}