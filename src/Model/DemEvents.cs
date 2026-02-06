namespace ii.Ascend.Model;

// Event 0: EOF
public class DemEofEvent : IDemEvent
{
    public byte EventType => DemEventTypes.Eof;
}

// Event 1: START_DEMO
public class DemStartDemoEvent : IDemEvent
{
    public byte EventType => DemEventTypes.StartDemo;
    public byte Version { get; set; }
    public byte GameType { get; set; }
    public int GameTime { get; set; }
    public int GameMode { get; set; }
    public byte? TeamVector { get; set; }
    public string? TeamName0 { get; set; }
    public string? TeamName1 { get; set; }
    public byte? NumPlayers { get; set; }
    public List<DemPlayerInfo>? Players { get; set; }
    public int? PlayerScore { get; set; }
    public short[]? PrimaryAmmo { get; set; }
    public short[]? SecondaryAmmo { get; set; }
    public byte LaserLevel { get; set; }
    public string CurrentMission { get; set; } = string.Empty;
    public byte Energy { get; set; }
    public byte Shield { get; set; }
    public int Flags { get; set; }
    public byte PrimaryWeapon { get; set; }
    public byte SecondaryWeapon { get; set; }
}

public class DemPlayerInfo
{
    public string Callsign { get; set; } = string.Empty;
    public byte Connected { get; set; }
    public int? Score { get; set; }
    public short? NetKilledTotal { get; set; }
    public short? NetKillsTotal { get; set; }
}

// Event 2: START_FRAME
public class DemStartFrameEvent : IDemEvent
{
    public byte EventType => DemEventTypes.StartFrame;
    public short LastFrameLength { get; set; }
    public int FrameCount { get; set; }
    public int RecordedTime { get; set; }
}

// Event 3: VIEWER_OBJECT
public class DemViewerObjectEvent : IDemEvent
{
    public byte EventType => DemEventTypes.ViewerObject;
    public byte? WhichWindow { get; set; } // D2 only
    public DemObject Object { get; set; } = new();
}

// Event 4: RENDER_OBJECT
public class DemRenderObjectEvent : IDemEvent
{
    public byte EventType => DemEventTypes.RenderObject;
    public DemObject Object { get; set; } = new();
}

// Event 5: SOUND
public class DemSoundEvent : IDemEvent
{
    public byte EventType => DemEventTypes.Sound;
    public int SoundNo { get; set; }
}

// Event 6: SOUND_ONCE
public class DemSoundOnceEvent : IDemEvent
{
    public byte EventType => DemEventTypes.SoundOnce;
    public int SoundNo { get; set; }
}

// Event 7: SOUND_3D
public class DemSound3DEvent : IDemEvent
{
    public byte EventType => DemEventTypes.Sound3D;
    public int SoundNo { get; set; }
    public int Angle { get; set; }
    public int Volume { get; set; }
}

// Event 8: WALL_HIT_PROCESS
public class DemWallHitProcessEvent : IDemEvent
{
    public byte EventType => DemEventTypes.WallHitProcess;
    public int SegNum { get; set; }
    public int Side { get; set; }
    public int Damage { get; set; }
    public int Player { get; set; }
}

// Event 9: TRIGGER
public class DemTriggerEvent : IDemEvent
{
    public byte EventType => DemEventTypes.Trigger;
    public int SegNum { get; set; }
    public int Side { get; set; }
    public int ObjNum { get; set; }
    public int? Shot { get; set; } // D2 only
}

// Event 10: HOSTAGE_RESCUED
public class DemHostageRescuedEvent : IDemEvent
{
    public byte EventType => DemEventTypes.HostageRescued;
    public int HostageNumber { get; set; }
}

// Event 11: SOUND_3D_ONCE
public class DemSound3DOnceEvent : IDemEvent
{
    public byte EventType => DemEventTypes.Sound3DOnce;
    public int SoundNo { get; set; }
    public int Angle { get; set; }
    public int Volume { get; set; }
}

// Event 12: MORPH_FRAME
public class DemMorphFrameEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MorphFrame;
    public DemObject Object { get; set; } = new();
}

// Event 13: WALL_TOGGLE
public class DemWallToggleEvent : IDemEvent
{
    public byte EventType => DemEventTypes.WallToggle;
    public int SegNum { get; set; }
    public int Side { get; set; }
}

// Event 14: HUD_MESSAGE
public class DemHudMessageEvent : IDemEvent
{
    public byte EventType => DemEventTypes.HudMessage;
    public string Message { get; set; } = string.Empty;
}

// Event 15: CONTROL_CENTER_DESTROYED
public class DemControlCenterDestroyedEvent : IDemEvent
{
    public byte EventType => DemEventTypes.ControlCenterDestroyed;
    public int CountdownSecondsLeft { get; set; }
}

