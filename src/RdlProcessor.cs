using ii.Ascend.Model;

namespace ii.Ascend;

public class RdlProcessor
{
	private const int LevelFileSignature = 0x504C564C; // PLVL
	private const int GameFileSignature = 0x6705;
	private const int CompiledMineVersion = 0;
	private const int GameVersion = 25;
	private const int GameCompatibleVersion = 22;
	private const int HostageDataVersion = 0;

	public RdlFile Read(string filename)
	{
		var fileData = File.ReadAllBytes(filename);
		return Read(fileData);
	}

	public RdlFile Read(byte[] fileData)
	{
		using var stream = new MemoryStream(fileData);
		using var reader = new BinaryReader(stream);

		var signature = reader.ReadInt32();
		if (signature != LevelFileSignature)
		{
			throw new InvalidDataException(
				$"Invalid RDL file. Expected signature 'PLVL' (0x{LevelFileSignature:X8}), got 0x{signature:X8}.");
		}

		var version = reader.ReadInt32();
		var mineDataOffset = reader.ReadInt32();
		var gameDataOffset = reader.ReadInt32();
		var hostageTextOffset = reader.ReadInt32();

		var rdl = new RdlFile();

		stream.Position = mineDataOffset;
		ReadMineData(reader, rdl);

		stream.Position = gameDataOffset;
		ReadGameData(reader, rdl, version, hostageTextOffset, fileData.Length);

		return rdl;
	}

	public void Write(string filename, RdlFile rdl)
	{
		var fileData = Write(rdl);
		File.WriteAllBytes(filename, fileData);
	}

