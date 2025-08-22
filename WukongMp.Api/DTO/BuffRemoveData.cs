using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffRemoveData(int buffId, EBuffEffectTriggerType triggerType, int layer, bool withTriggerRemoveEffect) : INetSerializable
{
    public int BuffId = buffId;
    public EBuffEffectTriggerType TriggerType = triggerType;
    public int Layer = layer;
    public bool WithTriggerRemoveEffect = withTriggerRemoveEffect;
}