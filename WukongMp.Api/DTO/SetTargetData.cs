using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct SetTargetData(
    NetworkId characterNetId,
    NetworkId targetNetId,
    bool clearTarget) : INetSerializable
{
    public NetworkId CharacterNetId = characterNetId;
    public NetworkId TargetNetId = targetNetId;
    public bool ClearTarget = clearTarget;
}