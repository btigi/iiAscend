using ii.Ascend.Model;

namespace ii.Ascend;

public class HamProcessor
{
    private const uint ExpectedSignature = 0x214D4148; // "HAM!" in little-endian
    private const int ExpectedVersion = 3;

    public HamFile Read(string filename)
    {
        var fileData = File.ReadAllBytes(filename);
        return Read(fileData);
    }

    public HamFile Read(byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var reader = new BinaryReader(stream);

        var signature = reader.ReadUInt32();
        if (signature != ExpectedSignature)
        {
            throw new InvalidDataException(
                $"Invalid HAM file. Expected signature 0x{ExpectedSignature:X8}, got 0x{signature:X8}.");
        }

        var version = reader.ReadInt32();
        if (version != ExpectedVersion)
        {
            throw new InvalidDataException(
                $"Unsupported HAM version. Expected {ExpectedVersion}, got {version}.");
        }

        var ham = new HamFile();

        var numTextures = reader.ReadInt32();
        for (int i = 0; i < numTextures; i++)
        {
            ham.TextureBitmapIndices.Add(reader.ReadUInt16());
        }
        for (int i = 0; i < numTextures; i++)
        {
            ham.TmapInfos.Add(ReadTmapInfo(reader));
        }

        var numSounds = reader.ReadInt32();
        ham.Sounds = reader.ReadBytes(numSounds);
        ham.AltSounds = reader.ReadBytes(numSounds);

        var numVClips = reader.ReadInt32();
        for (int i = 0; i < numVClips; i++)
        {
            ham.VClips.Add(ReadVClip(reader));
        }

        var numEClips = reader.ReadInt32();
        for (int i = 0; i < numEClips; i++)
        {
            ham.EClips.Add(ReadEClip(reader));
        }

        var numWClips = reader.ReadInt32();
        for (int i = 0; i < numWClips; i++)
        {
            ham.WClips.Add(ReadWClip(reader));
        }

        var numRobots = reader.ReadInt32();
        for (int i = 0; i < numRobots; i++)
        {
            ham.Robots.Add(ReadRobotInfo(reader));
        }

        var numJoints = reader.ReadInt32();
        for (int i = 0; i < numJoints; i++)
        {
            ham.RobotJoints.Add(ReadJointPos(reader));
        }

        var numWeapons = reader.ReadInt32();
        for (int i = 0; i < numWeapons; i++)
        {
            ham.Weapons.Add(ReadWeaponInfo(reader));
        }

        var numPowerups = reader.ReadInt32();
        for (int i = 0; i < numPowerups; i++)
        {
            ham.Powerups.Add(ReadPowerupInfo(reader));
        }

        var numModels = reader.ReadInt32();
        for (int i = 0; i < numModels; i++)
        {
            ham.PolygonModels.Add(ReadPolyModelHeader(reader));
        }

        for (int i = 0; i < numModels; i++)
        {
            var model = ham.PolygonModels[i];
            model.ModelData = reader.ReadBytes(model.ModelDataSize);
        }

        for (int i = 0; i < numModels; i++)
        {
            ham.DyingModelNums.Add(reader.ReadInt32());
        }
        for (int i = 0; i < numModels; i++)
        {
            ham.DeadModelNums.Add(reader.ReadInt32());
        }

        var numGauges = reader.ReadInt32();
        for (int i = 0; i < numGauges; i++)
        {
            ham.GaugesLores.Add(reader.ReadUInt16());
        }
        for (int i = 0; i < numGauges; i++)
        {
            ham.GaugesHires.Add(reader.ReadUInt16());
        }

        var numObjBitmaps = reader.ReadInt32();
        for (int i = 0; i < numObjBitmaps; i++)
        {
            ham.ObjBitmaps.Add(reader.ReadUInt16());
        }
        for (int i = 0; i < numObjBitmaps; i++)
        {
            ham.ObjBitmapPtrs.Add(reader.ReadUInt16());
        }

        ham.PlayerShip = ReadPlayerShip(reader);

        var numCockpits = reader.ReadInt32();
        for (int i = 0; i < numCockpits; i++)
        {
            ham.CockpitBitmaps.Add(reader.ReadUInt16());
        }

        ham.FirstMultiBitmapNum = reader.ReadInt32();

        var numReactors = reader.ReadInt32();
        for (int i = 0; i < numReactors; i++)
        {
            ham.Reactors.Add(ReadReactor(reader));
        }

        ham.MarkerModelNum = reader.ReadInt32();

        if (stream.Position < stream.Length)
        {
            var remaining = (int)(stream.Length - stream.Position) / 2;
            ham.GameBitmapXlat = new ushort[remaining];
            for (int i = 0; i < remaining; i++)
            {
                ham.GameBitmapXlat[i] = reader.ReadUInt16();
            }
        }

        return ham;
    }

