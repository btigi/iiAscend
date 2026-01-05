using ii.Ascend.Model;

namespace ii.Ascend;

public class BnkProcessor
{
    private const string ExpectedSignature = "ADLIB-";

    public BnkFile Read(string filename)
    {
        var fileData = File.ReadAllBytes(filename);
        return Read(fileData);
    }

    public BnkFile Read(byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var reader = new BinaryReader(stream);

        // Read header
        var versionMinor = reader.ReadByte();
        var versionMajor = reader.ReadByte();
        var signatureBytes = reader.ReadBytes(6);
        var signature = System.Text.Encoding.ASCII.GetString(signatureBytes);
        
        if (signature != ExpectedSignature)
        {
            throw new InvalidDataException($"Invalid BNK file. Expected '{ExpectedSignature}' signature, got '{signature}'.");
        }

        var numRecordsTotal = reader.ReadUInt16();
        var numRecordsUsed = reader.ReadUInt16();
        var offsetNameSection = reader.ReadUInt32();
        var offsetDataSection = reader.ReadUInt32();
        var unused = reader.ReadBytes(8); // 8 zero bytes

        var nameRecords = new List<BnkNameRecord>();
        reader.BaseStream.Position = offsetNameSection;
        
        for (var i = 0; i < numRecordsTotal; i++)
        {
            var indexIntoDataSection = reader.ReadUInt16();
            var used = reader.ReadByte();
            var nameBytes = reader.ReadBytes(9);
            var instrumentName = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];
            
            nameRecords.Add(new BnkNameRecord
            {
                IndexIntoDataSection = indexIntoDataSection,
                Used = used,
                InstrumentName = instrumentName
            });
        }

        reader.BaseStream.Position = offsetDataSection;
        var instruments = new List<BnkInstrumentData>();
        
        for (var i = 0; i < numRecordsUsed; i++)
        {
            var instrumentType = reader.ReadByte();
            var voiceNumber = reader.ReadByte();
            
            var op0 = ReadOperator(reader);
            var op1 = ReadOperator(reader);
            var op0Waveform = reader.ReadByte();
            var op1Waveform = reader.ReadByte();
            
            instruments.Add(new BnkInstrumentData
            {
                InstrumentType = instrumentType,
                VoiceNumber = voiceNumber,
                Operator0 = op0,
                Operator1 = op1,
                Operator0Waveform = op0Waveform,
                Operator1Waveform = op1Waveform
            });
        }

        return new BnkFile
        {
            VersionMinor = versionMinor,
            VersionMajor = versionMajor,
            NumRecordsTotal = numRecordsTotal,
            NumRecordsUsed = numRecordsUsed,
            NameRecords = nameRecords,
            Instruments = instruments
        };
    }

    public void Write(string filename, BnkFile bnkFile)
    {
        var fileData = Write(bnkFile);
        File.WriteAllBytes(filename, fileData);
    }

    public byte[] Write(BnkFile bnkFile)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        const int headerSize = 28;
        const int nameRecordSize = 12;

        var offsetNameSection = headerSize;
        var offsetDataSection = offsetNameSection + (bnkFile.NumRecordsTotal * nameRecordSize);

        writer.Write(bnkFile.VersionMinor);
        writer.Write(bnkFile.VersionMajor);
        writer.Write(System.Text.Encoding.ASCII.GetBytes(ExpectedSignature));
        writer.Write(bnkFile.NumRecordsTotal);
        writer.Write(bnkFile.NumRecordsUsed);
        writer.Write(offsetNameSection);
        writer.Write(offsetDataSection);
        writer.Write(new byte[8]); // 8 zero bytes

        writer.BaseStream.Position = offsetNameSection;
        foreach (var nameRecord in bnkFile.NameRecords)
        {
            writer.Write(nameRecord.IndexIntoDataSection);
            writer.Write(nameRecord.Used);
            var nameBytes = new byte[9];
            var nameAscii = System.Text.Encoding.ASCII.GetBytes(nameRecord.InstrumentName);
            Array.Copy(nameAscii, nameBytes, Math.Min(8, nameAscii.Length));
            writer.Write(nameBytes);
        }

        writer.BaseStream.Position = offsetDataSection;
        foreach (var instrument in bnkFile.Instruments)
        {
            writer.Write(instrument.InstrumentType);
            writer.Write(instrument.VoiceNumber);
            WriteOperator(writer, instrument.Operator0);
            WriteOperator(writer, instrument.Operator1);
            writer.Write(instrument.Operator0Waveform);
            writer.Write(instrument.Operator1Waveform);
        }

        return stream.ToArray();
    }

    private BnkOperatorParameters ReadOperator(BinaryReader reader)
    {
        return new BnkOperatorParameters
        {
            KeyScaleLevel = reader.ReadByte(),
            FrequencyMultiplier = reader.ReadByte(),
            FeedBack = reader.ReadByte(),
            AttackRate = reader.ReadByte(),
            SustainLevel = reader.ReadByte(),
            SustainingSound = reader.ReadByte(),
            DecayRate = reader.ReadByte(),
            OutputLevel = reader.ReadByte(),
            AmplitudeVibrato = reader.ReadByte(),
            FrequencyVibrato = reader.ReadByte(),
            EnvelopeScaling = reader.ReadByte(),
            Type = reader.ReadByte()
        };
    }

    private void WriteOperator(BinaryWriter writer, BnkOperatorParameters op)
    {
        writer.Write(op.KeyScaleLevel);
        writer.Write(op.FrequencyMultiplier);
        writer.Write(op.FeedBack);
        writer.Write(op.AttackRate);
        writer.Write(op.SustainLevel);
        writer.Write(op.SustainingSound);
        writer.Write(op.DecayRate);
        writer.Write(op.OutputLevel);
        writer.Write(op.AmplitudeVibrato);
        writer.Write(op.FrequencyVibrato);
        writer.Write(op.EnvelopeScaling);
        writer.Write(op.Type);
    }
}