using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct FsmStateData(NetworkIdComponent netId, string fsmStateName)
{
    public NetworkIdComponent NetId { get; } = netId;
    public string FsmStateName { get; } = fsmStateName;
}