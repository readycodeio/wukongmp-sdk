using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffAddData(NetworkId id, int buffId, float duration) : INetSerializable
{
    public NetworkId Id = id;
    public int BuffId = buffId;
    public float Duration = duration;
}