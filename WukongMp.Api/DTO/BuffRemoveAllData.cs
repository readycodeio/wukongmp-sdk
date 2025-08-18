using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffRemoveAllData(EBuffEffectTriggerType triggerType, bool withTriggerRemoveEffect) : INetSerializable
{
    public EBuffEffectTriggerType TriggerType = triggerType;
    public bool WithTriggerRemoveEffect = withTriggerRemoveEffect;
}