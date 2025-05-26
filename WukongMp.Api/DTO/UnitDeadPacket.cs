using b1;
using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Extensions;

namespace WukongMp.Api.DTO;

public readonly struct UnitDeadPacket(NetworkIdComponent netId, EDeadReason deadReason, int dmgId, int stiffLevel, bool isDotDmg, EAbnormalStateType abnormalType)
{
    public readonly NetworkIdComponent NetworkId = netId;
    public readonly EDeadReason DeadReason = deadReason;
    public readonly int DmgId = dmgId;
    public readonly int StiffLevel = stiffLevel;
    public readonly bool IsDotDmg = isDotDmg;
    public readonly EAbnormalStateType AbnormalType = abnormalType;

    public static void Serialize(NetDataWriter outStream, object customObject)
    {
        var data = (UnitDeadPacket)customObject;
        outStream.Put(data.NetworkId);
        outStream.Put((byte)data.DeadReason);
        outStream.Put(data.DmgId);
        outStream.Put(data.StiffLevel);
        outStream.Put(data.IsDotDmg);
        outStream.Put((byte)data.AbnormalType);
    }

    public static object Deserialize(NetDataReader inStream)
    {
        var networkId = inStream.GetNetworkId();
        var deadReason = (EDeadReason)inStream.GetByte();
        var dmgId = inStream.GetInt();
        var stiffLevel = inStream.GetInt();
        var isDotDmg = inStream.GetBool();
        var abnormalType = (EAbnormalStateType)inStream.GetByte();
        return new UnitDeadPacket(networkId, deadReason, dmgId, stiffLevel, isDotDmg, abnormalType);
    }
}