// Event 16: PALETTE_EFFECT
public class DemPaletteEffectEvent : IDemEvent
{
    public byte EventType => DemEventTypes.PaletteEffect;
    public short Red { get; set; }
    public short Green { get; set; }
    public short Blue { get; set; }
}

// Event 17: PLAYER_ENERGY
public class DemPlayerEnergyEvent : IDemEvent
{
    public byte EventType => DemEventTypes.PlayerEnergy;
    public byte? OldEnergy { get; set; } // Not in D1 Shareware
    public byte Energy { get; set; }
}

// Event 18: PLAYER_SHIELD
public class DemPlayerShieldEvent : IDemEvent
{
    public byte EventType => DemEventTypes.PlayerShield;
    public byte? OldShield { get; set; } // Not in D1 Shareware
    public byte Shield { get; set; }
}

// Event 19: PLAYER_FLAGS
public class DemPlayerFlagsEvent : IDemEvent
{
    public byte EventType => DemEventTypes.PlayerFlags;
    public short OldFlags { get; set; }
    public short Flags { get; set; }
}

// Event 20: PLAYER_WEAPON
public class DemPlayerWeaponEvent : IDemEvent
{
    public byte EventType => DemEventTypes.PlayerWeapon;
    public byte WeaponType { get; set; } // 0=primary, 1=secondary
    public byte WeaponNum { get; set; }
    public byte? OldWeapon { get; set; } // Not in D1 Shareware
}

// Event 21: EFFECT_BLOWUP
public class DemEffectBlowupEvent : IDemEvent
{
    public byte EventType => DemEventTypes.EffectBlowup;
    public short SegNum { get; set; }
    public byte Side { get; set; }
    public VmsVector Point { get; set; }
}

// Event 22: HOMING_DISTANCE
public class DemHomingDistanceEvent : IDemEvent
{
    public byte EventType => DemEventTypes.HomingDistance;
    public short Distance { get; set; }
}

// Event 23: LETTERBOX
public class DemLetterboxEvent : IDemEvent
{
    public byte EventType => DemEventTypes.Letterbox;
}

// Event 24: RESTORE_COCKPIT
public class DemRestoreCockpitEvent : IDemEvent
{
    public byte EventType => DemEventTypes.RestoreCockpit;
}

// Event 25: REARVIEW
public class DemRearviewEvent : IDemEvent
{
    public byte EventType => DemEventTypes.Rearview;
}

// Event 26: WALL_SET_TMAP_NUM1
public class DemWallSetTmapNum1Event : IDemEvent
{
    public byte EventType => DemEventTypes.WallSetTmapNum1;
    public short Seg { get; set; }
    public byte Side { get; set; }
    public short CSeg { get; set; }
    public byte CSide { get; set; }
    public short Tmap { get; set; }
}

// Event 27: WALL_SET_TMAP_NUM2
public class DemWallSetTmapNum2Event : IDemEvent
{
    public byte EventType => DemEventTypes.WallSetTmapNum2;
    public short Seg { get; set; }
    public byte Side { get; set; }
    public short CSeg { get; set; }
    public byte CSide { get; set; }
    public short Tmap { get; set; }
}

// Event 28: NEW_LEVEL
public class DemNewLevelEvent : IDemEvent
{
    public byte EventType => DemEventTypes.NewLevel;
    public byte NewLevel { get; set; }
    public byte OldLevel { get; set; }
    public List<DemWallState>? Walls { get; set; } // D2 only, when JustStartedPlayback
}

public class DemWallState
{
    public byte Type { get; set; }
    public byte Flags { get; set; }
    public byte State { get; set; }
    public short TmapNum1 { get; set; }
    public short TmapNum2 { get; set; }
}

// Event 29: MULTI_CLOAK
public class DemMultiCloakEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MultiCloak;
    public byte PlayerNum { get; set; }
}

// Event 30: MULTI_DECLOAK
public class DemMultiDecloakEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MultiDecloak;
    public byte PlayerNum { get; set; }
}

// Event 31: RESTORE_REARVIEW
public class DemRestoreRearviewEvent : IDemEvent
{
    public byte EventType => DemEventTypes.RestoreRearview;
}

// Event 32: MULTI_DEATH (D1 Full and D2 only)
public class DemMultiDeathEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MultiDeath;
    public byte PlayerNum { get; set; }
}

// Event 33: MULTI_KILL (D1 Full and D2 only)
public class DemMultiKillEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MultiKill;
    public byte PlayerNum { get; set; }
    public byte Kills { get; set; } // 1=kill, 255=suicide
}

