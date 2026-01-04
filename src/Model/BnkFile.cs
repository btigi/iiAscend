namespace ii.Ascend.Model;

public class BnkFile
{
    public byte VersionMinor { get; set; }
    public byte VersionMajor { get; set; }
    public ushort NumRecordsTotal { get; set; }
    public ushort NumRecordsUsed { get; set; }
    public List<BnkNameRecord> NameRecords { get; set; } = new();
    public List<BnkInstrumentData> Instruments { get; set; } = new();
}