using ii.Ascend.Model;

namespace ii.Ascend;

public class DemProcessor
{
	// Game mode flags
	private const int GM_MULTI = 1;
	private const int GM_TEAM = 2;
	private const int GM_MULTI_COOP = 4;

	// Object type constants
	private const byte OBJ_ROBOT = 2;
	private const byte OBJ_HOSTAGE = 3;
	private const byte OBJ_PLAYER = 4;
	private const byte OBJ_WEAPON = 5;
	private const byte OBJ_CAMERA = 6;
	private const byte OBJ_POWERUP = 7;
	private const byte OBJ_DEBRIS = 8;
	private const byte OBJ_CLUTTER = 11;

	// Render type constants
	private const byte RT_NONE = 0;
	private const byte RT_POLYOBJ = 1;
	private const byte RT_MORPH = 6;
	private const byte RT_WEAPON_VCLIP = 7;
	private const byte RT_POWERUP = 5;
	private const byte RT_FIREBALL = 2;
	private const byte RT_HOSTAGE = 4;

	// Control type constants
	private const byte CT_NONE = 0;
	private const byte CT_AI = 1;
	private const byte CT_EXPLOSION = 2;
	private const byte CT_POWERUP = 13;
	private const byte CT_LIGHT = 14;

	// Movement type constants
	private const byte MT_NONE = 0;
	private const byte MT_PHYSICS = 1;
	private const byte MT_SPINNING = 3;

	private const int MAX_PRIMARY_WEAPONS = 5;
	private const int MAX_SECONDARY_WEAPONS = 5;
	private const byte SPECIAL_REACTOR_ROBOT = 53;

	private byte _version;
	private byte _gameType;
	private bool _justStartedPlayback;

	public DemFile Read(string filename)
	{
		var fileData = File.ReadAllBytes(filename);
		return Read(fileData);
	}

	public DemFile Read(byte[] fileData)
	{
		using var stream = new MemoryStream(fileData);
		using var reader = new BinaryReader(stream);

		var demFile = new DemFile();
		_justStartedPlayback = false;

		while (stream.Position < stream.Length)
		{
			var eventType = reader.ReadByte();
			var demEvent = ReadEvent(reader, eventType);

			if (demEvent != null)
			{
				demFile.Events.Add(demEvent);

				if (eventType == DemEventTypes.StartDemo && demEvent is DemStartDemoEvent startDemo)
				{
					_version = startDemo.Version;
					_gameType = startDemo.GameType;
					demFile.Version = _version;
					demFile.GameType = _gameType;
					_justStartedPlayback = true;
				}

				// Handle NEW_LEVEL event to reset JustStartedPlayback
				if (eventType == DemEventTypes.NewLevel && _gameType == 3)
				{
					_justStartedPlayback = false;
				}

				if (eventType == DemEventTypes.Eof)
					break;
			}
		}

		return demFile;
	}

