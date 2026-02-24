using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct PlayerTransBeginData(
    NetworkId netId,
    int unitResId,
    int unitBornSkillId,
    bool enableBlendViewTarget, 
    EPlayerTransBeginType transBeginType) : INetSerializable
{
    public NetworkId NetId = netId;
    public int UnitResId = unitResId;
    public int UnitBornSkillId = unitBornSkillId;
    public bool EnableBlendViewTarget = enableBlendViewTarget;
    public EPlayerTransBeginType TransBeginType = transBeginType;
}
