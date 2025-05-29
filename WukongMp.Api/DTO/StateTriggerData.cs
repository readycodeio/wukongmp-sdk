using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct StateTriggerData(NetworkIdComponent netId, EBUStateTrigger trigger, float time, bool needForceUpdate) : INetSerializable
{
    public NetworkIdComponent NetId { get; } = netId;
    public EBUStateTrigger Trigger { get; } = trigger;
    public float Time { get; } = time;
    public bool NeedForceUpdate { get; } = needForceUpdate;
}