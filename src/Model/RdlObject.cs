namespace ii.Ascend.Model;

public class RdlObject
{
    // Object types
    public const byte TypeNone = 255;
    public const byte TypeWall = 0;
    public const byte TypeFireball = 1;
    public const byte TypeRobot = 2;
    public const byte TypeHostage = 3;
    public const byte TypePlayer = 4;
    public const byte TypeWeapon = 5;
    public const byte TypeCamera = 6;
    public const byte TypePowerup = 7;
    public const byte TypeDebris = 8;
    public const byte TypeControlCenter = 9;
    public const byte TypeFlare = 10;
    public const byte TypeClutter = 11;
    public const byte TypeGhost = 12;
    public const byte TypeLight = 13;
    public const byte TypeCoop = 14;

    // Control types
    public const byte CtrlNone = 0;
    public const byte CtrlAi = 1;
    public const byte CtrlExplosion = 2;
    public const byte CtrlFlying = 4;
    public const byte CtrlSlew = 5;
    public const byte CtrlFlythrough = 6;
    public const byte CtrlWeapon = 9;
    public const byte CtrlRepairCenter = 10;
    public const byte CtrlMorph = 11;
    public const byte CtrlDebris = 12;
    public const byte CtrlPowerup = 13;
    public const byte CtrlLight = 14;
    public const byte CtrlRemote = 15;
    public const byte CtrlControlCenter = 16;

    // Movement types
    public const byte MoveNone = 0;
    public const byte MovePhysics = 1;
    public const byte MoveSpinning = 3;

    // Render types
    public const byte RenderNone = 0;
    public const byte RenderPolyobj = 1;
    public const byte RenderFireball = 2;
    public const byte RenderLaser = 3;
    public const byte RenderHostage = 4;
    public const byte RenderPowerup = 5;
    public const byte RenderMorph = 6;
    public const byte RenderWeaponVclip = 7;

    public byte Type { get; set; }
    public byte Id { get; set; }
    public byte ControlType { get; set; }
    public byte MovementType { get; set; }
    public byte RenderType { get; set; }
    public byte Flags { get; set; }
    public short SegNum { get; set; }
    public VmsVector Position { get; set; }
    public VmsMatrix Orientation { get; set; }
    public int Size { get; set; }
    public int Shields { get; set; }
    public VmsVector LastPos { get; set; }
    public sbyte ContainsType { get; set; }
    public sbyte ContainsId { get; set; }
    public sbyte ContainsCount { get; set; }
    public RdlPhysicsInfo? PhysicsInfo { get; set; }
    public VmsVector? SpinRate { get; set; }
    public RdlAiInfo? AiInfo { get; set; }
    public RdlExplosionInfo? ExplosionInfo { get; set; }
    public RdlLaserInfo? LaserInfo { get; set; }
    public int? LightIntensity { get; set; }
    public int? PowerupCount { get; set; }
    public RdlPolyObjInfo? PolyObjInfo { get; set; }
    public RdlVClipInfo? VClipInfo { get; set; }
}

public struct VmsMatrix
{
    public VmsVector RVec { get; set; } // Right vector
    public VmsVector UVec { get; set; } // Up vector
    public VmsVector FVec { get; set; } // Forward vector

    public VmsMatrix(VmsVector rvec, VmsVector uvec, VmsVector fvec)
    {
        RVec = rvec;
        UVec = uvec;
        FVec = fvec;
    }
}

public class RdlPhysicsInfo
{
    public VmsVector Velocity { get; set; }
    public VmsVector Thrust { get; set; }
    public int Mass { get; set; }
    public int Drag { get; set; }
    public int Brakes { get; set; }
    public VmsVector RotVel { get; set; }
    public VmsVector RotThrust { get; set; }
    public short TurnRoll { get; set; }
    public ushort Flags { get; set; }
}

public class RdlAiInfo
{
    public const int MaxAiFlags = 11;

    public byte Behavior { get; set; }
    public byte[] Flags { get; set; } = new byte[MaxAiFlags];
    public short HideSegment { get; set; }
    public short HideIndex { get; set; }
    public short PathLength { get; set; }
    public short CurPathIndex { get; set; }
    public short FollowPathStartSeg { get; set; }
    public short FollowPathEndSeg { get; set; }
}

public class RdlExplosionInfo
{
    public int SpawnTime { get; set; }
    public int DeleteTime { get; set; }
    public short DeleteObjNum { get; set; }
}

public class RdlLaserInfo
{
    public short ParentType { get; set; }
    public short ParentNum { get; set; }
    public int ParentSignature { get; set; }
}

public class RdlPolyObjInfo
{
    public const int MaxSubmodels = 10;

    public int ModelNum { get; set; }
    public VmsAngvec[] AnimAngles { get; set; } = new VmsAngvec[MaxSubmodels];
    public int SubobjFlags { get; set; }
    public int TmapOverride { get; set; }
}

public class RdlVClipInfo
{
    public int VClipNum { get; set; }
    public int FrameTime { get; set; }
    public byte FrameNum { get; set; }
}