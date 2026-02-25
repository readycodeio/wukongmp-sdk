using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct TamerSkillInteractData(
    NetworkId netId,
    int skillId) : INetSerializable
{
    public NetworkId NetId = netId;
    public int SkillId = skillId;
}
