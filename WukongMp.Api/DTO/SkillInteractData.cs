using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct SkillInteractData(NetworkId interactiveId, int skillId) : INetSerializable
{
    public NetworkId InteractiveId = interactiveId;
    public int SkillId = skillId;
}
