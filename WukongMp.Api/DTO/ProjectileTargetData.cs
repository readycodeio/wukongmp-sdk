using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct ProjectileTargetData(string projectileName, NetworkIdComponent target, string socketName) : INetSerializable
{
    public string ProjectileName = projectileName;
    public NetworkIdComponent Target = target;
    public string SocketName = socketName;
}
