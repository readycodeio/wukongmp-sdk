using b1;
using LiteNetLib.Utils;

namespace WukongMp.Api.Old.DTO;

public readonly struct PlayerTransBeginData(int unitResId, int unitBornSkillId, bool enbleBlendViewTarget, EPlayerTransBeginType transBeginType)
{
    public readonly int UnitResId = unitResId;
    public readonly int UnitBornSkillId = unitBornSkillId;
    public readonly bool EnableBlendViewTarget = enbleBlendViewTarget;
    public readonly EPlayerTransBeginType TransBeginType = transBeginType;

    public static void Serialize(NetDataWriter outStream, object customObject)
    {
        var data = (PlayerTransBeginData)customObject;
        outStream.Put(data.UnitResId);
        outStream.Put(data.UnitBornSkillId);
        outStream.Put(data.EnableBlendViewTarget);
        outStream.Put((byte)data.TransBeginType);
    }

    public static object Deserialize(NetDataReader inStream)
    {
        var unitResId = inStream.GetInt();
        var unitBornSkillId = inStream.GetInt();
        var enbleBlendViewTarget = inStream.GetBool();
        var transBeginType = (EPlayerTransBeginType)inStream.GetByte();
        return new PlayerTransBeginData(unitResId, unitBornSkillId, enbleBlendViewTarget, transBeginType);
    }
}