	public byte[] Write(RdlFile rdl)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);

		writer.Write(LevelFileSignature);
		writer.Write(1); // version
		var mineOffsetPos = stream.Position;
		writer.Write(0); // mine data offset placeholder
		var gameOffsetPos = stream.Position;
		writer.Write(0); // game data offset placeholder
		var hostageOffsetPos = stream.Position;
		writer.Write(0); // hostage text offset placeholder

		var mineDataOffset = (int)stream.Position;
		WriteMineData(writer, rdl);

		var gameDataOffset = (int)stream.Position;
		WriteGameData(writer, rdl);

		var hostageTextOffset = (int)stream.Position;
		WriteHostageData(writer, rdl);

		stream.Position = mineOffsetPos;
		writer.Write(mineDataOffset);
		stream.Position = gameOffsetPos;
		writer.Write(gameDataOffset);
		stream.Position = hostageOffsetPos;
		writer.Write(hostageTextOffset);

		return stream.ToArray();
	}

	private void ReadMineData(BinaryReader reader, RdlFile rdl)
	{
		var version = reader.ReadByte();
		if (version != CompiledMineVersion)
		{
			throw new InvalidDataException(
				$"Unsupported compiled mine version. Expected {CompiledMineVersion}, got {version}.");
		}

		var numVertices = reader.ReadUInt16();
		var numSegments = reader.ReadUInt16();

		for (int i = 0; i < numVertices; i++)
		{
			rdl.Vertices.Add(ReadVector(reader));
		}

		for (int i = 0; i < numSegments; i++)
		{
			rdl.Segments.Add(ReadSegment(reader));
		}
	}

	private RdlSegment ReadSegment(BinaryReader reader)
	{
		var segment = new RdlSegment();

		var childMask = reader.ReadByte();
		for (int i = 0; i < RdlSegment.MaxSidesPerSegment; i++)
		{
			if ((childMask & (1 << i)) != 0)
			{
				segment.Children[i] = reader.ReadInt16();
			}
			else
			{
				segment.Children[i] = -1;
			}
		}

		for (int i = 0; i < RdlSegment.MaxVerticesPerSegment; i++)
		{
			segment.Verts[i] = reader.ReadInt16();
		}

		if ((childMask & (1 << RdlSegment.MaxSidesPerSegment)) != 0)
		{
			segment.Special = reader.ReadByte();
			segment.MatcenNum = reader.ReadSByte();
			segment.Value = reader.ReadInt16();
		}

		var staticLightShort = reader.ReadUInt16();
		segment.StaticLight = staticLightShort << 4;

		// Read wall bitmask and wall numbers
		// Note: If bit is set but value is 255, we store 255 (not -1) to preserve round-trip.
		// The original game converts 255 to -1 at load time, but we need to write 255 back.
		var wallMask = reader.ReadByte();
		for (int i = 0; i < RdlSegment.MaxSidesPerSegment; i++)
		{
			if ((wallMask & (1 << i)) != 0)
			{
				segment.Sides[i].WallNum = reader.ReadByte();
			}
			else
			{
				segment.Sides[i].WallNum = -1;
			}
		}

		for (int i = 0; i < RdlSegment.MaxSidesPerSegment; i++)
		{
			if (segment.Children[i] == -1 || segment.Sides[i].WallNum != -1)
			{
				var tmapNumWithFlag = reader.ReadUInt16();
				segment.Sides[i].TmapNum = (short)(tmapNumWithFlag & 0x7FFF);

				if ((tmapNumWithFlag & 0x8000) != 0)
				{
					segment.Sides[i].TmapNum2 = reader.ReadInt16();
				}

				for (int j = 0; j < RdlSide.MaxVerticesPerSide; j++)
				{
					var u = reader.ReadInt16() << 5;
					var v = reader.ReadInt16() << 5;
					var l = reader.ReadUInt16() << 1;
					segment.Sides[i].Uvls[j] = new RdlUvl(u, v, l);
				}
			}
		}

		return segment;
	}

	private void ReadGameData(BinaryReader reader, RdlFile rdl, int levelVersion, int hostageTextOffset, int fileLength)
	{
		var startOffset = reader.BaseStream.Position;

		var signature = reader.ReadUInt16();
		if (signature != GameFileSignature)
		{
			throw new InvalidDataException(
				$"Invalid game data signature. Expected 0x{GameFileSignature:X4}, got 0x{signature:X4}.");
		}

		var fileInfoVersion = reader.ReadUInt16();
		if (fileInfoVersion < GameCompatibleVersion)
		{
			throw new InvalidDataException(
				$"Unsupported game data version. Expected >= {GameCompatibleVersion}, got {fileInfoVersion}.");
		}

		var fileInfoSize = reader.ReadInt32();

		// Skip mine filename (15 bytes)
		reader.ReadBytes(15);

		var level = reader.ReadInt32();
		var playerOffset = reader.ReadInt32();
		var playerSize = reader.ReadInt32();
		var objectOffset = reader.ReadInt32();
		var objectCount = reader.ReadInt32();
		var objectSize = reader.ReadInt32();
		var wallsOffset = reader.ReadInt32();
		var wallsCount = reader.ReadInt32();
		var wallsSize = reader.ReadInt32();
		var doorsOffset = reader.ReadInt32();
		var doorsCount = reader.ReadInt32();
		var doorsSize = reader.ReadInt32();
		var triggersOffset = reader.ReadInt32();
		var triggersCount = reader.ReadInt32();
		var triggersSize = reader.ReadInt32();
		var linksOffset = reader.ReadInt32();
		var linksCount = reader.ReadInt32();
		var linksSize = reader.ReadInt32();
		var controlOffset = reader.ReadInt32();
		var controlCount = reader.ReadInt32();
		var controlSize = reader.ReadInt32();
		var matcenOffset = reader.ReadInt32();
		var matcenCount = reader.ReadInt32();
		var matcenSize = reader.ReadInt32();

		if (fileInfoVersion >= 14)
		{
			var nameChars = new List<char>();
			char c;
			while ((c = (char)reader.ReadByte()) != '\0')
			{
				nameChars.Add(c);
			}
			rdl.LevelName = new string(nameChars.ToArray());
		}

		// Read POF names
		// Note: Original game seems to have a bug where save writes N_polygon_models POF names
		// but only stores N_save_pof_names in the count field. We need to calculate the actual
		// count based on bytes available before player_offset.
		if (fileInfoVersion >= 19)
		{
			var storedPofCount = reader.ReadInt16();
			var pofStartPos = reader.BaseStream.Position;

			int actualPofCount;
			if (playerOffset > pofStartPos)
			{
				var availableBytes = playerOffset - pofStartPos;
				actualPofCount = (int)(availableBytes / 13);
			}
			else
			{
				actualPofCount = storedPofCount;
			}

			for (int i = 0; i < actualPofCount; i++)
			{
				var pofNameBytes = reader.ReadBytes(13);
				var pofName = System.Text.Encoding.ASCII.GetString(pofNameBytes).TrimEnd('\0');
				rdl.PofNames.Add(pofName);
			}
		}

		if (playerOffset > -1 && playerSize > 0)
		{
			reader.BaseStream.Position = playerOffset;
			rdl.PlayerData = reader.ReadBytes(playerSize);
		}

		if (objectOffset > -1 && objectCount > 0)
		{
			reader.BaseStream.Position = objectOffset;
			for (int i = 0; i < objectCount; i++)
			{
				rdl.Objects.Add(ReadObject(reader, fileInfoVersion));
			}
		}

		if (wallsOffset > -1 && wallsCount > 0)
		{
			reader.BaseStream.Position = wallsOffset;
			for (int i = 0; i < wallsCount; i++)
			{
				rdl.Walls.Add(ReadWall(reader, fileInfoVersion));
			}
		}

		if (doorsOffset > -1 && doorsCount > 0)
		{
			reader.BaseStream.Position = doorsOffset;
			for (int i = 0; i < doorsCount; i++)
			{
				rdl.ActiveDoors.Add(ReadActiveDoor(reader, fileInfoVersion));
			}
		}

		if (triggersOffset > -1 && triggersCount > 0)
		{
			reader.BaseStream.Position = triggersOffset;
			for (int i = 0; i < triggersCount; i++)
			{
				rdl.Triggers.Add(ReadTrigger(reader));
			}
		}

		if (controlOffset > -1)
		{
			reader.BaseStream.Position = controlOffset;
			rdl.ControlCenterTrigger = ReadControlCenterTrigger(reader);
		}

		if (matcenOffset > -1 && matcenCount > 0)
		{
			reader.BaseStream.Position = matcenOffset;
			for (int i = 0; i < matcenCount; i++)
			{
				rdl.MatCens.Add(ReadMatcen(reader));
			}
		}

		if (hostageTextOffset > 0 && hostageTextOffset < fileLength)
		{
			reader.BaseStream.Position = hostageTextOffset;
			ReadHostageData(reader, rdl, fileLength - hostageTextOffset);
		}
	}

	private void ReadHostageData(BinaryReader reader, RdlFile rdl, int maxLength)
	{
		if (maxLength < 4)
			return;

		var startPos = reader.BaseStream.Position;

		int numHostages = 0;
		foreach (var obj in rdl.Objects)
		{
			if (obj.Type == RdlObject.TypeHostage)
			{
				if (obj.Id + 1 > numHostages)
					numHostages = obj.Id + 1;
			}
		}

		if (numHostages == 0)
			return;

		var version = reader.ReadInt32();

		for (int i = 0; i < numHostages; i++)
		{
			if (reader.BaseStream.Position - startPos >= maxLength - 4)
				break;

			var hostage = new RdlHostage
			{
				VClipNum = reader.ReadInt32()
			};

			var textChars = new List<char>();
			while (reader.BaseStream.Position - startPos < maxLength)
			{
				var c = (char)reader.ReadByte();
				if (c == '\n' || c == '\0')
					break;
				textChars.Add(c);
			}
			hostage.Text = new string(textChars.ToArray());

			rdl.Hostages.Add(hostage);
		}
	}

	private RdlObject ReadObject(BinaryReader reader, int version)
	{
		var obj = new RdlObject
		{
			Type = reader.ReadByte(),
			Id = reader.ReadByte(),
			ControlType = reader.ReadByte(),
			MovementType = reader.ReadByte(),
			RenderType = reader.ReadByte(),
			Flags = reader.ReadByte(),
			SegNum = reader.ReadInt16()
		};

		obj.Position = ReadVector(reader);
		obj.Orientation = ReadMatrix(reader);
		obj.Size = reader.ReadInt32();
		obj.Shields = reader.ReadInt32();
		obj.LastPos = ReadVector(reader);
		obj.ContainsType = reader.ReadSByte();
		obj.ContainsId = reader.ReadSByte();
		obj.ContainsCount = reader.ReadSByte();

		switch (obj.MovementType)
		{
			case RdlObject.MovePhysics:
				obj.PhysicsInfo = new RdlPhysicsInfo
				{
					Velocity = ReadVector(reader),
					Thrust = ReadVector(reader),
					Mass = reader.ReadInt32(),
					Drag = reader.ReadInt32(),
					Brakes = reader.ReadInt32(),
					RotVel = ReadVector(reader),
					RotThrust = ReadVector(reader),
					TurnRoll = reader.ReadInt16(),
					Flags = reader.ReadUInt16()
				};
				break;
			case RdlObject.MoveSpinning:
				obj.SpinRate = ReadVector(reader);
				break;
		}

		switch (obj.ControlType)
		{
			case RdlObject.CtrlAi:
				obj.AiInfo = new RdlAiInfo
				{
					Behavior = reader.ReadByte()
				};
				for (int i = 0; i < RdlAiInfo.MaxAiFlags; i++)
				{
					obj.AiInfo.Flags[i] = reader.ReadByte();
				}
				obj.AiInfo.HideSegment = reader.ReadInt16();
				obj.AiInfo.HideIndex = reader.ReadInt16();
				obj.AiInfo.PathLength = reader.ReadInt16();
				obj.AiInfo.CurPathIndex = reader.ReadInt16();
				obj.AiInfo.FollowPathStartSeg = reader.ReadInt16();
				obj.AiInfo.FollowPathEndSeg = reader.ReadInt16();
				break;

			case RdlObject.CtrlExplosion:
				obj.ExplosionInfo = new RdlExplosionInfo
				{
					SpawnTime = reader.ReadInt32(),
					DeleteTime = reader.ReadInt32(),
					DeleteObjNum = reader.ReadInt16()
				};
				break;

			case RdlObject.CtrlWeapon:
				obj.LaserInfo = new RdlLaserInfo
				{
					ParentType = reader.ReadInt16(),
					ParentNum = reader.ReadInt16(),
					ParentSignature = reader.ReadInt32()
				};
				break;

			case RdlObject.CtrlLight:
				obj.LightIntensity = reader.ReadInt32();
				break;

			case RdlObject.CtrlPowerup:
				if (version >= 25)
				{
					obj.PowerupCount = reader.ReadInt32();
				}
				else
				{
					obj.PowerupCount = 1;
				}
				break;
		}

		switch (obj.RenderType)
		{
			case RdlObject.RenderPolyobj:
			case RdlObject.RenderMorph:
				obj.PolyObjInfo = new RdlPolyObjInfo
				{
					ModelNum = reader.ReadInt32()
				};
				for (int i = 0; i < RdlPolyObjInfo.MaxSubmodels; i++)
				{
					obj.PolyObjInfo.AnimAngles[i] = ReadAngvec(reader);
				}
				obj.PolyObjInfo.SubobjFlags = reader.ReadInt32();
				obj.PolyObjInfo.TmapOverride = reader.ReadInt32();
				break;

			case RdlObject.RenderWeaponVclip:
			case RdlObject.RenderHostage:
			case RdlObject.RenderPowerup:
			case RdlObject.RenderFireball:
				obj.VClipInfo = new RdlVClipInfo
				{
					VClipNum = reader.ReadInt32(),
					FrameTime = reader.ReadInt32(),
					FrameNum = reader.ReadByte()
				};
				break;
		}

		return obj;
	}

	private RdlWall ReadWall(BinaryReader reader, int version)
	{
		var wall = new RdlWall();

		if (version >= 20)
		{
			wall.SegNum = reader.ReadInt32();
			wall.SideNum = reader.ReadInt32();
			wall.Hps = reader.ReadInt32();
			wall.LinkedWall = reader.ReadInt32();
			wall.Type = reader.ReadByte();
			wall.Flags = reader.ReadByte();
			wall.State = reader.ReadByte();
			wall.Trigger = reader.ReadSByte();
			wall.ClipNum = reader.ReadSByte();
			wall.Keys = reader.ReadByte();
			reader.ReadInt16(); // pad
		}
		else if (version >= 17)
		{
			wall.SegNum = reader.ReadInt32();
			wall.SideNum = reader.ReadInt32();
			wall.Type = reader.ReadByte();
			wall.Flags = reader.ReadByte();
			wall.Hps = reader.ReadInt32();
			wall.Trigger = reader.ReadSByte();
			wall.ClipNum = reader.ReadSByte();
			wall.Keys = reader.ReadByte();
			wall.LinkedWall = reader.ReadInt32();
			wall.State = RdlWall.StateDoorClosed;
		}
		else
		{
			wall.Type = reader.ReadByte();
			wall.Flags = reader.ReadByte();
			wall.Hps = reader.ReadInt32();
			wall.Trigger = reader.ReadSByte();
			wall.ClipNum = reader.ReadSByte();
			wall.Keys = reader.ReadByte();
			wall.SegNum = -1;
			wall.SideNum = -1;
			wall.LinkedWall = -1;
		}

		return wall;
	}

	private RdlActiveDoor ReadActiveDoor(BinaryReader reader, int version)
	{
		var door = new RdlActiveDoor();

		if (version >= 20)
		{
			door.NumParts = reader.ReadInt32();
			door.FrontWallNum[0] = reader.ReadInt16();
			door.FrontWallNum[1] = reader.ReadInt16();
			door.BackWallNum[0] = reader.ReadInt16();
			door.BackWallNum[1] = reader.ReadInt16();
			door.Time = reader.ReadInt32();
		}
		else
		{
			door.NumParts = reader.ReadInt32();
			var seg0 = reader.ReadInt16();
			var seg1 = reader.ReadInt16();
			var side0 = reader.ReadInt16();
			var side1 = reader.ReadInt16();
			reader.ReadInt16(); // type[0]
			reader.ReadInt16(); // type[1]
			door.Time = reader.ReadInt32();
			// Note: front/back wall nums would need segment lookup
		}

		return door;
	}

	private RdlTrigger ReadTrigger(BinaryReader reader)
	{
		var trigger = new RdlTrigger
		{
			Type = reader.ReadByte(),
			Flags = reader.ReadInt16(),
			Value = reader.ReadInt32(),
			Time = reader.ReadInt32(),
			LinkNum = reader.ReadByte(),
			NumLinks = reader.ReadInt16()
		};

		for (int i = 0; i < RdlTrigger.MaxWallsPerLink; i++)
		{
			trigger.Seg[i] = reader.ReadInt16();
		}
		for (int i = 0; i < RdlTrigger.MaxWallsPerLink; i++)
		{
			trigger.Side[i] = reader.ReadInt16();
		}

		return trigger;
	}

	private RdlControlCenterTrigger ReadControlCenterTrigger(BinaryReader reader)
	{
		var trigger = new RdlControlCenterTrigger
		{
			NumLinks = reader.ReadInt16()
		};

		for (int i = 0; i < RdlControlCenterTrigger.MaxWallsPerLink; i++)
		{
			trigger.Seg[i] = reader.ReadInt16();
		}
		for (int i = 0; i < RdlControlCenterTrigger.MaxWallsPerLink; i++)
		{
			trigger.Side[i] = reader.ReadInt16();
		}

		return trigger;
	}

	private RdlMatcen ReadMatcen(BinaryReader reader)
	{
		return new RdlMatcen
		{
			RobotFlags = reader.ReadInt32(),
			HitPoints = reader.ReadInt32(),
			Interval = reader.ReadInt32(),
			SegNum = reader.ReadInt16(),
			FuelCenNum = reader.ReadInt16()
		};
	}

	private void WriteMineData(BinaryWriter writer, RdlFile rdl)
	{
		writer.Write((byte)CompiledMineVersion);
		writer.Write((ushort)rdl.Vertices.Count);
		writer.Write((ushort)rdl.Segments.Count);

		foreach (var vertex in rdl.Vertices)
		{
			WriteVector(writer, vertex);
		}

		foreach (var segment in rdl.Segments)
		{
			WriteSegment(writer, segment);
		}
	}

	private void WriteSegment(BinaryWriter writer, RdlSegment segment)
	{
		byte childMask = 0;
		for (int i = 0; i < RdlSegment.MaxSidesPerSegment; i++)
		{
			if (segment.Children[i] != -1)
			{
				childMask |= (byte)(1 << i);
			}
		}

		// Set bit 6 if segment has special properties
		// Note: Original game checks matcen_num != 0, which is true for -1 (0xFF)
		if (segment.Special != 0 || segment.MatcenNum != 0 || segment.Value != 0)
		{
			childMask |= (byte)(1 << RdlSegment.MaxSidesPerSegment);
		}

		writer.Write(childMask);

		for (int i = 0; i < RdlSegment.MaxSidesPerSegment; i++)
		{
			if (segment.Children[i] != -1)
			{
				writer.Write(segment.Children[i]);
			}
		}

		for (int i = 0; i < RdlSegment.MaxVerticesPerSegment; i++)
		{
			writer.Write(segment.Verts[i]);
		}

		if ((childMask & (1 << RdlSegment.MaxSidesPerSegment)) != 0)
		{
			writer.Write(segment.Special);
			writer.Write(segment.MatcenNum);
			writer.Write(segment.Value);
		}

		writer.Write((ushort)(segment.StaticLight >> 4));

		byte wallMask = 0;
		for (int i = 0; i < RdlSegment.MaxSidesPerSegment; i++)
		{
			if (segment.Sides[i].WallNum != -1)
			{
				wallMask |= (byte)(1 << i);
			}
		}
		writer.Write(wallMask);

		for (int i = 0; i < RdlSegment.MaxSidesPerSegment; i++)
		{
			if (segment.Sides[i].WallNum != -1)
			{
				writer.Write((byte)(segment.Sides[i].WallNum == -1 ? 255 : segment.Sides[i].WallNum));
			}
		}

		for (int i = 0; i < RdlSegment.MaxSidesPerSegment; i++)
		{
			if (segment.Children[i] == -1 || segment.Sides[i].WallNum != -1)
			{
				var side = segment.Sides[i];
				ushort tmapWithFlag = (ushort)side.TmapNum;

				if (side.TmapNum2 != 0)
				{
					tmapWithFlag |= 0x8000;
				}

				writer.Write(tmapWithFlag);

				if (side.TmapNum2 != 0)
				{
					writer.Write(side.TmapNum2);
				}

				for (int j = 0; j < RdlSide.MaxVerticesPerSide; j++)
				{
					writer.Write((short)(side.Uvls[j].U >> 5));
					writer.Write((short)(side.Uvls[j].V >> 5));
					writer.Write((ushort)(side.Uvls[j].L >> 1));
				}
			}
		}
	}

	private void WriteGameData(BinaryWriter writer, RdlFile rdl)
	{
		var startPos = writer.BaseStream.Position;

		writer.Write((ushort)GameFileSignature);
		writer.Write((ushort)GameVersion);
		var fileInfoSizePos = writer.BaseStream.Position;
		writer.Write(0); // fileinfo_sizeof placeholder

		// Write mine filename (15 bytes, null-padded)
		var mineFilename = new byte[15];
		writer.Write(mineFilename);

		writer.Write(0); // level
		var playerOffsetPos = writer.BaseStream.Position;
		writer.Write(-1); // player_offset
		var playerSizePos = writer.BaseStream.Position;
		writer.Write(rdl.PlayerData?.Length ?? 0); // player_sizeof
		var objectOffsetPos = writer.BaseStream.Position;
		writer.Write(-1); // object_offset
		writer.Write(rdl.Objects.Count); // object_howmany
		writer.Write(0); // object_sizeof
		var wallsOffsetPos = writer.BaseStream.Position;
		writer.Write(-1); // walls_offset
		writer.Write(rdl.Walls.Count); // walls_howmany
		writer.Write(0); // walls_sizeof
		var doorsOffsetPos = writer.BaseStream.Position;
		writer.Write(-1); // doors_offset
		writer.Write(rdl.ActiveDoors.Count); // doors_howmany
		writer.Write(0); // doors_sizeof
		var triggersOffsetPos = writer.BaseStream.Position;
		writer.Write(-1); // triggers_offset
		writer.Write(rdl.Triggers.Count); // triggers_howmany
		writer.Write(0); // triggers_sizeof
		writer.Write(-1); // links_offset
		writer.Write(0); // links_howmany
		writer.Write(0); // links_sizeof
		var controlOffsetPos = writer.BaseStream.Position;
		writer.Write(-1); // control_offset
		writer.Write(1); // control_howmany (always 1)
		writer.Write(0); // control_sizeof
		var matcenOffsetPos = writer.BaseStream.Position;
		writer.Write(-1); // matcen_offset
		writer.Write(rdl.MatCens.Count); // matcen_howmany
		writer.Write(0); // matcen_sizeof

		var fileInfoSize = (int)(writer.BaseStream.Position - startPos);

		if (!string.IsNullOrEmpty(rdl.LevelName))
		{
			var nameBytes = System.Text.Encoding.ASCII.GetBytes(rdl.LevelName);
			writer.Write(nameBytes);
		}
		writer.Write((byte)0); // null terminator

		writer.Write((short)rdl.PofNames.Count);
		foreach (var pofName in rdl.PofNames)
		{
			var nameBytes = new byte[13];
			var srcBytes = System.Text.Encoding.ASCII.GetBytes(pofName);
			Array.Copy(srcBytes, nameBytes, Math.Min(srcBytes.Length, 13));
			writer.Write(nameBytes);
		}

		var playerOffset = (int)writer.BaseStream.Position;
		if (rdl.PlayerData != null && rdl.PlayerData.Length > 0)
		{
			writer.Write(rdl.PlayerData);
		}

		var objectOffset = (int)writer.BaseStream.Position;
		foreach (var obj in rdl.Objects)
		{
			WriteObject(writer, obj);
		}

		var wallsOffset = (int)writer.BaseStream.Position;
		foreach (var wall in rdl.Walls)
		{
			WriteWall(writer, wall);
		}

		var doorsOffset = (int)writer.BaseStream.Position;
		foreach (var door in rdl.ActiveDoors)
		{
			WriteActiveDoor(writer, door);
		}

		var triggersOffset = (int)writer.BaseStream.Position;
		foreach (var trigger in rdl.Triggers)
		{
			WriteTrigger(writer, trigger);
		}

		// Write control center triggers (always written, even if empty)
		var controlOffset = (int)writer.BaseStream.Position;
		WriteControlCenterTrigger(writer, rdl.ControlCenterTrigger);

		var matcenOffset = (int)writer.BaseStream.Position;
		foreach (var matcen in rdl.MatCens)
		{
			WriteMatcen(writer, matcen);
		}

		// Update offsets
		var endPos = writer.BaseStream.Position;

		writer.BaseStream.Position = fileInfoSizePos;
		writer.Write(fileInfoSize);

		writer.BaseStream.Position = playerOffsetPos;
		writer.Write(rdl.PlayerData != null && rdl.PlayerData.Length > 0 ? playerOffset : -1);

		writer.BaseStream.Position = objectOffsetPos;
		writer.Write(rdl.Objects.Count > 0 ? objectOffset : -1);

		writer.BaseStream.Position = wallsOffsetPos;
		writer.Write(rdl.Walls.Count > 0 ? wallsOffset : -1);

		writer.BaseStream.Position = doorsOffsetPos;
		writer.Write(rdl.ActiveDoors.Count > 0 ? doorsOffset : -1);

		writer.BaseStream.Position = triggersOffsetPos;
		writer.Write(rdl.Triggers.Count > 0 ? triggersOffset : -1);

		writer.BaseStream.Position = controlOffsetPos;
		writer.Write(controlOffset); // Always written

		writer.BaseStream.Position = matcenOffsetPos;
		writer.Write(rdl.MatCens.Count > 0 ? matcenOffset : -1);

		writer.BaseStream.Position = endPos;
	}

	private void WriteObject(BinaryWriter writer, RdlObject obj)
	{
		writer.Write(obj.Type);
		writer.Write(obj.Id);
		writer.Write(obj.ControlType);
		writer.Write(obj.MovementType);
		writer.Write(obj.RenderType);
		writer.Write(obj.Flags);
		writer.Write(obj.SegNum);

		WriteVector(writer, obj.Position);
		WriteMatrix(writer, obj.Orientation);
		writer.Write(obj.Size);
		writer.Write(obj.Shields);
		WriteVector(writer, obj.LastPos);
		writer.Write(obj.ContainsType);
		writer.Write(obj.ContainsId);
		writer.Write(obj.ContainsCount);

		switch (obj.MovementType)
		{
			case RdlObject.MovePhysics when obj.PhysicsInfo != null:
				WriteVector(writer, obj.PhysicsInfo.Velocity);
				WriteVector(writer, obj.PhysicsInfo.Thrust);
				writer.Write(obj.PhysicsInfo.Mass);
				writer.Write(obj.PhysicsInfo.Drag);
				writer.Write(obj.PhysicsInfo.Brakes);
				WriteVector(writer, obj.PhysicsInfo.RotVel);
				WriteVector(writer, obj.PhysicsInfo.RotThrust);
				writer.Write(obj.PhysicsInfo.TurnRoll);
				writer.Write(obj.PhysicsInfo.Flags);
				break;
			case RdlObject.MoveSpinning when obj.SpinRate.HasValue:
				WriteVector(writer, obj.SpinRate.Value);
				break;
		}

		switch (obj.ControlType)
		{
			case RdlObject.CtrlAi when obj.AiInfo != null:
				writer.Write(obj.AiInfo.Behavior);
				for (int i = 0; i < RdlAiInfo.MaxAiFlags; i++)
				{
					writer.Write(obj.AiInfo.Flags[i]);
				}
				writer.Write(obj.AiInfo.HideSegment);
				writer.Write(obj.AiInfo.HideIndex);
				writer.Write(obj.AiInfo.PathLength);
				writer.Write(obj.AiInfo.CurPathIndex);
				writer.Write(obj.AiInfo.FollowPathStartSeg);
				writer.Write(obj.AiInfo.FollowPathEndSeg);
				break;

			case RdlObject.CtrlExplosion when obj.ExplosionInfo != null:
				writer.Write(obj.ExplosionInfo.SpawnTime);
				writer.Write(obj.ExplosionInfo.DeleteTime);
				writer.Write(obj.ExplosionInfo.DeleteObjNum);
				break;

			case RdlObject.CtrlWeapon when obj.LaserInfo != null:
				writer.Write(obj.LaserInfo.ParentType);
				writer.Write(obj.LaserInfo.ParentNum);
				writer.Write(obj.LaserInfo.ParentSignature);
				break;

			case RdlObject.CtrlLight when obj.LightIntensity.HasValue:
				writer.Write(obj.LightIntensity.Value);
				break;

			case RdlObject.CtrlPowerup:
				writer.Write(obj.PowerupCount ?? 1);
				break;
		}

		switch (obj.RenderType)
		{
			case RdlObject.RenderPolyobj:
			case RdlObject.RenderMorph:
				if (obj.PolyObjInfo != null)
				{
					writer.Write(obj.PolyObjInfo.ModelNum);
					for (int i = 0; i < RdlPolyObjInfo.MaxSubmodels; i++)
					{
						WriteAngvec(writer, obj.PolyObjInfo.AnimAngles[i]);
					}
					writer.Write(obj.PolyObjInfo.SubobjFlags);
					writer.Write(obj.PolyObjInfo.TmapOverride);
				}
				break;

			case RdlObject.RenderWeaponVclip:
			case RdlObject.RenderHostage:
			case RdlObject.RenderPowerup:
			case RdlObject.RenderFireball:
				if (obj.VClipInfo != null)
				{
					writer.Write(obj.VClipInfo.VClipNum);
					writer.Write(obj.VClipInfo.FrameTime);
					writer.Write(obj.VClipInfo.FrameNum);
				}
				break;
		}
	}

	private void WriteWall(BinaryWriter writer, RdlWall wall)
	{
		writer.Write(wall.SegNum);
		writer.Write(wall.SideNum);
		writer.Write(wall.Hps);
		writer.Write(wall.LinkedWall);
		writer.Write(wall.Type);
		writer.Write(wall.Flags);
		writer.Write(wall.State);
		writer.Write(wall.Trigger);
		writer.Write(wall.ClipNum);
		writer.Write(wall.Keys);
		writer.Write((short)0); // pad
	}

	private void WriteActiveDoor(BinaryWriter writer, RdlActiveDoor door)
	{
		writer.Write(door.NumParts);
		writer.Write(door.FrontWallNum[0]);
		writer.Write(door.FrontWallNum[1]);
		writer.Write(door.BackWallNum[0]);
		writer.Write(door.BackWallNum[1]);
		writer.Write(door.Time);
	}

	private void WriteTrigger(BinaryWriter writer, RdlTrigger trigger)
	{
		writer.Write(trigger.Type);
		writer.Write(trigger.Flags);
		writer.Write(trigger.Value);
		writer.Write(trigger.Time);
		writer.Write(trigger.LinkNum);
		writer.Write(trigger.NumLinks);

		for (int i = 0; i < RdlTrigger.MaxWallsPerLink; i++)
		{
			writer.Write(trigger.Seg[i]);
		}
		for (int i = 0; i < RdlTrigger.MaxWallsPerLink; i++)
		{
			writer.Write(trigger.Side[i]);
		}
	}

	private void WriteControlCenterTrigger(BinaryWriter writer, RdlControlCenterTrigger trigger)
	{
		writer.Write(trigger.NumLinks);

		for (int i = 0; i < RdlControlCenterTrigger.MaxWallsPerLink; i++)
		{
			writer.Write(trigger.Seg[i]);
		}
		for (int i = 0; i < RdlControlCenterTrigger.MaxWallsPerLink; i++)
		{
			writer.Write(trigger.Side[i]);
		}
	}

	private void WriteMatcen(BinaryWriter writer, RdlMatcen matcen)
	{
		writer.Write(matcen.RobotFlags);
		writer.Write(matcen.HitPoints);
		writer.Write(matcen.Interval);
		writer.Write(matcen.SegNum);
		writer.Write(matcen.FuelCenNum);
	}

	private void WriteHostageData(BinaryWriter writer, RdlFile rdl)
	{
		if (rdl.Hostages.Count == 0)
			return;

		writer.Write(HostageDataVersion);

		foreach (var hostage in rdl.Hostages)
		{
			writer.Write(hostage.VClipNum);

			var textBytes = System.Text.Encoding.ASCII.GetBytes(hostage.Text);
			writer.Write(textBytes);
			writer.Write((byte)'\n');
		}
	}

	private VmsVector ReadVector(BinaryReader reader)
	{
		return new VmsVector(
			reader.ReadInt32(),
			reader.ReadInt32(),
			reader.ReadInt32()
		);
	}

	private void WriteVector(BinaryWriter writer, VmsVector vector)
	{
		writer.Write(vector.X);
		writer.Write(vector.Y);
		writer.Write(vector.Z);
	}

	private VmsMatrix ReadMatrix(BinaryReader reader)
	{
		return new VmsMatrix(
			ReadVector(reader),
			ReadVector(reader),
			ReadVector(reader)
		);
	}

	private void WriteMatrix(BinaryWriter writer, VmsMatrix matrix)
	{
		WriteVector(writer, matrix.RVec);
		WriteVector(writer, matrix.UVec);
		WriteVector(writer, matrix.FVec);
	}

	private VmsAngvec ReadAngvec(BinaryReader reader)
	{
		return new VmsAngvec(
			reader.ReadInt16(),
			reader.ReadInt16(),
			reader.ReadInt16()
		);
	}

	private void WriteAngvec(BinaryWriter writer, VmsAngvec angvec)
	{
		writer.Write(angvec.P);
		writer.Write(angvec.B);
		writer.Write(angvec.H);
	}
}