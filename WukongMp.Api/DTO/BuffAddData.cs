using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffAddData(int buffId, float duration) : INetSerializable
{
    public int BuffId = buffId;
    public float Duration = duration;
}