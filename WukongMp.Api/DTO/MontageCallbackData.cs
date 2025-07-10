using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

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
