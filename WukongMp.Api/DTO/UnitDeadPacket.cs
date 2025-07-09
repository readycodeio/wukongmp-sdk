using b1;
using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct UnitDeadPacket(NetworkIdComponent netId, EDeadReason deadReason, int dmgId, int stiffLevel, bool isDotDmg, EAbnormalStateType abnormalType) : INetSerializable
{
    public NetworkIdComponent NetworkId = netId;
    public EDeadReason DeadReason = deadReason;
    public int DmgId = dmgId;
    public int StiffLevel = stiffLevel;
    public bool IsDotDmg = isDotDmg;
    public EAbnormalStateType AbnormalType = abnormalType;
}
