using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using ReadyM.Relay.Client;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct MontageCallbackData(NetworkIdComponent netId, bool compressed, string montagePath, float position, bool reset) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public bool Compressed = compressed;
    public string MontagePath = montagePath;
    public float Position = position;
    public bool Reset = reset;
}
