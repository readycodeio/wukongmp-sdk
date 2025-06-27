using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using ReadyM.Relay.Client;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct FsmStateData(NetworkIdComponent netId, string fsmStateName) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public string FsmStateName = fsmStateName;
}
