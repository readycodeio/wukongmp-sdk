using b1;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct StateTriggerData(NetworkIdComponent netId, EBUStateTrigger trigger, float time, bool needForceUpdate) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public EBUStateTrigger Trigger = trigger;
    public float Time = time;
    public bool NeedForceUpdate = needForceUpdate;
}
