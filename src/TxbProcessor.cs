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

    public void Write(string filename, string content)
    {
        var decryptedBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var encryptedBytes = new List<byte>();

        for (int i = 0; i < decryptedBytes.Length; i++)
        {
            var currentByte = decryptedBytes[i];
            
            if (currentByte == 0x0D && i + 1 < decryptedBytes.Length && decryptedBytes[i + 1] == 0x0A)
            {
                encryptedBytes.Add(0x0A);
                i++;
            }
            else
            {
                var rotated = RotateRight(currentByte, 2);
                var encrypted = (byte)(rotated ^ 0xE9);
                encryptedBytes.Add(encrypted);
            }
        }

        File.WriteAllBytes(filename, encryptedBytes.ToArray());
    }

    private static byte RotateLeft(byte value, int count)
    {
        count = count % 8;
        return (byte)((value << count) | (value >> (8 - count)));
    }

    private static byte RotateRight(byte value, int count)
    {
        count = count % 8;
        return (byte)((value >> count) | (value << (8 - count)));
    }
}