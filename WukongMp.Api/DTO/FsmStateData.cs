using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct FsmStateData(NetworkIdComponent netId, string fsmStateName) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public string FsmStateName = fsmStateName;
}
