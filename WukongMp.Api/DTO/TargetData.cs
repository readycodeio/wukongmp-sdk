using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct TargetData(NetworkId character, NetworkId target, bool clearTarget) : INetSerializable
{
    public NetworkId Character = character;
    public NetworkId Target = target;
    public bool ClearTarget = clearTarget;
}