	private IDemEvent? ReadEvent(BinaryReader reader, byte eventType)
	{
		return eventType switch
		{
			DemEventTypes.Eof => new DemEofEvent(),
			DemEventTypes.StartDemo => ReadStartDemoEvent(reader),
			DemEventTypes.StartFrame => ReadStartFrameEvent(reader),
			DemEventTypes.ViewerObject => ReadViewerObjectEvent(reader),
			DemEventTypes.RenderObject => ReadRenderObjectEvent(reader),
			DemEventTypes.Sound => ReadSoundEvent(reader),
			DemEventTypes.SoundOnce => ReadSoundOnceEvent(reader),
			DemEventTypes.Sound3D => ReadSound3DEvent(reader),
			DemEventTypes.WallHitProcess => ReadWallHitProcessEvent(reader),
			DemEventTypes.Trigger => ReadTriggerEvent(reader),
			DemEventTypes.HostageRescued => ReadHostageRescuedEvent(reader),
			DemEventTypes.Sound3DOnce => ReadSound3DOnceEvent(reader),
			DemEventTypes.MorphFrame => ReadMorphFrameEvent(reader),
			DemEventTypes.WallToggle => ReadWallToggleEvent(reader),
			DemEventTypes.HudMessage => ReadHudMessageEvent(reader),
			DemEventTypes.ControlCenterDestroyed => ReadControlCenterDestroyedEvent(reader),
			DemEventTypes.PaletteEffect => ReadPaletteEffectEvent(reader),
			DemEventTypes.PlayerEnergy => ReadPlayerEnergyEvent(reader),
			DemEventTypes.PlayerShield => ReadPlayerShieldEvent(reader),
			DemEventTypes.PlayerFlags => ReadPlayerFlagsEvent(reader),
			DemEventTypes.PlayerWeapon => ReadPlayerWeaponEvent(reader),
			DemEventTypes.EffectBlowup => ReadEffectBlowupEvent(reader),
			DemEventTypes.HomingDistance => ReadHomingDistanceEvent(reader),
			DemEventTypes.Letterbox => new DemLetterboxEvent(),
			DemEventTypes.RestoreCockpit => new DemRestoreCockpitEvent(),
			DemEventTypes.Rearview => new DemRearviewEvent(),
			DemEventTypes.WallSetTmapNum1 => ReadWallSetTmapNum1Event(reader),
			DemEventTypes.WallSetTmapNum2 => ReadWallSetTmapNum2Event(reader),
			DemEventTypes.NewLevel => ReadNewLevelEvent(reader),
			DemEventTypes.MultiCloak => ReadMultiCloakEvent(reader),
			DemEventTypes.MultiDecloak => ReadMultiDecloakEvent(reader),
			DemEventTypes.RestoreRearview => new DemRestoreRearviewEvent(),
			// D1 Full and D2 events (32-42)
			DemEventTypes.MultiDeath => _gameType >= 2 ? ReadMultiDeathEvent(reader) : null,
			DemEventTypes.MultiKill => _gameType >= 2 ? ReadMultiKillEvent(reader) : null,
			DemEventTypes.MultiConnect => _gameType >= 2 ? ReadMultiConnectEvent(reader) : null,
			DemEventTypes.MultiReconnect => _gameType >= 2 ? ReadMultiReconnectEvent(reader) : null,
			DemEventTypes.MultiDisconnect => _gameType >= 2 ? ReadMultiDisconnectEvent(reader) : null,
			DemEventTypes.MultiScore => _gameType >= 2 ? ReadMultiScoreEvent(reader) : null,
			DemEventTypes.PlayerScore => _gameType >= 2 ? ReadPlayerScoreEvent(reader) : null,
			DemEventTypes.PrimaryAmmo => _gameType >= 2 ? ReadPrimaryAmmoEvent(reader) : null,
			DemEventTypes.SecondaryAmmo => _gameType >= 2 ? ReadSecondaryAmmoEvent(reader) : null,
			DemEventTypes.DoorOpening => _gameType >= 2 ? ReadDoorOpeningEvent(reader) : null,
			DemEventTypes.LaserLevel => _gameType >= 2 ? ReadLaserLevelEvent(reader) : null,
			// D2 only events (43-50)
			DemEventTypes.PlayerAfterburner => _gameType == 3 ? ReadPlayerAfterburnerEvent(reader) : null,
			DemEventTypes.CloakingWall => _gameType == 3 ? ReadCloakingWallEvent(reader) : null,
			DemEventTypes.ChangeCockpit => _gameType == 3 ? ReadChangeCockpitEvent(reader) : null,
			DemEventTypes.StartGuided => _gameType == 3 ? new DemStartGuidedEvent() : null,
			DemEventTypes.EndGuided => _gameType == 3 ? new DemEndGuidedEvent() : null,
			DemEventTypes.SecretThingy => _gameType == 3 ? ReadSecretThingyEvent(reader) : null,
			DemEventTypes.LinkSoundToObject => _gameType == 3 ? ReadLinkSoundToObjectEvent(reader) : null,
			DemEventTypes.KillSoundToObject => _gameType == 3 ? ReadKillSoundToObjectEvent(reader) : null,
			_ => null
		};
	}

