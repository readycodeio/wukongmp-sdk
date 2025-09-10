using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffRemoveAllData(NetworkId id, EBuffEffectTriggerType triggerType, bool withTriggerRemoveEffect) : INetSerializable
{
    public NetworkId Id = id;
    public EBuffEffectTriggerType TriggerType = triggerType;
    public bool WithTriggerRemoveEffect = withTriggerRemoveEffect;
}