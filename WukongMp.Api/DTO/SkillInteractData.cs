using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct SkillInteractData(NetworkIdComponent interactiveId, int skillId) : INetSerializable
{
    public NetworkIdComponent InteractiveId = interactiveId;
    public int SkillId = skillId;
}
