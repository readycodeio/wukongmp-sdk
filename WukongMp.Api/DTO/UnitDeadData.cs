using b1;
using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct UnitDeadData(
    NetworkId netId, 
    EDeadReason deadReason, 
    int dmgId, 
    int stiffLevel, 
    bool isDotDmg,
    EAbnormalStateType abnormalType) : INetSerializable
{
    public NetworkId NetId = netId;
    public EDeadReason DeadReason = deadReason;
    public int DmgId = dmgId;
    public int StiffLevel = stiffLevel;
    public bool IsDotDmg = isDotDmg;
    public EAbnormalStateType AbnormalType = abnormalType;
}
