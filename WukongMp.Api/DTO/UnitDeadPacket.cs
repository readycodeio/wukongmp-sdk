using b1;
using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct UnitDeadPacket(NetworkIdComponent netId, EDeadReason deadReason, int dmgId, int stiffLevel, bool isDotDmg, EAbnormalStateType abnormalType)
{
    public NetworkIdComponent NetworkId = netId;
    public EDeadReason DeadReason = deadReason;
    public int DmgId = dmgId;
    public int StiffLevel = stiffLevel;
    public bool IsDotDmg = isDotDmg;
    public EAbnormalStateType AbnormalType = abnormalType;
}