	private DemStartDemoEvent ReadStartDemoEvent(BinaryReader reader)
	{
		var evt = new DemStartDemoEvent
		{
			Version = reader.ReadByte(),
			GameType = reader.ReadByte(),
			GameTime = reader.ReadInt32(),
			GameMode = reader.ReadInt32()
		};

		// Store for later use
		_version = evt.Version;
		_gameType = evt.GameType;

		// D1 Shareware
		if (evt.GameType == 1)
		{
			if ((evt.GameMode & GM_MULTI) != 0)
			{
				evt.TeamVector = reader.ReadByte();
			}
		}
		else // D1 Full or D2
		{
			if ((evt.GameMode & GM_TEAM) != 0)
			{
				evt.TeamVector = reader.ReadByte();
				evt.TeamName0 = ReadNullTerminatedString(reader);
				evt.TeamName1 = ReadNullTerminatedString(reader);
			}

			if ((evt.GameMode & GM_MULTI) != 0)
			{
				evt.NumPlayers = reader.ReadByte();
				evt.Players = new List<DemPlayerInfo>();

				for (int i = 0; i < evt.NumPlayers; i++)
				{
					var player = new DemPlayerInfo
					{
						Callsign = ReadNullTerminatedString(reader),
						Connected = reader.ReadByte()
					};

					if ((evt.GameMode & GM_MULTI_COOP) != 0)
					{
						player.Score = reader.ReadInt32();
					}
					else
					{
						player.NetKilledTotal = reader.ReadInt16();
						player.NetKillsTotal = reader.ReadInt16();
					}

					evt.Players.Add(player);
				}
			}
			else
			{
				evt.PlayerScore = reader.ReadInt32();
			}

			evt.PrimaryAmmo = new short[MAX_PRIMARY_WEAPONS];
			for (int i = 0; i < MAX_PRIMARY_WEAPONS; i++)
			{
				evt.PrimaryAmmo[i] = reader.ReadInt16();
			}

			evt.SecondaryAmmo = new short[MAX_SECONDARY_WEAPONS];
			for (int i = 0; i < MAX_SECONDARY_WEAPONS; i++)
			{
				evt.SecondaryAmmo[i] = reader.ReadInt16();
			}

			evt.LaserLevel = reader.ReadByte();
			evt.CurrentMission = ReadNullTerminatedString(reader);
		}

		evt.Energy = reader.ReadByte();
		evt.Shield = reader.ReadByte();
		evt.Flags = reader.ReadInt32();
		evt.PrimaryWeapon = reader.ReadByte();
		evt.SecondaryWeapon = reader.ReadByte();

		return evt;
	}

	private DemStartFrameEvent ReadStartFrameEvent(BinaryReader reader)
	{
		return new DemStartFrameEvent
		{
			LastFrameLength = reader.ReadInt16(),
			FrameCount = reader.ReadInt32(),
			RecordedTime = reader.ReadInt32()
		};
	}

	private DemViewerObjectEvent ReadViewerObjectEvent(BinaryReader reader)
	{
		var evt = new DemViewerObjectEvent();

		if (_gameType == 3) // D2
		{
			evt.WhichWindow = reader.ReadByte();
		}

		evt.Object = ReadObject(reader);
		return evt;
	}

	private DemRenderObjectEvent ReadRenderObjectEvent(BinaryReader reader)
	{
		return new DemRenderObjectEvent
		{
			Object = ReadObject(reader)
		};
	}

