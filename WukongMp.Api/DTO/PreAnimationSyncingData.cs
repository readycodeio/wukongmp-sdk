using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct PreAnimationSyncingData(NetworkId host, NetworkId guest) : INetSerializable
{
    public NetworkId Host = host;
    public NetworkId Guest = guest;
}