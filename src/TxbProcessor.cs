namespace ii.Ascend;

public class TxbProcessor
{
    public string Read(string filename)
    {
        var fileBytes = File.ReadAllBytes(filename);
        var decryptedBytes = new List<byte>();

        foreach (var currentByte in fileBytes)
        {
            if (currentByte == 0x0A)
            {
                decryptedBytes.Add(0x0D);
                decryptedBytes.Add(0x0A);
            }
            else
            {
                var xored = (byte)(currentByte ^ 0xE9);
                var decrypted = RotateLeft(xored, 2);
                decryptedBytes.Add(decrypted);
            }
        }

        return System.Text.Encoding.UTF8.GetString(decryptedBytes.ToArray());
    }

    private static byte RotateLeft(byte value, int count)
    {
        count = count % 8;
        return (byte)((value << count) | (value >> (8 - count)));
    }
}