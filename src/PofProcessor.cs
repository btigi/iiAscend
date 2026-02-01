using ii.Ascend.Model;

namespace ii.Ascend;

public class PofProcessor
{
    private const int PofSignature = 0x4F505350; // 'OPSP' in little-endian ('PSPO' as string)
    private const int MinVersion = 6;
    private const int MaxVersion = 8;

    // Chunk IDs (stored as 4-char codes in little-endian)
    private const int ID_OHDR = 0x5244484F; // 'OHDR' -> 'RDHO'
    private const int ID_SOBJ = 0x4A424F53; // 'SOBJ' -> 'JBOS'
    private const int ID_GUNS = 0x534E5547; // 'GUNS' -> 'SNUG'
    private const int ID_ANIM = 0x4D494E41; // 'ANIM' -> 'MINA'
    private const int ID_IDTA = 0x41544449; // 'IDTA' -> 'ATDI'
    private const int ID_TXTR = 0x52545854; // 'TXTR' -> 'RTXT'

    public PolyModel Read(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        return Read(reader);
    }

    public PolyModel Read(string filename)
    {
        using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);
        return Read(reader);
    }

    public PolyModel Read(BinaryReader reader)
    {
        var model = new PolyModel();

        var signature = reader.ReadInt32();
        if (signature != PofSignature)
        {
            throw new InvalidDataException($"Invalid POF signature. Expected 'PSPO' (0x{PofSignature:X8}), got 0x{signature:X8}");
        }

        var version = reader.ReadInt16();
        if (version < MinVersion || version > MaxVersion)
        {
            throw new InvalidDataException($"Unsupported POF version {version}. Supported versions: {MinVersion}-{MaxVersion}");
        }

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            if (reader.BaseStream.Length - reader.BaseStream.Position < 8)
                break; // Not enough bytes for chunk header

            var chunkId = reader.ReadInt32();
            var chunkLength = reader.ReadInt32();
            var nextChunkPosition = reader.BaseStream.Position + chunkLength;

            switch (chunkId)
            {
                case ID_OHDR:
                    ReadObjectHeader(reader, model);
                    break;

                case ID_SOBJ:
                    ReadSubobjectHeader(reader, model);
                    break;

                case ID_IDTA:
                    ReadInterpreterData(reader, model, chunkLength);
                    break;

                case ID_TXTR:
                    ReadTextureList(reader, model);
                    break;

                case ID_GUNS:
                case ID_ANIM:
                default:
                    // Skip unknown or unhandled chunks by advancing to next chunk position
                    reader.BaseStream.Position = nextChunkPosition;
                    break;
            }

            // Version 8+ requires 4-byte alignment, so ensure we're at the aligned position
            // For version < 8, each handler reads exactly what it needs, or we've already skipped
            if (version >= 8 && reader.BaseStream.Position != nextChunkPosition)
            {
                reader.BaseStream.Position = nextChunkPosition;
            }
        }

        return model;
    }

    private void ReadObjectHeader(BinaryReader reader, PolyModel model)
    {
        model.NumModels = reader.ReadInt32();
        model.Rad = reader.ReadInt32();

        model.Mins = ReadVmsVector(reader);
        model.Maxs = ReadVmsVector(reader);
    }

    private void ReadSubobjectHeader(BinaryReader reader, PolyModel model)
    {
        var submodelIndex = reader.ReadInt16();

        if (submodelIndex < 0 || submodelIndex >= PolyModel.MaxSubmodels)
        {
            throw new InvalidDataException($"Submodel index {submodelIndex} out of range (max {PolyModel.MaxSubmodels})");
        }

        model.SubmodelParents[submodelIndex] = (byte)reader.ReadInt16();
        model.SubmodelNorms[submodelIndex] = ReadVmsVector(reader);
        model.SubmodelPnts[submodelIndex] = ReadVmsVector(reader);
        model.SubmodelOffsets[submodelIndex] = ReadVmsVector(reader);
        model.SubmodelRads[submodelIndex] = reader.ReadInt32();
        model.SubmodelPtrs[submodelIndex] = reader.ReadInt32();
    }

    private void ReadInterpreterData(BinaryReader reader, PolyModel model, int length)
    {
        model.ModelDataSize = length;
        model.ModelData = reader.ReadBytes(length);
    }

    private void ReadTextureList(BinaryReader reader, PolyModel model)
    {
        var numTextures = reader.ReadInt16();
        model.NumTextures = (byte)numTextures;
        model.TextureNames.Clear();

        for (int i = 0; i < numTextures; i++)
        {
            var textureName = ReadNullTerminatedString(reader, 128);
            model.TextureNames.Add(textureName);
        }
    }

    private string ReadNullTerminatedString(BinaryReader reader, int maxLength)
    {
        var bytes = new List<byte>();
        for (int i = 0; i < maxLength; i++)
        {
            var b = reader.ReadByte();
            if (b == 0)
                break;
            bytes.Add(b);
        }
        return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
    }

    private VmsVector ReadVmsVector(BinaryReader reader)
    {
        return new VmsVector(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );
    }

    public byte[] Write(PolyModel model)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(PofSignature);
        writer.Write((short)8);

        writer.Write(ID_OHDR);
        writer.Write(32); // Length: 2 ints + 2 vectors (4 + 4 + 12 + 12)
        writer.Write(model.NumModels);
        writer.Write(model.Rad);
        WriteVmsVector(writer, model.Mins);
        WriteVmsVector(writer, model.Maxs);

        for (int i = 0; i < model.NumModels; i++)
        {
            writer.Write(ID_SOBJ);
            writer.Write(48); // Length: 2 shorts + 3 vectors + 2 ints (4 + 36 + 8)
            writer.Write((short)i);
            writer.Write((short)model.SubmodelParents[i]);
            WriteVmsVector(writer, model.SubmodelNorms[i]);
            WriteVmsVector(writer, model.SubmodelPnts[i]);
            WriteVmsVector(writer, model.SubmodelOffsets[i]);
            writer.Write(model.SubmodelRads[i]);
            writer.Write(model.SubmodelPtrs[i]);
        }

        if (model.TextureNames != null && model.TextureNames.Count > 0)
        {
            // Calculate chunk length: 2 bytes for count + sum of string lengths + null terminators
            int chunkLength = 2; // short for count
            foreach (var name in model.TextureNames)
            {
                chunkLength += Math.Min(name.Length + 1, 128); // string + null terminator, max 128
            }

            writer.Write(ID_TXTR);
            writer.Write(chunkLength);
            writer.Write((short)model.TextureNames.Count);
            
            foreach (var name in model.TextureNames)
            {
                var nameBytes = System.Text.Encoding.ASCII.GetBytes(name);
                var maxLength = Math.Min(nameBytes.Length, 127); // Leave room for null terminator
                writer.Write(nameBytes, 0, maxLength);
                writer.Write((byte)0); // Null terminator
            }
        }

        if (model.ModelData != null && model.ModelData.Length > 0)
        {
            writer.Write(ID_IDTA);
            writer.Write(model.ModelData.Length);
            writer.Write(model.ModelData);
        }

        return stream.ToArray();
    }

    private void WriteVmsVector(BinaryWriter writer, VmsVector vec)
    {
        writer.Write(vec.X);
        writer.Write(vec.Y);
        writer.Write(vec.Z);
    }
}