using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct StateTriggerData(NetworkId netId, EBUStateTrigger trigger, float time, bool needForceUpdate) : INetSerializable
{
    public NetworkId NetId = netId;
    public EBUStateTrigger Trigger = trigger;
    public float Time = time;
    public bool NeedForceUpdate = needForceUpdate;
}