// Event 34: MULTI_CONNECT (D1 Full and D2 only)
public class DemMultiConnectEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MultiConnect;
    public byte PlayerNum { get; set; }
    public byte NewPlayer { get; set; }
    public string? OldCallsign { get; set; }
    public int? KilledTotal { get; set; }
    public int? KillsTotal { get; set; }
    public string NewCallsign { get; set; } = string.Empty;
}

// Event 35: MULTI_RECONNECT (D1 Full and D2 only)
public class DemMultiReconnectEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MultiReconnect;
    public byte PlayerNum { get; set; }
}

// Event 36: MULTI_DISCONNECT (D1 Full and D2 only)
public class DemMultiDisconnectEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MultiDisconnect;
    public byte PlayerNum { get; set; }
}

// Event 37: MULTI_SCORE (D1 Full and D2 only)
public class DemMultiScoreEvent : IDemEvent
{
    public byte EventType => DemEventTypes.MultiScore;
    public byte PlayerNum { get; set; }
    public int Score { get; set; }
}

// Event 38: PLAYER_SCORE (D1 Full and D2 only)
public class DemPlayerScoreEvent : IDemEvent
{
    public byte EventType => DemEventTypes.PlayerScore;
    public int Score { get; set; }
}

// Event 39: PRIMARY_AMMO (D1 Full and D2 only)
public class DemPrimaryAmmoEvent : IDemEvent
{
    public byte EventType => DemEventTypes.PrimaryAmmo;
    public short OldAmmo { get; set; }
    public short NewAmmo { get; set; }
}

// Event 40: SECONDARY_AMMO (D1 Full and D2 only)
public class DemSecondaryAmmoEvent : IDemEvent
{
    public byte EventType => DemEventTypes.SecondaryAmmo;
    public short OldAmmo { get; set; }
    public short NewAmmo { get; set; }
}

// Event 41: DOOR_OPENING (D1 Full and D2 only)
public class DemDoorOpeningEvent : IDemEvent
{
    public byte EventType => DemEventTypes.DoorOpening;
    public short SegNum { get; set; }
    public byte Side { get; set; }
}

// Event 42: LASER_LEVEL (D1 Full and D2 only)
public class DemLaserLevelEvent : IDemEvent
{
    public byte EventType => DemEventTypes.LaserLevel;
    public short OldLevel { get; set; }
    public short NewLevel { get; set; }
}

// Event 43: PLAYER_AFTERBURNER (D2 only)
public class DemPlayerAfterburnerEvent : IDemEvent
{
    public byte EventType => DemEventTypes.PlayerAfterburner;
    public short OldAfterburner { get; set; }
    public short Afterburner { get; set; }
}

// Event 44: CLOAKING_WALL (D2 only)
public class DemCloakingWallEvent : IDemEvent
{
    public byte EventType => DemEventTypes.CloakingWall;
    public byte FrontWallNum { get; set; }
    public byte BackWallNum { get; set; }
    public byte Type { get; set; }
    public byte State { get; set; }
    public byte CloakValue { get; set; }
    public short L0 { get; set; }
    public short L1 { get; set; }
    public short L2 { get; set; }
    public short L3 { get; set; }
}

// Event 45: CHANGE_COCKPIT (D2 only)
public class DemChangeCockpitEvent : IDemEvent
{
    public byte EventType => DemEventTypes.ChangeCockpit;
    public int Cockpit { get; set; }
}

// Event 46: START_GUIDED (D2 only)
public class DemStartGuidedEvent : IDemEvent
{
    public byte EventType => DemEventTypes.StartGuided;
}

// Event 47: END_GUIDED (D2 only)
public class DemEndGuidedEvent : IDemEvent
{
    public byte EventType => DemEventTypes.EndGuided;
}

// Event 48: SECRET_THINGY (D2 only)
public class DemSecretThingyEvent : IDemEvent
{
    public byte EventType => DemEventTypes.SecretThingy;
    public int Truth { get; set; }
}

// Event 49: LINK_SOUND_TO_OBJECT (D2 only)
public class DemLinkSoundToObjectEvent : IDemEvent
{
    public byte EventType => DemEventTypes.LinkSoundToObject;
    public int SoundNo { get; set; }
    public int Signature { get; set; }
    public int MaxVolume { get; set; }
    public int MaxDistance { get; set; }
    public int LoopStart { get; set; }
    public int LoopEnd { get; set; }
}

// Event 50: KILL_SOUND_TO_OBJECT (D2 only)
public class DemKillSoundToObjectEvent : IDemEvent
{
    public byte EventType => DemEventTypes.KillSoundToObject;
    public int Signature { get; set; }
}