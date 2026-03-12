using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
internal partial struct MontageCallbackData(
    NetworkId netId, 
    bool compressed,
    string montagePath,
    float position,
    bool reset) : INetSerializable
{
    public NetworkId NetId = netId;
    public bool Compressed = compressed;
    public string MontagePath = montagePath;
    public float Position = position;
    public bool Reset = reset;
}
