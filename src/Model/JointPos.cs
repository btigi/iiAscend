namespace ii.Ascend.Model;

public struct JointPos
{
    public short JointNum { get; set; }
    public VmsAngvec Angles { get; set; }

    public JointPos(short jointNum, VmsAngvec angles)
    {
        JointNum = jointNum;
        Angles = angles;
    }
}

public struct JointList
{
    public short NumJoints { get; set; }
    public short Offset { get; set; }

    public JointList(short numJoints, short offset)
    {
        NumJoints = numJoints;
        Offset = offset;
    }
}