using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct MontageCallbackData(NetworkIdComponent netId, bool compressed, string montagePath, float position, bool reset) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public bool Compressed = compressed;
    public string MontagePath = montagePath;
    public float Position = position;
    public bool Reset = reset;
}