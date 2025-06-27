using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using ReadyM.Relay.Client;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct TargetData(NetworkIdComponent character, NetworkIdComponent target, bool clearTarget) : INetSerializable
{
    public NetworkIdComponent Character = character;
    public NetworkIdComponent Target = target;
    public bool ClearTarget = clearTarget;
}
