using b1;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct PlayerTransBeginData(int unitResId, int unitBornSkillId, bool enbleBlendViewTarget, EPlayerTransBeginType transBeginType) : INetSerializable
{
    public int UnitResId = unitResId;
    public int UnitBornSkillId = unitBornSkillId;
    public bool EnableBlendViewTarget = enbleBlendViewTarget;
    public EPlayerTransBeginType TransBeginType = transBeginType;
}
