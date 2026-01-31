using ii.Ascend.Model;

namespace ii.Ascend;

public class Descent1PigProcessor : IPigProcessor
{
    private const int DISK_BITMAP_HEADER_SIZE = 17;
    private const int DISK_SOUND_HEADER_SIZE = 20;
    private const byte DBM_FLAG_LARGE = 128;  // Width + 256

	public bool CanHandle(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        if (fileStream.Length < 4)
            return false;

        var firstInt = binaryReader.ReadInt32();

		// Descent 2 PIG files start with 'PPIG' signature (0x47495050)
		// If it's NOT that signature, assume Descent 1 format
		return firstInt != 0x47495050;
	}

    public List<(string filename, byte[] bytes)> Read(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        var pigdataStart = binaryReader.ReadInt32();
        var result = new List<(string filename, byte[] bytes)>();

        if (pigdataStart > 4)
        {
            var gameData = ReadGameData(binaryReader, pigdataStart);
            var gameDataBytes = SerializeGameDataSummary(gameData);
            result.Add(("gamedata.txt", gameDataBytes));
        }

        binaryReader.BaseStream.Position = pigdataStart;

        var numImages = binaryReader.ReadInt32();
        var numSounds = binaryReader.ReadInt32();

        var headerSize = (numImages * DISK_BITMAP_HEADER_SIZE) + (numSounds * DISK_SOUND_HEADER_SIZE);

		// Base offset for data: pigdataStart + numImages size + numSounds size ints (counts) + all headers
		var dataBaseOffset = pigdataStart + 4 + 4 + headerSize;

		// Read image structs
		var imageEntries = new List<(string name, byte dflags, byte width, byte height, byte flags, byte avgColor, uint offset)>();
        for (int i = 0; i < numImages; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var dflags = binaryReader.ReadByte();
            var width = binaryReader.ReadByte();
            var height = binaryReader.ReadByte();
            var flags = binaryReader.ReadByte();
            var avgColor = binaryReader.ReadByte();
            var offset = binaryReader.ReadUInt32();
            imageEntries.Add((name, dflags, width, height, flags, avgColor, offset));
        }

        var soundEntries = new List<(string name, uint length, uint offset)>();
        for (int i = 0; i < numSounds; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
			var length = binaryReader.ReadUInt32(); // uncompressed length
			var dataLength = binaryReader.ReadUInt32(); // compressed length
			var offset = binaryReader.ReadUInt32();
            soundEntries.Add((name, length, offset));
        }

        for (int i = 0; i < imageEntries.Count; i++)
        {
            var entry = imageEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

			// Determine size: read from this offset to next image offset, or first sound offset, or end of file
			uint dataSize;
            if (i + 1 < imageEntries.Count)
            {
                var nextAbsoluteOffset = (long)dataBaseOffset + imageEntries[i + 1].offset;
                dataSize = (uint)(nextAbsoluteOffset - absoluteOffset);
            }
            else if (soundEntries.Count > 0)
            {
                var firstSoundOffset = (long)dataBaseOffset + soundEntries[0].offset;
                dataSize = (uint)(firstSoundOffset - absoluteOffset);
            }
            else
            {
                dataSize = (uint)(binaryReader.BaseStream.Length - absoluteOffset);
            }

            var fileData = binaryReader.ReadBytes((int)dataSize);
            if (fileData.Length < dataSize)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading image data. Expected {dataSize} bytes, got {fileData.Length}.");
            }

            result.Add((entry.name + ".bbm", fileData));
        }

        for (int i = 0; i < soundEntries.Count; i++)
        {
            var entry = soundEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            var fileData = binaryReader.ReadBytes((int)entry.length);
            if (fileData.Length < entry.length)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading sound data. Expected {entry.length} bytes, got {fileData.Length}.");
            }

