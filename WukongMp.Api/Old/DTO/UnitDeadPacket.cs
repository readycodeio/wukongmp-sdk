using b1;
using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.Old.DTO;

public struct UnitDeadPacket(NetworkIdComponent netId, EDeadReason deadReason, int dmgId, int stiffLevel, bool isDotDmg, EAbnormalStateType abnormalType) : INetSerializable
{
    public NetworkIdComponent NetworkId = netId;
    public EDeadReason DeadReason = deadReason;
    public int DmgId = dmgId;
    public int StiffLevel = stiffLevel;
    public bool IsDotDmg = isDotDmg;
    public EAbnormalStateType AbnormalType = abnormalType;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkId);
        writer.Put((byte)DeadReason);
        writer.Put(DmgId);
        writer.Put(StiffLevel);
        writer.Put(IsDotDmg);
        writer.Put((byte)AbnormalType);
    }

    void INetSerializable.Deserialize(NetDataReader reader)
    {
        NetworkId = reader.GetNetworkId();
        DeadReason = (EDeadReason)reader.GetByte();
        DmgId = reader.GetInt();
        StiffLevel = reader.GetInt();
        IsDotDmg = reader.GetBool();
        AbnormalType = (EAbnormalStateType)reader.GetByte();
    }
}