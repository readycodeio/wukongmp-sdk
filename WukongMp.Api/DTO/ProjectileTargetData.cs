using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
internal partial struct ProjectileTargetData(
    NetworkId characterNetId, 
    string projectileName,
    NetworkId targetNetId, 
    string socketName) : INetSerializable
{
    public NetworkId CharacterNetId = characterNetId;
    public string ProjectileName = projectileName;
    public NetworkId TargetNetId = targetNetId;
    public string SocketName = socketName;
}
