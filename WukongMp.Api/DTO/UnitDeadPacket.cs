using b1;
using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct UnitDeadPacket(NetworkId netId, EDeadReason deadReason, int dmgId, int stiffLevel, bool isDotDmg, EAbnormalStateType abnormalType) : INetSerializable
{
    public NetworkId NetworkId = netId;
    public EDeadReason DeadReason = deadReason;
    public int DmgId = dmgId;
    public int StiffLevel = stiffLevel;
    public bool IsDotDmg = isDotDmg;
    public EAbnormalStateType AbnormalType = abnormalType;
}
