using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct SkillInteractData(NetworkIdComponent interactiveId, int skillId) : INetSerializable
{
    public NetworkIdComponent InteractiveId = interactiveId;
    public int SkillId = skillId;
}
