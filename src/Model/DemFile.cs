namespace ii.Ascend.Model;

public class DemFile
{
    // Version from the START_DEMO event (5=D1 Shareware, 13=D1 Full, 15=D2)
    public byte Version { get; set; }
    
    // Game type from the START_DEMO event (1=D1 Shareware, 2=D1 Full, 3=D2)
    public byte GameType { get; set; }
    
    public List<IDemEvent> Events { get; set; } = [];
}