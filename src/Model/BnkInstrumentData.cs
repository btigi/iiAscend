namespace ii.Ascend.Model;

public class BnkInstrumentData
{
    public byte InstrumentType { get; set; } // 0=melodic, 1=percussive
    public byte VoiceNumber { get; set; } // for percussive instruments
    public BnkOperatorParameters Operator0 { get; set; } = null!;
    public BnkOperatorParameters Operator1 { get; set; } = null!;
    public byte Operator0Waveform { get; set; }
    public byte Operator1Waveform { get; set; }
}