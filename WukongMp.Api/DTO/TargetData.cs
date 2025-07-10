using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct TargetData(NetworkIdComponent character, NetworkIdComponent target, bool clearTarget) : INetSerializable
{
    public NetworkIdComponent Character = character;
    public NetworkIdComponent Target = target;
    public bool ClearTarget = clearTarget;
}
