using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffAddData(int buffId, float duration) : INetSerializable
{
    public int BuffId = buffId;
    public float Duration = duration;
}

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffRemoveData(int buffId, EBuffEffectTriggerType triggerType, int layer, bool withTriggerRemoveEffect) : INetSerializable
{
    public int BuffId = buffId;
    public EBuffEffectTriggerType TriggerType = triggerType;
    public int Layer = layer;
    public bool WithTriggerRemoveEffect = withTriggerRemoveEffect;
}

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffRemoveAllData(EBuffEffectTriggerType triggerType, bool withTriggerRemoveEffect) : INetSerializable
{
    public EBuffEffectTriggerType TriggerType = triggerType;
    public bool WithTriggerRemoveEffect = withTriggerRemoveEffect;
}
