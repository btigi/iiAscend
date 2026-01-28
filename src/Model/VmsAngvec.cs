namespace ii.Ascend.Model;

public struct VmsAngvec
{
    // Pitch (rotation around X axis)
    public short P { get; set; }
    
    // Bank (rotation around Z axis)
    public short B { get; set; }
    
    // Yaw (rotation around Y axis)
    public short H { get; set; }

    public VmsAngvec(short p, short b, short h)
    {
        P = p;
        B = b;
        H = h;
    }

    public (float Pitch, float Bank, float Heading) ToDegrees()
    {
        const float scale = 360.0f / 65536.0f;
        return (P * scale, B * scale, H * scale);
    }

    public override string ToString() => $"(P:{P}, B:{B}, H:{H})";
}