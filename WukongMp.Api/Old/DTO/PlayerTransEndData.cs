using b1;
using LiteNetLib.Utils;

namespace WukongMp.Api.Old.DTO;

public readonly struct PlayerTransEndData(int unitResId, int unitBornSkillId, bool enbleBlendViewTarget, EPlayerTransEndType transEndType)
{
    public readonly int UnitResId = unitResId;
    public readonly int UnitBornSkillId = unitBornSkillId;
    public readonly bool EnableBlendViewTarget = enbleBlendViewTarget;
    public readonly EPlayerTransEndType TransEndType = transEndType;

    public static void Serialize(NetDataWriter outStream, object customObject)
    {
        var data = (PlayerTransEndData)customObject;
        outStream.Put(data.UnitResId);
        outStream.Put(data.UnitBornSkillId);
        outStream.Put(data.EnableBlendViewTarget);
        outStream.Put((byte)data.TransEndType);
    }

    public static object Deserialize(NetDataReader inStream)
    {
        var unitResId = inStream.GetInt();
        var unitBornSkillId = inStream.GetInt();
        var enbleBlendViewTarget = inStream.GetBool();
        var transEndType = (EPlayerTransEndType)inStream.GetByte();
        return new PlayerTransEndData(unitResId, unitBornSkillId, enbleBlendViewTarget, transEndType);
    }
}