    public void Write(string filename, HamFile ham)
    {
        var fileData = Write(ham);
        File.WriteAllBytes(filename, fileData);
    }

    public byte[] Write(HamFile ham)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(ExpectedSignature);
        writer.Write(ExpectedVersion);

        writer.Write(ham.TextureBitmapIndices.Count);
        foreach (var idx in ham.TextureBitmapIndices)
        {
            writer.Write(idx);
        }
        foreach (var tmap in ham.TmapInfos)
        {
            WriteTmapInfo(writer, tmap);
        }

        writer.Write(ham.Sounds.Length);
        writer.Write(ham.Sounds);
        writer.Write(ham.AltSounds);

        writer.Write(ham.VClips.Count);
        foreach (var vclip in ham.VClips)
        {
            WriteVClip(writer, vclip);
        }

        writer.Write(ham.EClips.Count);
        foreach (var eclip in ham.EClips)
        {
            WriteEClip(writer, eclip);
        }

        writer.Write(ham.WClips.Count);
        foreach (var wclip in ham.WClips)
        {
            WriteWClip(writer, wclip);
        }

        writer.Write(ham.Robots.Count);
        foreach (var robot in ham.Robots)
        {
            WriteRobotInfo(writer, robot);
        }

        writer.Write(ham.RobotJoints.Count);
        foreach (var joint in ham.RobotJoints)
        {
            WriteJointPos(writer, joint);
        }

        writer.Write(ham.Weapons.Count);
        foreach (var weapon in ham.Weapons)
        {
            WriteWeaponInfo(writer, weapon);
        }

        writer.Write(ham.Powerups.Count);
        foreach (var powerup in ham.Powerups)
        {
            WritePowerupInfo(writer, powerup);
        }

        writer.Write(ham.PolygonModels.Count);
        foreach (var model in ham.PolygonModels)
        {
            WritePolyModelHeader(writer, model);
        }

        foreach (var model in ham.PolygonModels)
        {
            writer.Write(model.ModelData);
        }

        foreach (var num in ham.DyingModelNums)
        {
            writer.Write(num);
        }
        foreach (var num in ham.DeadModelNums)
        {
            writer.Write(num);
        }

        writer.Write(ham.GaugesLores.Count);
        foreach (var idx in ham.GaugesLores)
        {
            writer.Write(idx);
        }
        foreach (var idx in ham.GaugesHires)
        {
            writer.Write(idx);
        }

        writer.Write(ham.ObjBitmaps.Count);
        foreach (var idx in ham.ObjBitmaps)
        {
            writer.Write(idx);
        }
        foreach (var ptr in ham.ObjBitmapPtrs)
        {
            writer.Write(ptr);
        }

        WritePlayerShip(writer, ham.PlayerShip);

        writer.Write(ham.CockpitBitmaps.Count);
        foreach (var idx in ham.CockpitBitmaps)
        {
            writer.Write(idx);
        }

        writer.Write(ham.FirstMultiBitmapNum);

        writer.Write(ham.Reactors.Count);
        foreach (var reactor in ham.Reactors)
        {
            WriteReactor(writer, reactor);
        }

        writer.Write(ham.MarkerModelNum);

        foreach (var idx in ham.GameBitmapXlat)
        {
            writer.Write(idx);
        }

