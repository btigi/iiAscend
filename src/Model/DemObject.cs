namespace ii.Ascend.Model;

public class DemObject
{
	public byte RenderType { get; set; }
	public byte Type { get; set; }
	public byte Id { get; set; }
	public byte Flags { get; set; }
	public short Signature { get; set; }
	public DemShortPos Position { get; set; }
	public byte ControlType { get; set; }
	public byte MovementType { get; set; }
	public int? Size { get; set; }
	public VmsVector LastPos { get; set; }
	public int? Lifeleft { get; set; }
	public byte? LifeleftByte { get; set; }
	public bool? Cloaked { get; set; }

	// Movement-specific
	public VmsVector? Velocity { get; set; }
	public VmsVector? Thrust { get; set; }
	public VmsVector? SpinRate { get; set; }

	// Control-specific
	public int? SpawnTime { get; set; }
	public int? DeleteTime { get; set; }
	public short? DeleteObjNum { get; set; }
	public int? LightIntensity { get; set; }

	// Render-specific
	public int? ModelNum { get; set; }
	public int? SubobjFlags { get; set; }
	public VmsAngvec[]? AnimAngles { get; set; }
	public int? VClipNum { get; set; }
	public int? FrameTime { get; set; }
	public byte? FrameNum { get; set; }
	public int? Tmo { get; set; }
}