            result.Add((entry.name + ".raw", fileData));
        }

        return result;
    }

    public (List<ImageInfo> images, List<SoundInfo> sounds, List<(string filename, byte[] data)> pofFiles, D1PigGameData? gameData) ReadDetailed(string filename)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        var pigdataStart = binaryReader.ReadInt32();

        D1PigGameData? gameData = null;
        if (pigdataStart > 4)
        {
            gameData = ReadGameData(binaryReader, pigdataStart);
        }

        binaryReader.BaseStream.Position = pigdataStart;

        var numImages = binaryReader.ReadInt32();
        var numSounds = binaryReader.ReadInt32();

        var headerSize = (numImages * DISK_BITMAP_HEADER_SIZE) + (numSounds * DISK_SOUND_HEADER_SIZE);
        var dataBaseOffset = pigdataStart + 4 + 4 + headerSize;

        var imageEntries = new List<(string name, byte dflags, byte width, byte height, byte flags, byte avgColor, uint offset)>();
        for (var i = 0; i < numImages; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var dflags = binaryReader.ReadByte();
            var width = binaryReader.ReadByte();
            var height = binaryReader.ReadByte();
            var flags = binaryReader.ReadByte();
            var avgColor = binaryReader.ReadByte();
            var offset = binaryReader.ReadUInt32();
            imageEntries.Add((name, dflags, width, height, flags, avgColor, offset));
        }

        var soundEntries = new List<(string name, uint length, uint offset)>();
        for (var i = 0; i < numSounds; i++)
        {
            var nameBytes = binaryReader.ReadBytes(8);
            var name = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            var length = binaryReader.ReadUInt32();
            var dataLength = binaryReader.ReadUInt32();
            var offset = binaryReader.ReadUInt32();
            soundEntries.Add((name, length, offset));
        }

        var images = new List<ImageInfo>();
        var sounds = new List<SoundInfo>();

        for (var i = 0; i < imageEntries.Count; i++)
        {
            var entry = imageEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            uint dataSize;
            if (i + 1 < imageEntries.Count)
            {
                var nextAbsoluteOffset = (long)dataBaseOffset + imageEntries[i + 1].offset;
                dataSize = (uint)(nextAbsoluteOffset - absoluteOffset);
            }
            else if (soundEntries.Count > 0)
            {
                var firstSoundOffset = (long)dataBaseOffset + soundEntries[0].offset;
                dataSize = (uint)(firstSoundOffset - absoluteOffset);
            }
            else
            {
                dataSize = (uint)(binaryReader.BaseStream.Length - absoluteOffset);
            }

            var fileData = binaryReader.ReadBytes((int)dataSize);
            if (fileData.Length < dataSize)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading image data. Expected {dataSize} bytes, got {fileData.Length}.");
            }

            var actualWidth = (entry.dflags & DBM_FLAG_LARGE) != 0 ? (short)(entry.width + 256) : entry.width;
            var actualHeight = (short)entry.height;
            var isRleCompressed = (entry.flags & 8) != 0;

            images.Add(new ImageInfo
            {
                Filename = entry.name + ".bbm",
                Data = fileData,
                Width = actualWidth,
                Height = actualHeight,
                IsRleCompressed = isRleCompressed,
                Flags = entry.flags,
                AvgColor = entry.avgColor
            });
        }

        for (var i = 0; i < soundEntries.Count; i++)
        {
            var entry = soundEntries[i];
            var absoluteOffset = (long)dataBaseOffset + entry.offset;
            binaryReader.BaseStream.Position = absoluteOffset;

            var fileData = binaryReader.ReadBytes((int)entry.length);
            if (fileData.Length < entry.length)
            {
                throw new EndOfStreamException($"Unexpected end of stream while reading sound data. Expected {entry.length} bytes, got {fileData.Length}.");
            }

            sounds.Add(new SoundInfo
            {
                Filename = entry.name + ".raw",
                Data = fileData,
                UncompressedLength = entry.length
            });
        }

        var pofFiles = new List<(string filename, byte[] data)>();
        var pofProcessor = new PofProcessor();
        
        if (gameData != null && gameData.PolygonModels != null)
        {
            for (int i = 0; i < gameData.NumPolygonModels && i < gameData.PolygonModels.Count; i++)
            {
                var model = gameData.PolygonModels[i];
                if (model.ModelData == null || model.ModelData.Length == 0)
                    continue;

                string pofFilename = $"model{i:D2}.pof";
                
                var robot = gameData.Robots.FirstOrDefault(r => r.ModelNum == i);
                if (robot != null)
                {
                    var robotIndex = gameData.Robots.IndexOf(robot);
                    pofFilename = $"robot{robotIndex:D2}.pof";
                }
                else
                {
                    var weapon = gameData.Weapons.FirstOrDefault(w => w.ModelNum == i || w.ModelNumInner == i);
                    if (weapon != null)
                    {
                        var weaponIndex = gameData.Weapons.IndexOf(weapon);
                        pofFilename = $"weapon{weaponIndex:D2}.pof";
                    }
                    else if (i == gameData.PlayerShip.ModelNum)
                    {
                        pofFilename = "player.pof";
                    }
                    else if (i == gameData.ExitModelNum)
                    {
                        pofFilename = "exit01.pof";
                    }
                    else if (i == gameData.DestroyedExitModelNum)
                    {
                        pofFilename = "exit01d.pof";
                    }
                }

                try
                {
                    var pofData = pofProcessor.Write(model);
                    pofFiles.Add((pofFilename, pofData));
                }
                catch
                {
                    // Skip models that can't be converted
                }
            }
        }

        return (images, sounds, pofFiles, gameData);
    }

    #region Game Data Reading

    private D1PigGameData ReadGameData(BinaryReader reader, int pigdataStart)
    {
        reader.BaseStream.Position = 4;

        var data = new D1PigGameData();

        data.NumTextures = reader.ReadInt32();

        for (int i = 0; i < D1PigGameData.MaxTextures; i++)
        {
            data.TextureBitmapIndices.Add(reader.ReadUInt16());
        }

        for (int i = 0; i < D1PigGameData.MaxTextures; i++)
        {
            data.TmapInfos.Add(ReadD1TmapInfo(reader));
        }

        data.Sounds = reader.ReadBytes(D1PigGameData.MaxSounds);
        data.AltSounds = reader.ReadBytes(D1PigGameData.MaxSounds);

        data.NumVClips = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.VClipMaxNum; i++)
        {
            data.VClips.Add(ReadVClip(reader));
        }

        data.NumEffects = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.MaxEffects; i++)
        {
            data.EClips.Add(ReadEClip(reader));
        }

        data.NumWallAnims = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.MaxWallAnims; i++)
        {
            data.WClips.Add(ReadD1WClip(reader));
        }

        data.NumRobotTypes = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.MaxRobotTypes; i++)
        {
            data.Robots.Add(ReadD1RobotInfo(reader));
        }

        data.NumRobotJoints = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.MaxRobotJoints; i++)
        {
            data.RobotJoints.Add(ReadJointPos(reader));
        }

        data.NumWeaponTypes = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.MaxWeaponTypes; i++)
        {
            data.Weapons.Add(ReadD1WeaponInfo(reader));
        }

        data.NumPowerupTypes = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.MaxPowerupTypes; i++)
        {
            data.Powerups.Add(ReadPowerupInfo(reader));
        }

        data.NumPolygonModels = reader.ReadInt32();
        for (int i = 0; i < data.NumPolygonModels; i++)
        {
            data.PolygonModels.Add(ReadPolyModelHeader(reader));
        }

        for (int i = 0; i < data.NumPolygonModels; i++)
        {
            var model = data.PolygonModels[i];
            model.ModelData = reader.ReadBytes(model.ModelDataSize);
        }

        for (int i = 0; i < D1PigGameData.MaxGaugeBms; i++)
        {
            data.Gauges.Add(reader.ReadUInt16());
        }

        for (int i = 0; i < D1PigGameData.MaxPolygonModels; i++)
        {
            data.DyingModelNums.Add(reader.ReadInt32());
        }

        for (int i = 0; i < D1PigGameData.MaxPolygonModels; i++)
        {
            data.DeadModelNums.Add(reader.ReadInt32());
        }

        for (int i = 0; i < D1PigGameData.MaxObjBitmaps; i++)
        {
            data.ObjBitmaps.Add(reader.ReadUInt16());
        }

        for (int i = 0; i < D1PigGameData.MaxObjBitmaps; i++)
        {
            data.ObjBitmapPtrs.Add(reader.ReadUInt16());
        }

        data.PlayerShip = ReadPlayerShip(reader);

        data.NumCockpits = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.NumCockpitBitmaps; i++)
        {
            data.CockpitBitmaps.Add(reader.ReadUInt16());
        }

        data.Sounds2 = reader.ReadBytes(D1PigGameData.MaxSounds);
        data.AltSounds2 = reader.ReadBytes(D1PigGameData.MaxSounds);

        data.NumTotalObjectTypes = reader.ReadInt32();
        data.ObjType = reader.ReadBytes(D1PigGameData.MaxObjType);
        data.ObjId = reader.ReadBytes(D1PigGameData.MaxObjType);
        data.ObjStrength = new int[D1PigGameData.MaxObjType];
        for (int i = 0; i < D1PigGameData.MaxObjType; i++)
        {
            data.ObjStrength[i] = reader.ReadInt32();
        }

        data.FirstMultiBitmapNum = reader.ReadInt32();

        data.NumControlCenGuns = reader.ReadInt32();
        for (int i = 0; i < D1PigGameData.MaxControlCenGuns; i++)
        {
            data.ControlCenGunPoints[i] = ReadVector(reader);
        }
        for (int i = 0; i < D1PigGameData.MaxControlCenGuns; i++)
        {
            data.ControlCenGunDirs[i] = ReadVector(reader);
        }

        data.ExitModelNum = reader.ReadInt32();
        data.DestroyedExitModelNum = reader.ReadInt32();

        return data;
    }

    private D1TmapInfo ReadD1TmapInfo(BinaryReader reader)
    {
        var filenameBytes = reader.ReadBytes(13);
        var filename = System.Text.Encoding.ASCII.GetString(filenameBytes).TrimEnd('\0');

        return new D1TmapInfo
        {
            Filename = filename,
            Flags = reader.ReadByte(),
            Lighting = reader.ReadInt32(),
            Damage = reader.ReadInt32(),
            EClipNum = reader.ReadInt32()
        };
    }

    private VClip ReadVClip(BinaryReader reader)
    {
        var vclip = new VClip
        {
            PlayTime = reader.ReadInt32(),
            NumFrames = reader.ReadInt32(),
            FrameTime = reader.ReadInt32(),
            Flags = reader.ReadInt32(),
            SoundNum = reader.ReadInt16()
        };

        for (int i = 0; i < VClip.MaxFrames; i++)
        {
            vclip.Frames[i] = reader.ReadUInt16();
        }

        vclip.LightValue = reader.ReadInt32();
        return vclip;
    }

    private EClip ReadEClip(BinaryReader reader)
    {
        return new EClip
        {
            Vc = ReadVClip(reader),
            TimeLeft = reader.ReadInt32(),
            FrameCount = reader.ReadInt32(),
            ChangingWallTexture = reader.ReadInt16(),
            ChangingObjectTexture = reader.ReadInt16(),
            Flags = reader.ReadInt32(),
            CritClip = reader.ReadInt32(),
            DestBmNum = reader.ReadInt32(),
            DestVClip = reader.ReadInt32(),
            DestEClip = reader.ReadInt32(),
            DestSize = reader.ReadInt32(),
            SoundNum = reader.ReadInt32(),
            SegNum = reader.ReadInt32(),
            SideNum = reader.ReadInt32()
        };
    }

    private D1WClip ReadD1WClip(BinaryReader reader)
    {
        var wclip = new D1WClip
        {
            PlayTime = reader.ReadInt32(),
            NumFrames = reader.ReadInt16()
        };

        for (int i = 0; i < D1WClip.MaxFrames; i++)
        {
            wclip.Frames[i] = reader.ReadInt16();
        }

        wclip.OpenSound = reader.ReadInt16();
        wclip.CloseSound = reader.ReadInt16();
        wclip.Flags = reader.ReadInt16();

        var filenameBytes = reader.ReadBytes(13);
        wclip.Filename = System.Text.Encoding.ASCII.GetString(filenameBytes).TrimEnd('\0');

        reader.ReadByte(); // pad

        return wclip;
    }

    private D1RobotInfo ReadD1RobotInfo(BinaryReader reader)
    {
        var robot = new D1RobotInfo
        {
            ModelNum = reader.ReadInt32(),
            NumGuns = reader.ReadInt32()
        };

        for (int i = 0; i < D1RobotInfo.MaxGuns; i++)
        {
            robot.GunPoints[i] = ReadVector(reader);
        }

        robot.GunSubmodels = reader.ReadBytes(D1RobotInfo.MaxGuns);

        robot.Exp1VClipNum = reader.ReadInt16();
        robot.Exp1SoundNum = reader.ReadInt16();
        robot.Exp2VClipNum = reader.ReadInt16();
        robot.Exp2SoundNum = reader.ReadInt16();

        robot.WeaponType = reader.ReadInt16();

        robot.ContainsId = reader.ReadSByte();
        robot.ContainsCount = reader.ReadSByte();
        robot.ContainsProb = reader.ReadSByte();
        robot.ContainsType = reader.ReadSByte();

        robot.ScoreValue = reader.ReadInt32();

        robot.Lighting = reader.ReadInt32();
        robot.Strength = reader.ReadInt32();

        robot.Mass = reader.ReadInt32();
        robot.Drag = reader.ReadInt32();

        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.FieldOfView[i] = reader.ReadInt32();
        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.FiringWait[i] = reader.ReadInt32();
        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.TurnTime[i] = reader.ReadInt32();
        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.FirePower[i] = reader.ReadInt32();
        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.Shield[i] = reader.ReadInt32();
        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.MaxSpeed[i] = reader.ReadInt32();
        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.CircleDistance[i] = reader.ReadInt32();

        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.RapidfireCount[i] = reader.ReadSByte();
        for (int i = 0; i < D1RobotInfo.NumDifficultyLevels; i++)
            robot.EvadeSpeed[i] = reader.ReadSByte();

        robot.CloakType = reader.ReadSByte();
        robot.AttackType = reader.ReadSByte();
        robot.BossFlag = reader.ReadSByte();

        robot.SeeSound = reader.ReadByte();
        robot.AttackSound = reader.ReadByte();
        robot.ClawSound = reader.ReadByte();

        // Animation states
        for (int gun = 0; gun < D1RobotInfo.MaxGuns + 1; gun++)
        {
            for (int state = 0; state < D1RobotInfo.NumAnimStates; state++)
            {
                robot.AnimStates[gun, state] = new JointList(
                    reader.ReadInt16(),
                    reader.ReadInt16()
                );
            }
        }

        robot.Always0xABCD = reader.ReadInt32();

        return robot;
    }

    private JointPos ReadJointPos(BinaryReader reader)
    {
        return new JointPos(
            reader.ReadInt16(),
            new VmsAngvec(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16())
        );
    }

    private D1WeaponInfo ReadD1WeaponInfo(BinaryReader reader)
    {
        var weapon = new D1WeaponInfo
        {
            RenderType = reader.ReadSByte(),
            ModelNum = reader.ReadSByte(),
            ModelNumInner = reader.ReadSByte(),
            Persistent = reader.ReadSByte(),

            FlashVClip = reader.ReadSByte(),
            FlashSound = reader.ReadInt16(),
            RobotHitVClip = reader.ReadSByte(),
            RobotHitSound = reader.ReadInt16(),

            WallHitVClip = reader.ReadSByte(),
            WallHitSound = reader.ReadInt16(),
            FireCount = reader.ReadSByte(),
            AmmoUsage = reader.ReadSByte(),

            WeaponVClip = reader.ReadSByte(),
            Destroyable = reader.ReadSByte(),
            Matter = reader.ReadSByte(),
            Bounce = reader.ReadSByte(),

            HomingFlag = reader.ReadSByte(),
            Dum1 = reader.ReadSByte(),
            Dum2 = reader.ReadSByte(),
            Dum3 = reader.ReadSByte(),

            EnergyUsage = reader.ReadInt32(),
            FireWait = reader.ReadInt32(),

            BitmapIndex = reader.ReadUInt16(),

            BlobSize = reader.ReadInt32(),
            FlashSize = reader.ReadInt32(),
            ImpactSize = reader.ReadInt32()
        };

        for (int i = 0; i < D1WeaponInfo.NumDifficultyLevels; i++)
            weapon.Strength[i] = reader.ReadInt32();
        for (int i = 0; i < D1WeaponInfo.NumDifficultyLevels; i++)
            weapon.Speed[i] = reader.ReadInt32();

        weapon.Mass = reader.ReadInt32();
        weapon.Drag = reader.ReadInt32();
        weapon.Thrust = reader.ReadInt32();
        weapon.PoLenToWidthRatio = reader.ReadInt32();
        weapon.Light = reader.ReadInt32();
        weapon.Lifetime = reader.ReadInt32();
        weapon.DamageRadius = reader.ReadInt32();

        weapon.Picture = reader.ReadUInt16();

        return weapon;
    }

    private PowerupInfo ReadPowerupInfo(BinaryReader reader)
    {
        return new PowerupInfo
        {
            VClipNum = reader.ReadInt32(),
            HitSound = reader.ReadInt32(),
            Size = reader.ReadInt32(),
            Light = reader.ReadInt32()
        };
    }

    private PolyModel ReadPolyModelHeader(BinaryReader reader)
    {
        var model = new PolyModel
        {
            NumModels = reader.ReadInt32(),
            ModelDataSize = reader.ReadInt32()
        };

        reader.ReadInt32(); // model_data pointer (unused)

        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            model.SubmodelPtrs[i] = reader.ReadInt32();
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            model.SubmodelOffsets[i] = ReadVector(reader);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            model.SubmodelNorms[i] = ReadVector(reader);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            model.SubmodelPnts[i] = ReadVector(reader);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            model.SubmodelRads[i] = reader.ReadInt32();
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            model.SubmodelParents[i] = reader.ReadByte();
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            model.SubmodelMins[i] = ReadVector(reader);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            model.SubmodelMaxs[i] = ReadVector(reader);

        model.Mins = ReadVector(reader);
        model.Maxs = ReadVector(reader);
        model.Rad = reader.ReadInt32();
        model.NumTextures = reader.ReadByte();
        model.FirstTexture = reader.ReadUInt16();
        model.SimplerModel = reader.ReadByte();

        return model;
    }

    private PlayerShip ReadPlayerShip(BinaryReader reader)
    {
        var ship = new PlayerShip
        {
            ModelNum = reader.ReadInt32(),
            ExplVClipNum = reader.ReadInt32(),
            Mass = reader.ReadInt32(),
            Drag = reader.ReadInt32(),
            MaxThrust = reader.ReadInt32(),
            ReverseThrust = reader.ReadInt32(),
            Brakes = reader.ReadInt32(),
            Wiggle = reader.ReadInt32(),
            MaxRotThrust = reader.ReadInt32()
        };

        for (int i = 0; i < PlayerShip.NumPlayerGuns; i++)
        {
            ship.GunPoints[i] = ReadVector(reader);
        }

        return ship;
    }

    private VmsVector ReadVector(BinaryReader reader)
    {
        return new VmsVector(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );
    }

    private byte[] SerializeGameDataSummary(D1PigGameData data)
    {
        var lines = new List<string>
        {
            $"Textures: {data.NumTextures} (max {D1PigGameData.MaxTextures})",
            $"VClips: {data.NumVClips} (max {D1PigGameData.VClipMaxNum})",
            $"EClips (Effects): {data.NumEffects} (max {D1PigGameData.MaxEffects})",
            $"WClips (Wall Anims): {data.NumWallAnims} (max {D1PigGameData.MaxWallAnims})",
            $"Robot Types: {data.NumRobotTypes} (max {D1PigGameData.MaxRobotTypes})",
            $"Robot Joints: {data.NumRobotJoints} (max {D1PigGameData.MaxRobotJoints})",
            $"Weapon Types: {data.NumWeaponTypes} (max {D1PigGameData.MaxWeaponTypes})",
            $"Powerup Types: {data.NumPowerupTypes} (max {D1PigGameData.MaxPowerupTypes})",
            $"Polygon Models: {data.NumPolygonModels}",
            $"Cockpits: {data.NumCockpits}",
            $"Object Types: {data.NumTotalObjectTypes}",
            $"Control Center Guns: {data.NumControlCenGuns}",
            $"First Multi Bitmap Num: {data.FirstMultiBitmapNum}",
            $"Exit Model Num: {data.ExitModelNum}",
            $"Destroyed Exit Model Num: {data.DestroyedExitModelNum}",
            "",
            "Player Ship:",
            $"  Model: {data.PlayerShip.ModelNum}",
            $"  Explosion VClip: {data.PlayerShip.ExplVClipNum}",
            "",
            "Robots:",
        };

        for (int i = 0; i < data.NumRobotTypes && i < data.Robots.Count; i++)
        {
            var robot = data.Robots[i];
            lines.Add($"  [{i}] Model:{robot.ModelNum} Guns:{robot.NumGuns} Weapon:{robot.WeaponType} Score:{robot.ScoreValue}");
        }

        lines.Add("");
        lines.Add("Weapons:");
        for (int i = 0; i < data.NumWeaponTypes && i < data.Weapons.Count; i++)
        {
            var weapon = data.Weapons[i];
            lines.Add($"  [{i}] Render:{weapon.RenderType} Model:{weapon.ModelNum} Damage:{weapon.Strength[2]}");
        }

        return System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines));
    }

    #endregion
}