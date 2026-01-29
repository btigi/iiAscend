namespace ii.Ascend.Model;

public class RdlFile
{
    public string LevelName { get; set; } = string.Empty;
    public List<string> PofNames { get; set; } = [];
    public List<VmsVector> Vertices { get; set; } = [];
    public List<RdlSegment> Segments { get; set; } = [];
    public List<RdlObject> Objects { get; set; } = [];
    public List<RdlWall> Walls { get; set; } = [];
    public List<RdlTrigger> Triggers { get; set; } = [];
    public List<RdlMatcen> MatCens { get; set; } = [];
    public RdlControlCenterTrigger ControlCenterTrigger { get; set; } = new();
    public List<RdlActiveDoor> ActiveDoors { get; set; } = [];
    public byte[]? PlayerData { get; set; }
    public List<RdlHostage> Hostages { get; set; } = [];
}

public class RdlHostage
{
    public int VClipNum { get; set; }

    public string Text { get; set; } = string.Empty;
}