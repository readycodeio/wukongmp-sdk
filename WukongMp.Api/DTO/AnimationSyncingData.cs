using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct AnimationSyncingData(NetworkId hostNetId, NetworkId guestNetId, bool compressed, string montage) : INetSerializable
{
    public NetworkId HostNetId = hostNetId;
    public NetworkId GuestNetId = guestNetId;
    public bool Compressed = compressed;
    public string Montage = montage;
}