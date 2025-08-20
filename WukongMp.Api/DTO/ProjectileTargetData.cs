using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct ProjectileTargetData(string projectileName, NetworkId target, string socketName) : INetSerializable
{
    public string ProjectileName = projectileName;
    public NetworkId Target = target;
    public string SocketName = socketName;
}