	private DemObject ReadObject(BinaryReader reader)
	{
		var obj = new DemObject
		{
			RenderType = reader.ReadByte(),
			Type = reader.ReadByte()
		};

		// Early return if not renderable
		if (obj.RenderType == RT_NONE && obj.Type != OBJ_CAMERA)
		{
			return obj;
		}

		obj.Id = reader.ReadByte();
		obj.Flags = reader.ReadByte();
		obj.Signature = reader.ReadInt16();
		obj.Position = ReadShortPos(reader);

		DetermineObjectTypes(obj, reader, out var controlType, out var movementType);
		obj.ControlType = controlType;
		obj.MovementType = movementType;

		// Read size if not default type
		if (obj.Type != OBJ_ROBOT && obj.Type != OBJ_HOSTAGE &&
			obj.Type != OBJ_PLAYER && obj.Type != OBJ_POWERUP && obj.Type != OBJ_CLUTTER)
		{
			obj.Size = reader.ReadInt32();
		}

		obj.LastPos = ReadVector(reader);

		if (obj.Type == OBJ_WEAPON && obj.RenderType == RT_WEAPON_VCLIP)
		{
			obj.Lifeleft = reader.ReadInt32();
		}
		else
		{
			reader.ReadByte(); // Skip byte
		}

		// Check if boss robot is cloaked (D1 Full and D2)
		if (_gameType >= 2 && obj.Type == OBJ_ROBOT)
		{
			// Should read Robot_info from HAM
			obj.Cloaked = reader.ReadByte() != 0;
		}

		switch (movementType)
		{
			case MT_PHYSICS:
				obj.Velocity = ReadVector(reader);
				obj.Thrust = ReadVector(reader);
				break;
			case MT_SPINNING:
				obj.SpinRate = ReadVector(reader);
				break;
		}

		switch (controlType)
		{
			case CT_EXPLOSION:
				obj.SpawnTime = reader.ReadInt32();
				obj.DeleteTime = reader.ReadInt32();
				obj.DeleteObjNum = reader.ReadInt16();
				break;
			case CT_LIGHT:
				obj.LightIntensity = reader.ReadInt32();
				break;
		}

		switch (obj.RenderType)
		{
			case RT_MORPH:
			case RT_POLYOBJ:
				if (obj.Type != OBJ_ROBOT && obj.Type != OBJ_PLAYER && obj.Type != OBJ_CLUTTER)
				{
					obj.ModelNum = reader.ReadInt32();
					obj.SubobjFlags = reader.ReadInt32();
				}

				if (obj.Type != OBJ_PLAYER && obj.Type != OBJ_DEBRIS)
				{
					// Should read POF from HAM - just read something for now
					var numAngles = 10; // MAX_SUBMODELS
					obj.AnimAngles = new VmsAngvec[numAngles];
					for (int i = 0; i < numAngles; i++)
					{
						obj.AnimAngles[i] = ReadAngVec(reader);
					}
				}

				obj.Tmo = reader.ReadInt32();
				break;

			case RT_POWERUP:
			case RT_WEAPON_VCLIP:
			case RT_FIREBALL:
			case RT_HOSTAGE:
				obj.VClipNum = reader.ReadInt32();
				obj.FrameTime = reader.ReadInt32();
				obj.FrameNum = reader.ReadByte();
				break;
		}

		return obj;
	}

	private void DetermineObjectTypes(DemObject obj, BinaryReader reader, out byte controlType, out byte movementType)
	{
		switch (obj.Type)
		{
			case OBJ_HOSTAGE:
				controlType = CT_POWERUP;
				movementType = MT_NONE;
				break;
			case OBJ_ROBOT:
				controlType = CT_AI;
				if (_gameType == 3 && obj.Id == SPECIAL_REACTOR_ROBOT)
					movementType = MT_NONE;
				else
					movementType = MT_PHYSICS;
				break;
			case OBJ_POWERUP:
				controlType = CT_POWERUP;
				movementType = reader.ReadByte();
				break;
			case OBJ_PLAYER:
				controlType = CT_NONE;
				movementType = MT_PHYSICS;
				break;
			case OBJ_CLUTTER:
				controlType = CT_NONE;
				movementType = MT_NONE;
				break;
			default:
				controlType = reader.ReadByte();
				movementType = reader.ReadByte();
				break;
		}
	}

	private DemShortPos ReadShortPos(BinaryReader reader)
	{
		return new DemShortPos
		{
			X = reader.ReadInt16(),
			Y = reader.ReadInt16(),
			Z = reader.ReadInt16(),
			Segment = reader.ReadInt16(),
			VelX = reader.ReadInt16(),
			VelY = reader.ReadInt16(),
			VelZ = reader.ReadInt16(),
			Pitch = reader.ReadInt16(),
			Bank = reader.ReadInt16(),
			Heading = reader.ReadInt16()
		};
	}