        return stream.ToArray();
    }

    #region Read Methods

    private TmapInfo ReadTmapInfo(BinaryReader reader)
    {
        return new TmapInfo
        {
            Flags = reader.ReadByte(),
            Pad = reader.ReadBytes(3),
            Lighting = reader.ReadInt32(),
            Damage = reader.ReadInt32(),
            EClipNum = reader.ReadInt16(),
            DestroyedBitmap = reader.ReadInt16(),
            SlideU = reader.ReadInt16(),
            SlideV = reader.ReadInt16()
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

    private WClip ReadWClip(BinaryReader reader)
    {
        var wclip = new WClip
        {
            PlayTime = reader.ReadInt32(),
            NumFrames = reader.ReadInt16()
        };

        for (int i = 0; i < WClip.MaxFrames; i++)
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

    private RobotInfo ReadRobotInfo(BinaryReader reader)
    {
        var robot = new RobotInfo
        {
            ModelNum = reader.ReadInt32()
        };

        for (int i = 0; i < RobotInfo.MaxGuns; i++)
        {
            robot.GunPoints[i] = ReadVector(reader);
        }

        robot.GunSubmodels = reader.ReadBytes(RobotInfo.MaxGuns);

        robot.Exp1VClipNum = reader.ReadInt16();
        robot.Exp1SoundNum = reader.ReadInt16();
        robot.Exp2VClipNum = reader.ReadInt16();
        robot.Exp2SoundNum = reader.ReadInt16();

        robot.WeaponType = reader.ReadSByte();
        robot.WeaponType2 = reader.ReadSByte();
        robot.NumGuns = reader.ReadSByte();
        robot.ContainsId = reader.ReadSByte();

        robot.ContainsCount = reader.ReadSByte();
        robot.ContainsProb = reader.ReadSByte();
        robot.ContainsType = reader.ReadSByte();
        robot.Kamikaze = reader.ReadSByte();

        robot.ScoreValue = reader.ReadInt16();
        robot.Badass = reader.ReadSByte();
        robot.EnergyDrain = reader.ReadSByte();

        robot.Lighting = reader.ReadInt32();
        robot.Strength = reader.ReadInt32();

        robot.Mass = reader.ReadInt32();
        robot.Drag = reader.ReadInt32();

        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            robot.FieldOfView[i] = reader.ReadInt32();
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            robot.FiringWait[i] = reader.ReadInt32();
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            robot.FiringWait2[i] = reader.ReadInt32();
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            robot.TurnTime[i] = reader.ReadInt32();
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            robot.MaxSpeed[i] = reader.ReadInt32();
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            robot.CircleDistance[i] = reader.ReadInt32();

        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            robot.RapidfireCount[i] = reader.ReadSByte();
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            robot.EvadeSpeed[i] = reader.ReadSByte();

        robot.CloakType = reader.ReadSByte();
        robot.AttackType = reader.ReadSByte();

        robot.SeeSound = reader.ReadByte();
        robot.AttackSound = reader.ReadByte();
        robot.ClawSound = reader.ReadByte();
        robot.TauntSound = reader.ReadByte();

        robot.BossFlag = reader.ReadSByte();
        robot.Companion = reader.ReadSByte();
        robot.SmartBlobs = reader.ReadSByte();
        robot.EnergyBlobs = reader.ReadSByte();

        robot.Thief = reader.ReadSByte();
        robot.Pursuit = reader.ReadSByte();
        robot.Lightcast = reader.ReadSByte();
        robot.DeathRoll = reader.ReadSByte();

        robot.Flags = reader.ReadByte();
        robot.Pad = reader.ReadBytes(3);

        robot.DeathrollSound = reader.ReadByte();
        robot.Glow = reader.ReadByte();
        robot.Behavior = reader.ReadByte();
        robot.Aim = reader.ReadByte();

        for (int gun = 0; gun < RobotInfo.MaxGuns + 1; gun++)
        {
            for (int state = 0; state < RobotInfo.NumAnimStates; state++)
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
            ReadAngvec(reader)
        );
    }

    private WeaponInfo ReadWeaponInfo(BinaryReader reader)
    {
        var weapon = new WeaponInfo
        {
            RenderType = reader.ReadSByte(),
            Persistent = reader.ReadSByte(),
            ModelNum = reader.ReadInt16(),
            ModelNumInner = reader.ReadInt16(),
            FlashVClip = reader.ReadSByte(),
            RobotHitVClip = reader.ReadSByte(),
            FlashSound = reader.ReadInt16(),
            WallHitVClip = reader.ReadSByte(),
            FireCount = reader.ReadSByte(),
            RobotHitSound = reader.ReadInt16(),
            AmmoUsage = reader.ReadSByte(),
            WeaponVClip = reader.ReadSByte(),
            WallHitSound = reader.ReadInt16(),
            Destroyable = reader.ReadSByte(),
            Matter = reader.ReadSByte(),
            Bounce = reader.ReadSByte(),
            HomingFlag = reader.ReadSByte(),
            SpeedVar = reader.ReadByte(),
            Flags = reader.ReadByte(),
            Flash = reader.ReadSByte(),
            AfterburnerSize = reader.ReadSByte(),
            Children = reader.ReadSByte()
        };

        weapon.EnergyUsage = reader.ReadInt32();
        weapon.FireWait = reader.ReadInt32();
        weapon.MultiDamageScale = reader.ReadInt32();
        weapon.BitmapIndex = reader.ReadUInt16();
        weapon.BlobSize = reader.ReadInt32();
        weapon.FlashSize = reader.ReadInt32();
        weapon.ImpactSize = reader.ReadInt32();

        for (int i = 0; i < WeaponInfo.NumDifficultyLevels; i++)
            weapon.Strength[i] = reader.ReadInt32();
        for (int i = 0; i < WeaponInfo.NumDifficultyLevels; i++)
            weapon.Speed[i] = reader.ReadInt32();

        weapon.Mass = reader.ReadInt32();
        weapon.Drag = reader.ReadInt32();
        weapon.Thrust = reader.ReadInt32();
        weapon.PoLenToWidthRatio = reader.ReadInt32();
        weapon.Light = reader.ReadInt32();
        weapon.Lifetime = reader.ReadInt32();
        weapon.DamageRadius = reader.ReadInt32();
        weapon.Picture = reader.ReadUInt16();
        weapon.HiresPicture = reader.ReadUInt16();

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

    private Reactor ReadReactor(BinaryReader reader)
    {
        var reactor = new Reactor
        {
            ModelNum = reader.ReadInt32(),
            NumGuns = reader.ReadInt32()
        };

        for (int i = 0; i < Reactor.MaxGuns; i++)
        {
            reactor.GunPoints[i] = ReadVector(reader);
        }
        for (int i = 0; i < Reactor.MaxGuns; i++)
        {
            reactor.GunDirs[i] = ReadVector(reader);
        }

        return reactor;
    }

    private VmsVector ReadVector(BinaryReader reader)
    {
        return new VmsVector(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );
    }

    private VmsAngvec ReadAngvec(BinaryReader reader)
    {
        return new VmsAngvec(
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16()
        );
    }

    #endregion

    #region Write Methods

    private void WriteTmapInfo(BinaryWriter writer, TmapInfo tmap)
    {
        writer.Write(tmap.Flags);
        writer.Write(tmap.Pad);
        writer.Write(tmap.Lighting);
        writer.Write(tmap.Damage);
        writer.Write(tmap.EClipNum);
        writer.Write(tmap.DestroyedBitmap);
        writer.Write(tmap.SlideU);
        writer.Write(tmap.SlideV);
    }

    private void WriteVClip(BinaryWriter writer, VClip vclip)
    {
        writer.Write(vclip.PlayTime);
        writer.Write(vclip.NumFrames);
        writer.Write(vclip.FrameTime);
        writer.Write(vclip.Flags);
        writer.Write(vclip.SoundNum);

        for (int i = 0; i < VClip.MaxFrames; i++)
        {
            writer.Write(vclip.Frames[i]);
        }

        writer.Write(vclip.LightValue);
    }

    private void WriteEClip(BinaryWriter writer, EClip eclip)
    {
        WriteVClip(writer, eclip.Vc);
        writer.Write(eclip.TimeLeft);
        writer.Write(eclip.FrameCount);
        writer.Write(eclip.ChangingWallTexture);
        writer.Write(eclip.ChangingObjectTexture);
        writer.Write(eclip.Flags);
        writer.Write(eclip.CritClip);
        writer.Write(eclip.DestBmNum);
        writer.Write(eclip.DestVClip);
        writer.Write(eclip.DestEClip);
        writer.Write(eclip.DestSize);
        writer.Write(eclip.SoundNum);
        writer.Write(eclip.SegNum);
        writer.Write(eclip.SideNum);
    }

    private void WriteWClip(BinaryWriter writer, WClip wclip)
    {
        writer.Write(wclip.PlayTime);
        writer.Write(wclip.NumFrames);

        for (int i = 0; i < WClip.MaxFrames; i++)
        {
            writer.Write(wclip.Frames[i]);
        }

        writer.Write(wclip.OpenSound);
        writer.Write(wclip.CloseSound);
        writer.Write(wclip.Flags);

        var filenameBytes = new byte[13];
        var nameBytes = System.Text.Encoding.ASCII.GetBytes(wclip.Filename);
        Array.Copy(nameBytes, filenameBytes, Math.Min(nameBytes.Length, 13));
        writer.Write(filenameBytes);
        writer.Write((byte)0); // pad
    }

    private void WriteRobotInfo(BinaryWriter writer, RobotInfo robot)
    {
        writer.Write(robot.ModelNum);

        for (int i = 0; i < RobotInfo.MaxGuns; i++)
        {
            WriteVector(writer, robot.GunPoints[i]);
        }

        writer.Write(robot.GunSubmodels);

        writer.Write(robot.Exp1VClipNum);
        writer.Write(robot.Exp1SoundNum);
        writer.Write(robot.Exp2VClipNum);
        writer.Write(robot.Exp2SoundNum);

        writer.Write(robot.WeaponType);
        writer.Write(robot.WeaponType2);
        writer.Write(robot.NumGuns);
        writer.Write(robot.ContainsId);

        writer.Write(robot.ContainsCount);
        writer.Write(robot.ContainsProb);
        writer.Write(robot.ContainsType);
        writer.Write(robot.Kamikaze);

        writer.Write(robot.ScoreValue);
        writer.Write(robot.Badass);
        writer.Write(robot.EnergyDrain);

        writer.Write(robot.Lighting);
        writer.Write(robot.Strength);

        writer.Write(robot.Mass);
        writer.Write(robot.Drag);

        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            writer.Write(robot.FieldOfView[i]);
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            writer.Write(robot.FiringWait[i]);
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            writer.Write(robot.FiringWait2[i]);
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            writer.Write(robot.TurnTime[i]);
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            writer.Write(robot.MaxSpeed[i]);
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            writer.Write(robot.CircleDistance[i]);

        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            writer.Write(robot.RapidfireCount[i]);
        for (int i = 0; i < RobotInfo.NumDifficultyLevels; i++)
            writer.Write(robot.EvadeSpeed[i]);

        writer.Write(robot.CloakType);
        writer.Write(robot.AttackType);

        writer.Write(robot.SeeSound);
        writer.Write(robot.AttackSound);
        writer.Write(robot.ClawSound);
        writer.Write(robot.TauntSound);

        writer.Write(robot.BossFlag);
        writer.Write(robot.Companion);
        writer.Write(robot.SmartBlobs);
        writer.Write(robot.EnergyBlobs);

        writer.Write(robot.Thief);
        writer.Write(robot.Pursuit);
        writer.Write(robot.Lightcast);
        writer.Write(robot.DeathRoll);

        writer.Write(robot.Flags);
        writer.Write(robot.Pad);

        writer.Write(robot.DeathrollSound);
        writer.Write(robot.Glow);
        writer.Write(robot.Behavior);
        writer.Write(robot.Aim);

        for (int gun = 0; gun < RobotInfo.MaxGuns + 1; gun++)
        {
            for (int state = 0; state < RobotInfo.NumAnimStates; state++)
            {
                writer.Write(robot.AnimStates[gun, state].NumJoints);
                writer.Write(robot.AnimStates[gun, state].Offset);
            }
        }

        writer.Write(robot.Always0xABCD);
    }

    private void WriteJointPos(BinaryWriter writer, JointPos joint)
    {
        writer.Write(joint.JointNum);
        WriteAngvec(writer, joint.Angles);
    }

    private void WriteWeaponInfo(BinaryWriter writer, WeaponInfo weapon)
    {
        writer.Write(weapon.RenderType);
        writer.Write(weapon.Persistent);
        writer.Write(weapon.ModelNum);
        writer.Write(weapon.ModelNumInner);
        writer.Write(weapon.FlashVClip);
        writer.Write(weapon.RobotHitVClip);
        writer.Write(weapon.FlashSound);
        writer.Write(weapon.WallHitVClip);
        writer.Write(weapon.FireCount);
        writer.Write(weapon.RobotHitSound);
        writer.Write(weapon.AmmoUsage);
        writer.Write(weapon.WeaponVClip);
        writer.Write(weapon.WallHitSound);
        writer.Write(weapon.Destroyable);
        writer.Write(weapon.Matter);
        writer.Write(weapon.Bounce);
        writer.Write(weapon.HomingFlag);
        writer.Write(weapon.SpeedVar);
        writer.Write(weapon.Flags);
        writer.Write(weapon.Flash);
        writer.Write(weapon.AfterburnerSize);
        writer.Write(weapon.Children);

        writer.Write(weapon.EnergyUsage);
        writer.Write(weapon.FireWait);
        writer.Write(weapon.MultiDamageScale);
        writer.Write(weapon.BitmapIndex);
        writer.Write(weapon.BlobSize);
        writer.Write(weapon.FlashSize);
        writer.Write(weapon.ImpactSize);

        for (int i = 0; i < WeaponInfo.NumDifficultyLevels; i++)
            writer.Write(weapon.Strength[i]);
        for (int i = 0; i < WeaponInfo.NumDifficultyLevels; i++)
            writer.Write(weapon.Speed[i]);

        writer.Write(weapon.Mass);
        writer.Write(weapon.Drag);
        writer.Write(weapon.Thrust);
        writer.Write(weapon.PoLenToWidthRatio);
        writer.Write(weapon.Light);
        writer.Write(weapon.Lifetime);
        writer.Write(weapon.DamageRadius);
        writer.Write(weapon.Picture);
        writer.Write(weapon.HiresPicture);
    }

    private void WritePowerupInfo(BinaryWriter writer, PowerupInfo powerup)
    {
        writer.Write(powerup.VClipNum);
        writer.Write(powerup.HitSound);
        writer.Write(powerup.Size);
        writer.Write(powerup.Light);
    }

    private void WritePolyModelHeader(BinaryWriter writer, PolyModel model)
    {
        writer.Write(model.NumModels);
        writer.Write(model.ModelDataSize);
        writer.Write(0); // model_data pointer placeholder

        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            writer.Write(model.SubmodelPtrs[i]);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            WriteVector(writer, model.SubmodelOffsets[i]);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            WriteVector(writer, model.SubmodelNorms[i]);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            WriteVector(writer, model.SubmodelPnts[i]);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            writer.Write(model.SubmodelRads[i]);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            writer.Write(model.SubmodelParents[i]);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            WriteVector(writer, model.SubmodelMins[i]);
        for (int i = 0; i < PolyModel.MaxSubmodels; i++)
            WriteVector(writer, model.SubmodelMaxs[i]);

        WriteVector(writer, model.Mins);
        WriteVector(writer, model.Maxs);
        writer.Write(model.Rad);
        writer.Write(model.NumTextures);
        writer.Write(model.FirstTexture);
        writer.Write(model.SimplerModel);
    }

    private void WritePlayerShip(BinaryWriter writer, PlayerShip ship)
    {
        writer.Write(ship.ModelNum);
        writer.Write(ship.ExplVClipNum);
        writer.Write(ship.Mass);
        writer.Write(ship.Drag);
        writer.Write(ship.MaxThrust);
        writer.Write(ship.ReverseThrust);
        writer.Write(ship.Brakes);
        writer.Write(ship.Wiggle);
        writer.Write(ship.MaxRotThrust);

        for (int i = 0; i < PlayerShip.NumPlayerGuns; i++)
        {
            WriteVector(writer, ship.GunPoints[i]);
        }
    }

    private void WriteReactor(BinaryWriter writer, Reactor reactor)
    {
        writer.Write(reactor.ModelNum);
        writer.Write(reactor.NumGuns);

        for (int i = 0; i < Reactor.MaxGuns; i++)
        {
            WriteVector(writer, reactor.GunPoints[i]);
        }
        for (int i = 0; i < Reactor.MaxGuns; i++)
        {
            WriteVector(writer, reactor.GunDirs[i]);
        }
    }

    private void WriteVector(BinaryWriter writer, VmsVector vector)
    {
        writer.Write(vector.X);
        writer.Write(vector.Y);
        writer.Write(vector.Z);
    }

    private void WriteAngvec(BinaryWriter writer, VmsAngvec angvec)
    {
        writer.Write(angvec.P);
        writer.Write(angvec.B);
        writer.Write(angvec.H);
    }

    #endregion
}