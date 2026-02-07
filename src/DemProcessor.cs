using ii.Ascend.Model;

namespace ii.Ascend;

public class DemProcessor
{
	// Game mode flags
	private const int GM_MULTI = 38;
	private const int GM_MULTI_COOP = 16;
	private const int GM_TEAM = 256;

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

	// Game data lookups
	private int[] _robotModelNums = [];
	private bool[] _robotIsBoss = [];
	private int[] _polyModelNumSubmodels = [];

	public DemFile Read(string filename, int[] robotModelNums, bool[] robotIsBoss, int[] polyModelNumSubmodels)
	{
		var fileData = File.ReadAllBytes(filename);
		return Read(fileData, robotModelNums, robotIsBoss, polyModelNumSubmodels);
	}

	public DemFile Read(byte[] fileData, int[] robotModelNums, bool[] robotIsBoss, int[] polyModelNumSubmodels)
	{
		using var stream = new MemoryStream(fileData);
		using var reader = new BinaryReader(stream);

		_robotModelNums = robotModelNums;
		_robotIsBoss = robotIsBoss;
		_polyModelNumSubmodels = polyModelNumSubmodels;

		var demFile = new DemFile();
		_justStartedPlayback = false;

		try
		{
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

					if (eventType == DemEventTypes.NewLevel && _gameType == 3)
					{
						_justStartedPlayback = false;
					}

					if (eventType == DemEventTypes.Eof)
					{
						// Unused data?
						var remaining = (int)(stream.Length - stream.Position);
						if (remaining > 0)
						{
							demFile.TrailingData = reader.ReadBytes(remaining);
						}
						break;
					}
				}
			}
		}
		catch (EndOfStreamException)
		{
			// Nothing :(
		}

		return demFile;
	}

	public void Write(string filename, DemFile demFile, int[] robotModelNums, bool[] robotIsBoss, int[] polyModelNumSubmodels)
	{
		var fileData = Write(demFile, robotModelNums, robotIsBoss, polyModelNumSubmodels);
		File.WriteAllBytes(filename, fileData);
	}

	public byte[] Write(DemFile demFile, int[] robotModelNums, bool[] robotIsBoss, int[] polyModelNumSubmodels)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);

		_robotModelNums = robotModelNums;
		_robotIsBoss = robotIsBoss;
		_polyModelNumSubmodels = polyModelNumSubmodels;

		_justStartedPlayback = false;
		_gameType = demFile.GameType;
		_version = demFile.Version;

		foreach (var evt in demFile.Events)
		{
			writer.Write(evt.EventType);
			WriteEvent(writer, evt);

			if (evt is DemStartDemoEvent startDemo)
			{
				_version = startDemo.Version;
				_gameType = startDemo.GameType;
				_justStartedPlayback = true;
			}

			if (evt.EventType == DemEventTypes.NewLevel && _gameType == 3)
			{
				_justStartedPlayback = false;
			}
		}

		// Write any trailing data
		if (demFile.TrailingData.Length > 0)
		{
			writer.Write(demFile.TrailingData);
		}

		return stream.ToArray();
	}

	private int GetModelNumForObject(DemObject obj)
	{
		if (obj.Type == OBJ_ROBOT && obj.Id < _robotModelNums.Length)
		{
			return _robotModelNums[obj.Id];
		}

		if (obj.Type == OBJ_PLAYER)
		{
			return 0;
		}

		if (obj.Type == OBJ_CLUTTER)
		{
			return obj.Id;
		}

		return obj.ModelNum ?? 0;
	}

	private int GetNumSubmodels(int modelNum)
	{
		if (modelNum >= 0 && modelNum < _polyModelNumSubmodels.Length)
		{
			return _polyModelNumSubmodels[modelNum];
		}
		return 0;
	}

	private bool IsRobotBoss(byte robotId)
	{
		if (robotId < _robotIsBoss.Length)
		{
			return _robotIsBoss[robotId];
		}
		return false;
	}

	private void WriteEvent(BinaryWriter writer, IDemEvent evt)
	{
		switch (evt)
		{
			case DemEofEvent:
				break;
			case DemStartDemoEvent e:
				WriteStartDemoEvent(writer, e);
				break;
			case DemStartFrameEvent e:
				WriteStartFrameEvent(writer, e);
				break;
			case DemViewerObjectEvent e:
				WriteViewerObjectEvent(writer, e);
				break;
			case DemRenderObjectEvent e:
				WriteRenderObjectEvent(writer, e);
				break;
			case DemSoundEvent e:
				writer.Write(e.SoundNo);
				break;
			case DemSoundOnceEvent e:
				writer.Write(e.SoundNo);
				break;
			case DemSound3DEvent e:
				writer.Write(e.SoundNo);
				writer.Write(e.Angle);
				writer.Write(e.Volume);
				break;
			case DemWallHitProcessEvent e:
				writer.Write(e.SegNum);
				writer.Write(e.Side);
				writer.Write(e.Damage);
				writer.Write(e.Player);
				break;
			case DemTriggerEvent e:
				WriteTriggerEvent(writer, e);
				break;
			case DemHostageRescuedEvent e:
				writer.Write(e.HostageNumber);
				break;
			case DemSound3DOnceEvent e:
				writer.Write(e.SoundNo);
				writer.Write(e.Angle);
				writer.Write(e.Volume);
				break;
			case DemMorphFrameEvent e:
				WriteObject(writer, e.Object);
				break;
			case DemWallToggleEvent e:
				writer.Write(e.SegNum);
				writer.Write(e.Side);
				break;
			case DemHudMessageEvent e:
				WriteLengthPrefixedString(writer, e.Message);
				break;
			case DemControlCenterDestroyedEvent e:
				writer.Write(e.CountdownSecondsLeft);
				break;
			case DemPaletteEffectEvent e:
				writer.Write(e.Red);
				writer.Write(e.Green);
				writer.Write(e.Blue);
				break;
			case DemPlayerEnergyEvent e:
				WritePlayerEnergyEvent(writer, e);
				break;
			case DemPlayerShieldEvent e:
				WritePlayerShieldEvent(writer, e);
				break;
			case DemPlayerFlagsEvent e:
				writer.Write(e.OldFlags);
				writer.Write(e.Flags);
				break;
			case DemPlayerWeaponEvent e:
				WritePlayerWeaponEvent(writer, e);
				break;
			case DemEffectBlowupEvent e:
				writer.Write(e.SegNum);
				writer.Write(e.Side);
				WriteVector(writer, e.Point);
				break;
			case DemHomingDistanceEvent e:
				writer.Write(e.Distance);
				break;
			case DemLetterboxEvent:
				break;
			case DemRestoreCockpitEvent:
				break;
			case DemRearviewEvent:
				break;
			case DemWallSetTmapNum1Event e:
				writer.Write(e.Seg);
				writer.Write(e.Side);
				writer.Write(e.CSeg);
				writer.Write(e.CSide);
				writer.Write(e.Tmap);
				break;
			case DemWallSetTmapNum2Event e:
				writer.Write(e.Seg);
				writer.Write(e.Side);
				writer.Write(e.CSeg);
				writer.Write(e.CSide);
				writer.Write(e.Tmap);
				break;
			case DemNewLevelEvent e:
				WriteNewLevelEvent(writer, e);
				break;
			case DemMultiCloakEvent e:
				writer.Write(e.PlayerNum);
				break;
			case DemMultiDecloakEvent e:
				writer.Write(e.PlayerNum);
				break;
			case DemRestoreRearviewEvent:
				break;
			case DemMultiDeathEvent e:
				writer.Write(e.PlayerNum);
				break;
			case DemMultiKillEvent e:
				writer.Write(e.PlayerNum);
				writer.Write(e.Kills);
				break;
			case DemMultiConnectEvent e:
				WriteMultiConnectEvent(writer, e);
				break;
			case DemMultiReconnectEvent e:
				writer.Write(e.PlayerNum);
				break;
			case DemMultiDisconnectEvent e:
				writer.Write(e.PlayerNum);
				break;
			case DemMultiScoreEvent e:
				writer.Write(e.PlayerNum);
				writer.Write(e.Score);
				break;
			case DemPlayerScoreEvent e:
				writer.Write(e.Score);
				break;
			case DemPrimaryAmmoEvent e:
				writer.Write(e.OldAmmo);
				writer.Write(e.NewAmmo);
				break;
			case DemSecondaryAmmoEvent e:
				writer.Write(e.OldAmmo);
				writer.Write(e.NewAmmo);
				break;
			case DemDoorOpeningEvent e:
				writer.Write(e.SegNum);
				writer.Write(e.Side);
				break;
			case DemLaserLevelEvent e:
				if (_gameType == 3) // D2 uses shorts
				{
					writer.Write(e.OldLevel);
					writer.Write(e.NewLevel);
				}
				else // D1 uses bytes
				{
					writer.Write((byte)e.OldLevel);
					writer.Write((byte)e.NewLevel);
				}
				break;
			case DemPlayerAfterburnerEvent e:
				writer.Write(e.OldAfterburner);
				writer.Write(e.Afterburner);
				break;
			case DemCloakingWallEvent e:
				writer.Write(e.FrontWallNum);
				writer.Write(e.BackWallNum);
				writer.Write(e.Type);
				writer.Write(e.State);
				writer.Write(e.CloakValue);
				writer.Write(e.L0);
				writer.Write(e.L1);
				writer.Write(e.L2);
				writer.Write(e.L3);
				break;
			case DemChangeCockpitEvent e:
				writer.Write(e.Cockpit);
				break;
			case DemStartGuidedEvent:
				break;
			case DemEndGuidedEvent:
				break;
			case DemSecretThingyEvent e:
				writer.Write(e.Truth);
				break;
			case DemLinkSoundToObjectEvent e:
				writer.Write(e.SoundNo);
				writer.Write(e.Signature);
				writer.Write(e.MaxVolume);
				writer.Write(e.MaxDistance);
				writer.Write(e.LoopStart);
				writer.Write(e.LoopEnd);
				break;
			case DemKillSoundToObjectEvent e:
				writer.Write(e.Signature);
				break;
		}
	}

	private void WriteStartDemoEvent(BinaryWriter writer, DemStartDemoEvent evt)
	{
		writer.Write(evt.Version);
		writer.Write(evt.GameType);
		writer.Write(evt.GameTime);
		writer.Write(evt.GameMode);

		if (evt.GameType == 1)
		{
			if ((evt.GameMode & GM_MULTI) != 0)
			{
				writer.Write(evt.TeamVector ?? 0);
			}
		}
		else
		{
			if ((evt.GameMode & GM_TEAM) != 0)
			{
				writer.Write(evt.TeamVector ?? 0);
				WriteLengthPrefixedString(writer, evt.TeamName0 ?? string.Empty);
				WriteLengthPrefixedString(writer, evt.TeamName1 ?? string.Empty);
			}

			if ((evt.GameMode & GM_MULTI) != 0)
			{
				writer.Write(evt.NumPlayers ?? 0);
				if (evt.Players != null)
				{
					foreach (var player in evt.Players)
					{
						WriteLengthPrefixedString(writer, player.Callsign);
						writer.Write(player.Connected);

						if ((evt.GameMode & GM_MULTI_COOP) != 0)
						{
							writer.Write(player.Score ?? 0);
						}
						else
						{
							writer.Write(player.NetKilledTotal ?? 0);
							writer.Write(player.NetKillsTotal ?? 0);
						}
					}
				}
			}
			else
			{
				writer.Write(evt.PlayerScore ?? 0);
			}

			for (int i = 0; i < MAX_PRIMARY_WEAPONS; i++)
			{
				writer.Write(evt.PrimaryAmmo != null && i < evt.PrimaryAmmo.Length ? evt.PrimaryAmmo[i] : (short)0);
			}

			for (int i = 0; i < MAX_SECONDARY_WEAPONS; i++)
			{
				writer.Write(evt.SecondaryAmmo != null && i < evt.SecondaryAmmo.Length ? evt.SecondaryAmmo[i] : (short)0);
			}

			writer.Write(evt.LaserLevel);
			WriteLengthPrefixedString(writer, evt.CurrentMission);
		}

		writer.Write(evt.Energy);
		writer.Write(evt.Shield);
		writer.Write(evt.Flags);
		writer.Write(evt.PrimaryWeapon);
		writer.Write(evt.SecondaryWeapon);
	}

	private void WriteStartFrameEvent(BinaryWriter writer, DemStartFrameEvent evt)
	{
		writer.Write(evt.LastFrameLength);
		writer.Write(evt.FrameCount);
		writer.Write(evt.RecordedTime);
	}

	private void WriteViewerObjectEvent(BinaryWriter writer, DemViewerObjectEvent evt)
	{
		if (_gameType == 3)
		{
			writer.Write(evt.WhichWindow ?? 0);
		}

		WriteObject(writer, evt.Object);
	}

	private void WriteRenderObjectEvent(BinaryWriter writer, DemRenderObjectEvent evt)
	{
		WriteObject(writer, evt.Object);
	}

	private void WriteObject(BinaryWriter writer, DemObject obj)
	{
		writer.Write(obj.RenderType);
		writer.Write(obj.Type);

		if (obj.RenderType == RT_NONE && obj.Type != OBJ_CAMERA)
		{
			return;
		}

		writer.Write(obj.Id);
		writer.Write(obj.Flags);
		writer.Write(obj.Signature);
		WriteShortPos(writer, obj);

		switch (obj.Type)
		{
			case OBJ_HOSTAGE:
				break;
			case OBJ_ROBOT:
				break;
			case OBJ_POWERUP:
				writer.Write(obj.MovementType);
				break;
			case OBJ_PLAYER:
				break;
			case OBJ_CLUTTER:
				break;
			default:
				writer.Write(obj.ControlType);
				writer.Write(obj.MovementType);
				break;
		}

		if (obj.Type != OBJ_ROBOT && obj.Type != OBJ_HOSTAGE &&
			obj.Type != OBJ_PLAYER && obj.Type != OBJ_POWERUP && obj.Type != OBJ_CLUTTER)
		{
			writer.Write(obj.Size ?? 0);
		}

		WriteVector(writer, obj.LastPos);

		if (obj.Type == OBJ_WEAPON && obj.RenderType == RT_WEAPON_VCLIP)
		{
			writer.Write(obj.Lifeleft ?? 0);
		}
		else
		{
			writer.Write(obj.LifeleftByte ?? (byte)0);
		}

		if (_gameType >= 2 && obj.Type == OBJ_ROBOT && IsRobotBoss(obj.Id))
		{
			writer.Write((byte)(obj.Cloaked == true ? 1 : 0));
		}

		switch (obj.MovementType)
		{
			case MT_PHYSICS:
				WriteVector(writer, obj.Velocity ?? new VmsVector());
				WriteVector(writer, obj.Thrust ?? new VmsVector());
				break;
			case MT_SPINNING:
				WriteVector(writer, obj.SpinRate ?? new VmsVector());
				break;
		}

		switch (obj.ControlType)
		{
			case CT_EXPLOSION:
				writer.Write(obj.SpawnTime ?? 0);
				writer.Write(obj.DeleteTime ?? 0);
				writer.Write(obj.DeleteObjNum ?? (short)0);
				break;
			case CT_LIGHT:
				writer.Write(obj.LightIntensity ?? 0);
				break;
		}

		switch (obj.RenderType)
		{
			case RT_MORPH:
			case RT_POLYOBJ:
				if (obj.Type != OBJ_ROBOT && obj.Type != OBJ_PLAYER && obj.Type != OBJ_CLUTTER)
				{
					writer.Write(obj.ModelNum ?? 0);
					writer.Write(obj.SubobjFlags ?? 0);
				}

				if (obj.Type != OBJ_PLAYER && obj.Type != OBJ_DEBRIS)
				{
					if (obj.AnimAngles != null)
					{
						for (int i = 0; i < obj.AnimAngles.Length; i++)
						{
							WriteAngVec(writer, obj.AnimAngles[i]);
						}
					}
				}

				writer.Write(obj.Tmo ?? 0);
				break;

			case RT_POWERUP:
			case RT_WEAPON_VCLIP:
			case RT_FIREBALL:
			case RT_HOSTAGE:
				writer.Write(obj.VClipNum ?? 0);
				writer.Write(obj.FrameTime ?? 0);
				writer.Write(obj.FrameNum ?? (byte)0);
				break;
		}
	}

	private void WriteTriggerEvent(BinaryWriter writer, DemTriggerEvent evt)
	{
		writer.Write(evt.SegNum);
		writer.Write(evt.Side);
		writer.Write(evt.ObjNum);
		if (_gameType == 3)
		{
			writer.Write(evt.Shot ?? 0);
		}
	}

	private void WritePlayerEnergyEvent(BinaryWriter writer, DemPlayerEnergyEvent evt)
	{
		if (_gameType != 1)
		{
			writer.Write(evt.OldEnergy ?? 0);
		}
		writer.Write(evt.Energy);
	}

	private void WritePlayerShieldEvent(BinaryWriter writer, DemPlayerShieldEvent evt)
	{
		if (_gameType != 1)
		{
			writer.Write(evt.OldShield ?? 0);
		}
		writer.Write(evt.Shield);
	}

	private void WritePlayerWeaponEvent(BinaryWriter writer, DemPlayerWeaponEvent evt)
	{
		writer.Write(evt.WeaponType);
		writer.Write(evt.WeaponNum);
		if (_gameType != 1)
		{
			writer.Write(evt.OldWeapon ?? 0);
		}
	}

	private void WriteNewLevelEvent(BinaryWriter writer, DemNewLevelEvent evt)
	{
		writer.Write(evt.NewLevel);
		writer.Write(evt.OldLevel);

		if (_gameType == 3 && _justStartedPlayback)
		{
			if (evt.Walls != null)
			{
				writer.Write(evt.Walls.Count);
				foreach (var wall in evt.Walls)
				{
					writer.Write(wall.Type);
					writer.Write(wall.Flags);
					writer.Write(wall.State);
					writer.Write(wall.TmapNum1);
					writer.Write(wall.TmapNum2);
				}
			}
			else
			{
				writer.Write(0);
			}
		}
	}

	private void WriteMultiConnectEvent(BinaryWriter writer, DemMultiConnectEvent evt)
	{
		writer.Write(evt.PlayerNum);
		writer.Write(evt.NewPlayer);

		if (evt.NewPlayer == 0)
		{
			WriteLengthPrefixedString(writer, evt.OldCallsign ?? string.Empty);
			writer.Write(evt.KilledTotal ?? 0);
			writer.Write(evt.KillsTotal ?? 0);
		}

		WriteLengthPrefixedString(writer, evt.NewCallsign);
	}

	private void WriteShortPos(BinaryWriter writer, DemObject obj)
	{
		var pos = obj.Position;

		if (ShortPosHasByteMat(obj.RenderType, obj.Type))
		{
			if (pos.ByteMat != null && pos.ByteMat.Length == 9)
			{
				writer.Write(pos.ByteMat);
			}
			else
			{
				writer.Write(new byte[9]);
			}
		}

		writer.Write(pos.X);
		writer.Write(pos.Y);
		writer.Write(pos.Z);
		writer.Write(pos.Segment);
		writer.Write(pos.VelX);
		writer.Write(pos.VelY);
		writer.Write(pos.VelZ);
	}

	private void WriteVector(BinaryWriter writer, VmsVector vec)
	{
		writer.Write(vec.X);
		writer.Write(vec.Y);
		writer.Write(vec.Z);
	}

	private void WriteAngVec(BinaryWriter writer, VmsAngvec ang)
	{
		writer.Write(ang.P);
		writer.Write(ang.B);
		writer.Write(ang.H);
	}

	private void WriteLengthPrefixedString(BinaryWriter writer, string str)
	{
		var bytes = System.Text.Encoding.ASCII.GetBytes(str + '\0');
		writer.Write((byte)bytes.Length);
		writer.Write(bytes);
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
				evt.TeamName0 = ReadLengthPrefixedString(reader);
				evt.TeamName1 = ReadLengthPrefixedString(reader);
			}

			if ((evt.GameMode & GM_MULTI) != 0)
			{
				evt.NumPlayers = reader.ReadByte();
				evt.Players = new List<DemPlayerInfo>();

				for (int i = 0; i < evt.NumPlayers; i++)
				{
					var player = new DemPlayerInfo
					{
						Callsign = ReadLengthPrefixedString(reader),
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
			evt.CurrentMission = ReadLengthPrefixedString(reader);
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

		if (obj.RenderType == RT_NONE && obj.Type != OBJ_CAMERA)
		{
			return obj;
		}

		obj.Id = reader.ReadByte();
		obj.Flags = reader.ReadByte();
		obj.Signature = reader.ReadInt16();
		obj.Position = ReadShortPos(reader, obj.RenderType, obj.Type);

		DetermineObjectTypes(obj, reader, out var controlType, out var movementType);
		obj.ControlType = controlType;
		obj.MovementType = movementType;

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
			obj.LifeleftByte = reader.ReadByte();
		}

		// Only read cloaked byte for boss robots (D1 Full and D2)
		if (_gameType >= 2 && obj.Type == OBJ_ROBOT && IsRobotBoss(obj.Id))
		{
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
					var modelNum = GetModelNumForObject(obj);
					var numAngles = GetNumSubmodels(modelNum);
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

	private static bool ShortPosHasByteMat(byte renderType, byte objectType)
	{
		return renderType == RT_POLYOBJ || renderType == RT_HOSTAGE || renderType == RT_MORPH || objectType == OBJ_CAMERA;
	}

	private DemShortPos ReadShortPos(BinaryReader reader, byte renderType, byte objectType)
	{
		var sp = new DemShortPos();

		if (ShortPosHasByteMat(renderType, objectType))
		{
			sp.ByteMat = reader.ReadBytes(9);
		}

		sp.X = reader.ReadInt16();
		sp.Y = reader.ReadInt16();
		sp.Z = reader.ReadInt16();
		sp.Segment = reader.ReadInt16();
		sp.VelX = reader.ReadInt16();
		sp.VelY = reader.ReadInt16();
		sp.VelZ = reader.ReadInt16();

		return sp;
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

	private string ReadLengthPrefixedString(BinaryReader reader)
	{
		var len = reader.ReadByte();
		var bytes = reader.ReadBytes(len);
		// The string includes a null terminator; strip it
		var str = System.Text.Encoding.ASCII.GetString(bytes);
		return str.TrimEnd('\0');
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
		return new DemHudMessageEvent { Message = ReadLengthPrefixedString(reader) };
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
			evt.OldCallsign = ReadLengthPrefixedString(reader);
			evt.KilledTotal = reader.ReadInt32();
			evt.KillsTotal = reader.ReadInt32();
		}

		evt.NewCallsign = ReadLengthPrefixedString(reader);
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
		if (_gameType == 3) // D2 uses shorts
		{
			return new DemLaserLevelEvent
			{
				OldLevel = reader.ReadInt16(),
				NewLevel = reader.ReadInt16()
			};
		}
		else // D1 uses bytes
		{
			return new DemLaserLevelEvent
			{
				OldLevel = reader.ReadByte(),
				NewLevel = reader.ReadByte()
			};
		}
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