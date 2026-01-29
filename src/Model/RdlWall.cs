namespace ii.Ascend.Model;

public class RdlWall
{
    // Wall types
    public const byte TypeNormal = 0;
    public const byte TypeBlastable = 1;
    public const byte TypeDoor = 2;
    public const byte TypeIllusion = 3;
    public const byte TypeOpen = 4;
    public const byte TypeClosed = 5;

    // Wall flags
    public const byte FlagBlasted = 1;
    public const byte FlagDoorOpened = 2;
    public const byte FlagDoorLocked = 8;
    public const byte FlagDoorAuto = 16;
    public const byte FlagIllusionOff = 32;

    // Wall states
    public const byte StateDoorClosed = 0;
    public const byte StateDoorOpening = 1;
    public const byte StateDoorWaiting = 2;
    public const byte StateDoorClosing = 3;

    // Key types
    public const byte KeyNone = 1;
    public const byte KeyBlue = 2;
    public const byte KeyRed = 4;
    public const byte KeyGold = 8;

    public int SegNum { get; set; }
    public int SideNum { get; set; }
    public int Hps { get; set; }
    public int LinkedWall { get; set; } = -1;
    public byte Type { get; set; }
    public byte Flags { get; set; }
    public byte State { get; set; }
    public sbyte Trigger { get; set; } = -1;
    public sbyte ClipNum { get; set; }
    public byte Keys { get; set; }
}

public class RdlActiveDoor
{
    public int NumParts { get; set; }
    public short[] FrontWallNum { get; set; } = new short[2];
    public short[] BackWallNum { get; set; } = new short[2];
    public int Time { get; set; }
}