	private VmsVector ReadVector(BinaryReader reader)
	{
		return new VmsVector(
			reader.ReadInt32(),
			reader.ReadInt32(),
			reader.ReadInt32()
		);
	}

	private VmsAngvec ReadAngVec(BinaryReader reader)
	{
		return new VmsAngvec(
			reader.ReadInt16(),
			reader.ReadInt16(),
			reader.ReadInt16()
		);
	}

	private string ReadNullTerminatedString(BinaryReader reader)
	{
		var bytes = new List<byte>();
		byte b;
		while ((b = reader.ReadByte()) != 0)
		{
			bytes.Add(b);
		}
		return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
	}

	private DemSoundEvent ReadSoundEvent(BinaryReader reader)
	{
		return new DemSoundEvent { SoundNo = reader.ReadInt32() };
	}

	private DemSoundOnceEvent ReadSoundOnceEvent(BinaryReader reader)
	{
		return new DemSoundOnceEvent { SoundNo = reader.ReadInt32() };
	}

	private DemSound3DEvent ReadSound3DEvent(BinaryReader reader)
	{
		return new DemSound3DEvent
		{
			SoundNo = reader.ReadInt32(),
			Angle = reader.ReadInt32(),
			Volume = reader.ReadInt32()
		};
	}

	private DemWallHitProcessEvent ReadWallHitProcessEvent(BinaryReader reader)
	{
		return new DemWallHitProcessEvent
		{
			SegNum = reader.ReadInt32(),
			Side = reader.ReadInt32(),
			Damage = reader.ReadInt32(),
			Player = reader.ReadInt32()
		};
	}

	private DemTriggerEvent ReadTriggerEvent(BinaryReader reader)
	{
		var evt = new DemTriggerEvent
		{
			SegNum = reader.ReadInt32(),
			Side = reader.ReadInt32(),
			ObjNum = reader.ReadInt32()
		};
		if (_gameType == 3)
			evt.Shot = reader.ReadInt32();
		return evt;
	}

	private DemHostageRescuedEvent ReadHostageRescuedEvent(BinaryReader reader)
	{
		return new DemHostageRescuedEvent { HostageNumber = reader.ReadInt32() };
	}

	private DemSound3DOnceEvent ReadSound3DOnceEvent(BinaryReader reader)
	{
		return new DemSound3DOnceEvent
		{
			SoundNo = reader.ReadInt32(),
			Angle = reader.ReadInt32(),
			Volume = reader.ReadInt32()
		};
	}

	private DemMorphFrameEvent ReadMorphFrameEvent(BinaryReader reader)
	{
		return new DemMorphFrameEvent { Object = ReadObject(reader) };
	}

	private DemWallToggleEvent ReadWallToggleEvent(BinaryReader reader)
	{
		return new DemWallToggleEvent
		{
			SegNum = reader.ReadInt32(),
			Side = reader.ReadInt32()
		};
	}

	private DemHudMessageEvent ReadHudMessageEvent(BinaryReader reader)
	{
		return new DemHudMessageEvent { Message = ReadNullTerminatedString(reader) };
	}

	private DemControlCenterDestroyedEvent ReadControlCenterDestroyedEvent(BinaryReader reader)
	{
		return new DemControlCenterDestroyedEvent { CountdownSecondsLeft = reader.ReadInt32() };
	}

	private DemPaletteEffectEvent ReadPaletteEffectEvent(BinaryReader reader)
	{
		return new DemPaletteEffectEvent
		{
			Red = reader.ReadInt16(),
			Green = reader.ReadInt16(),
			Blue = reader.ReadInt16()
		};
	}

	private DemPlayerEnergyEvent ReadPlayerEnergyEvent(BinaryReader reader)
	{
		var evt = new DemPlayerEnergyEvent();
		if (_gameType != 1) // Not D1 Shareware
			evt.OldEnergy = reader.ReadByte();
		evt.Energy = reader.ReadByte();
		return evt;
	}

