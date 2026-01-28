namespace ii.Ascend.Model;

public struct VmsVector
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public VmsVector(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public (float X, float Y, float Z) ToFloat()
    {
        const float scale = 1.0f / 65536.0f;
        return (X * scale, Y * scale, Z * scale);
    }

    public static VmsVector FromFloat(float x, float y, float z)
    {
        const float scale = 65536.0f;
        return new VmsVector(
            (int)(x * scale),
            (int)(y * scale),
            (int)(z * scale)
        );
    }

    public override string ToString() => $"({X}, {Y}, {Z})";
}