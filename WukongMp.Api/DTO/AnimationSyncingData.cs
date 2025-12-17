using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct AnimationSyncingData(NetworkId host, NetworkId guest, bool compressed, string montage) : INetSerializable
{
    public NetworkId Host = host;
    public NetworkId Guest = guest;
    public bool Compressed = compressed;
    public string Montage = montage;
}