	private DemPlayerShieldEvent ReadPlayerShieldEvent(BinaryReader reader)
	{
		var evt = new DemPlayerShieldEvent();
		if (_gameType != 1) // Not D1 Shareware
			evt.OldShield = reader.ReadByte();
		evt.Shield = reader.ReadByte();
		return evt;
	}

	private DemPlayerFlagsEvent ReadPlayerFlagsEvent(BinaryReader reader)
	{
		return new DemPlayerFlagsEvent
		{
			OldFlags = reader.ReadInt16(),
			Flags = reader.ReadInt16()
		};
	}

	private DemPlayerWeaponEvent ReadPlayerWeaponEvent(BinaryReader reader)
	{
		var evt = new DemPlayerWeaponEvent
		{
			WeaponType = reader.ReadByte(),
			WeaponNum = reader.ReadByte()
		};
		if (_gameType != 1) // Not D1 Shareware
			evt.OldWeapon = reader.ReadByte();
		return evt;
	}

	private DemEffectBlowupEvent ReadEffectBlowupEvent(BinaryReader reader)
	{
		return new DemEffectBlowupEvent
		{
			SegNum = reader.ReadInt16(),
			Side = reader.ReadByte(),
			Point = ReadVector(reader)
		};
	}

	private DemHomingDistanceEvent ReadHomingDistanceEvent(BinaryReader reader)
	{
		return new DemHomingDistanceEvent { Distance = reader.ReadInt16() };
	}

	private DemWallSetTmapNum1Event ReadWallSetTmapNum1Event(BinaryReader reader)
	{
		return new DemWallSetTmapNum1Event
		{
			Seg = reader.ReadInt16(),
			Side = reader.ReadByte(),
			CSeg = reader.ReadInt16(),
			CSide = reader.ReadByte(),
			Tmap = reader.ReadInt16()
		};
	}

	private DemWallSetTmapNum2Event ReadWallSetTmapNum2Event(BinaryReader reader)
	{
		return new DemWallSetTmapNum2Event
		{
			Seg = reader.ReadInt16(),
			Side = reader.ReadByte(),
			CSeg = reader.ReadInt16(),
			CSide = reader.ReadByte(),
			Tmap = reader.ReadInt16()
		};
	}

	private DemNewLevelEvent ReadNewLevelEvent(BinaryReader reader)
	{
		var evt = new DemNewLevelEvent
		{
			NewLevel = reader.ReadByte(),
			OldLevel = reader.ReadByte()
		};

		if (_gameType == 3 && _justStartedPlayback)
		{
			var numWalls = reader.ReadInt32();
			evt.Walls = new List<DemWallState>();

			for (int i = 0; i < numWalls; i++)
			{
				evt.Walls.Add(new DemWallState
				{
					Type = reader.ReadByte(),
					Flags = reader.ReadByte(),
					State = reader.ReadByte(),
					TmapNum1 = reader.ReadInt16(),
					TmapNum2 = reader.ReadInt16()
				});
			}
		}

		return evt;
	}

	private DemMultiCloakEvent ReadMultiCloakEvent(BinaryReader reader)
	{
		return new DemMultiCloakEvent { PlayerNum = reader.ReadByte() };
	}

	private DemMultiDecloakEvent ReadMultiDecloakEvent(BinaryReader reader)
	{
		return new DemMultiDecloakEvent { PlayerNum = reader.ReadByte() };
	}

	private DemMultiDeathEvent ReadMultiDeathEvent(BinaryReader reader)
	{
		return new DemMultiDeathEvent { PlayerNum = reader.ReadByte() };
	}

	private DemMultiKillEvent ReadMultiKillEvent(BinaryReader reader)
	{
		return new DemMultiKillEvent
		{
			PlayerNum = reader.ReadByte(),
			Kills = reader.ReadByte()
		};
	}

	private DemMultiConnectEvent ReadMultiConnectEvent(BinaryReader reader)
	{
		var evt = new DemMultiConnectEvent
		{
			PlayerNum = reader.ReadByte(),
			NewPlayer = reader.ReadByte()
		};

		if (evt.NewPlayer == 0)
		{
			evt.OldCallsign = ReadNullTerminatedString(reader);
			evt.KilledTotal = reader.ReadInt32();
			evt.KillsTotal = reader.ReadInt32();
		}

		evt.NewCallsign = ReadNullTerminatedString(reader);
		return evt;
	}

