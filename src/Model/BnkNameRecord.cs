namespace ii.Ascend.Model;

public class BnkNameRecord
{
    public ushort IndexIntoDataSection { get; set; }
    public byte Used { get; set; } // 0 if used, 1 otherwise
    public string InstrumentName { get; set; } = string.Empty;
}