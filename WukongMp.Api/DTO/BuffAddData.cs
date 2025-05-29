using BtlShare;
using ReadyM.Api.Multiplayer;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct BuffAddData(int buffId, float duration)
{
    public int BuffId = buffId;
    public float Duration = duration;
}

[DeriveINetSerializable]
public partial struct BuffRemoveData(int buffId, EBuffEffectTriggerType triggerType, int layer, bool withTriggerRemoveEffect)
{
    public int BuffId = buffId;
    public EBuffEffectTriggerType TriggerType = triggerType;
    public int Layer = layer;
    public bool WithTriggerRemoveEffect = withTriggerRemoveEffect;
}

[DeriveINetSerializable]
public partial struct BuffRemoveAllData(EBuffEffectTriggerType triggerType, bool withTriggerRemoveEffect)
{
    public EBuffEffectTriggerType TriggerType = triggerType;
    public bool WithTriggerRemoveEffect = withTriggerRemoveEffect;
}