	private DemMultiReconnectEvent ReadMultiReconnectEvent(BinaryReader reader)
	{
		return new DemMultiReconnectEvent { PlayerNum = reader.ReadByte() };
	}

	private DemMultiDisconnectEvent ReadMultiDisconnectEvent(BinaryReader reader)
	{
		return new DemMultiDisconnectEvent { PlayerNum = reader.ReadByte() };
	}

	private DemMultiScoreEvent ReadMultiScoreEvent(BinaryReader reader)
	{
		return new DemMultiScoreEvent
		{
			PlayerNum = reader.ReadByte(),
			Score = reader.ReadInt32()
		};
	}

	private DemPlayerScoreEvent ReadPlayerScoreEvent(BinaryReader reader)
	{
		return new DemPlayerScoreEvent { Score = reader.ReadInt32() };
	}

	private DemPrimaryAmmoEvent ReadPrimaryAmmoEvent(BinaryReader reader)
	{
		return new DemPrimaryAmmoEvent
		{
			OldAmmo = reader.ReadInt16(),
			NewAmmo = reader.ReadInt16()
		};
	}

	private DemSecondaryAmmoEvent ReadSecondaryAmmoEvent(BinaryReader reader)
	{
		return new DemSecondaryAmmoEvent
		{
			OldAmmo = reader.ReadInt16(),
			NewAmmo = reader.ReadInt16()
		};
	}

	private DemDoorOpeningEvent ReadDoorOpeningEvent(BinaryReader reader)
	{
		return new DemDoorOpeningEvent
		{
			SegNum = reader.ReadInt16(),
			Side = reader.ReadByte()
		};
	}

	private DemLaserLevelEvent ReadLaserLevelEvent(BinaryReader reader)
	{
		return new DemLaserLevelEvent
		{
			OldLevel = reader.ReadInt16(),
			NewLevel = reader.ReadInt16()
		};
	}

	private DemPlayerAfterburnerEvent ReadPlayerAfterburnerEvent(BinaryReader reader)
	{
		return new DemPlayerAfterburnerEvent
		{
			OldAfterburner = reader.ReadInt16(),
			Afterburner = reader.ReadInt16()
		};
	}

	private DemCloakingWallEvent ReadCloakingWallEvent(BinaryReader reader)
	{
		return new DemCloakingWallEvent
		{
			FrontWallNum = reader.ReadByte(),
			BackWallNum = reader.ReadByte(),
			Type = reader.ReadByte(),
			State = reader.ReadByte(),
			CloakValue = reader.ReadByte(),
			L0 = reader.ReadInt16(),
			L1 = reader.ReadInt16(),
			L2 = reader.ReadInt16(),
			L3 = reader.ReadInt16()
		};
	}

	private DemChangeCockpitEvent ReadChangeCockpitEvent(BinaryReader reader)
	{
		return new DemChangeCockpitEvent { Cockpit = reader.ReadInt32() };
	}

	private DemSecretThingyEvent ReadSecretThingyEvent(BinaryReader reader)
	{
		return new DemSecretThingyEvent { Truth = reader.ReadInt32() };
	}

	private DemLinkSoundToObjectEvent ReadLinkSoundToObjectEvent(BinaryReader reader)
	{
		return new DemLinkSoundToObjectEvent
		{
			SoundNo = reader.ReadInt32(),
			Signature = reader.ReadInt32(),
			MaxVolume = reader.ReadInt32(),
			MaxDistance = reader.ReadInt32(),
			LoopStart = reader.ReadInt32(),
			LoopEnd = reader.ReadInt32()
		};
	}

	private DemKillSoundToObjectEvent ReadKillSoundToObjectEvent(BinaryReader reader)
	{
		return new DemKillSoundToObjectEvent { Signature = reader.ReadInt32() };
	}
}