namespace ii.Ascend.Model;

public class PolyModel
{
    public const int MaxSubmodels = 10;

    public int NumModels { get; set; }
    public int ModelDataSize { get; set; }
    public byte[] ModelData { get; set; } = Array.Empty<byte>();
    public int[] SubmodelPtrs { get; set; } = new int[MaxSubmodels];
    public VmsVector[] SubmodelOffsets { get; set; } = new VmsVector[MaxSubmodels];
    public VmsVector[] SubmodelNorms { get; set; } = new VmsVector[MaxSubmodels];
    public VmsVector[] SubmodelPnts { get; set; } = new VmsVector[MaxSubmodels];
    public int[] SubmodelRads { get; set; } = new int[MaxSubmodels];
    public byte[] SubmodelParents { get; set; } = new byte[MaxSubmodels];
    public VmsVector[] SubmodelMins { get; set; } = new VmsVector[MaxSubmodels];
    public VmsVector[] SubmodelMaxs { get; set; } = new VmsVector[MaxSubmodels];
    public VmsVector Mins { get; set; }
    public VmsVector Maxs { get; set; }
    public int Rad { get; set; }
    public byte NumTextures { get; set; }
    public ushort FirstTexture { get; set; }
    public byte SimplerModel { get; set; }
}