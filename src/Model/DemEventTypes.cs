namespace ii.Ascend.Model;

public static class DemEventTypes
{
    public const byte Eof = 0;
    public const byte StartDemo = 1;
    public const byte StartFrame = 2;
    public const byte ViewerObject = 3;
    public const byte RenderObject = 4;
    public const byte Sound = 5;
    public const byte SoundOnce = 6;
    public const byte Sound3D = 7;
    public const byte WallHitProcess = 8;
    public const byte Trigger = 9;
    public const byte HostageRescued = 10;
    public const byte Sound3DOnce = 11;
    public const byte MorphFrame = 12;
    public const byte WallToggle = 13;
    public const byte HudMessage = 14;
    public const byte ControlCenterDestroyed = 15;
    public const byte PaletteEffect = 16;
    public const byte PlayerEnergy = 17;
    public const byte PlayerShield = 18;
    public const byte PlayerFlags = 19;
    public const byte PlayerWeapon = 20;
    public const byte EffectBlowup = 21;
    public const byte HomingDistance = 22;
    public const byte Letterbox = 23;
    public const byte RestoreCockpit = 24;
    public const byte Rearview = 25;
    public const byte WallSetTmapNum1 = 26;
    public const byte WallSetTmapNum2 = 27;
    public const byte NewLevel = 28;
    public const byte MultiCloak = 29;
    public const byte MultiDecloak = 30;
    public const byte RestoreRearview = 31;
    // Events 32-42: D1 Full and D2 only
    public const byte MultiDeath = 32;
    public const byte MultiKill = 33;
    public const byte MultiConnect = 34;
    public const byte MultiReconnect = 35;
    public const byte MultiDisconnect = 36;
    public const byte MultiScore = 37;
    public const byte PlayerScore = 38;
    public const byte PrimaryAmmo = 39;
    public const byte SecondaryAmmo = 40;
    public const byte DoorOpening = 41;
    public const byte LaserLevel = 42;
    // Events 43-50: D2 only
    public const byte PlayerAfterburner = 43;
    public const byte CloakingWall = 44;
    public const byte ChangeCockpit = 45;
    public const byte StartGuided = 46;
    public const byte EndGuided = 47;
    public const byte SecretThingy = 48;
    public const byte LinkSoundToObject = 49;
    public const byte KillSoundToObject = 50;
}