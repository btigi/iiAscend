namespace ii.Ascend.Model;

public class BnkOperatorParameters
{
    public byte KeyScaleLevel { get; set; }
    public byte FrequencyMultiplier { get; set; }
    public byte FeedBack { get; set; }
    public byte AttackRate { get; set; }
    public byte SustainLevel { get; set; }
    public byte SustainingSound { get; set; }
    public byte DecayRate { get; set; }
    public byte OutputLevel { get; set; }
    public byte AmplitudeVibrato { get; set; }
    public byte FrequencyVibrato { get; set; }
    public byte EnvelopeScaling { get; set; }
    public byte Type { get; set; } // 0=FM sound, 1=Additive sound (for operator 0 only)
}