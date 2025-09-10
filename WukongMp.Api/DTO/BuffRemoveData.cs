using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BuffRemoveData(NetworkId id, int buffId, EBuffEffectTriggerType triggerType, int layer, bool withTriggerRemoveEffect) : INetSerializable
{
    public NetworkId Id = id;
    public int BuffId = buffId;
    public EBuffEffectTriggerType TriggerType = triggerType;
    public int Layer = layer;
    public bool WithTriggerRemoveEffect = withTriggerRemoveEffect;
}