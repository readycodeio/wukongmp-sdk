using System;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct RemoveBuffEvent(
    Entity entity,
    int buffId,
    EBuffEffectTriggerType triggerType,
    int layer,
    bool withTriggerRemoveEffect) : IEquatable<RemoveBuffEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly int BuffId = buffId;
    public readonly EBuffEffectTriggerType TriggerType = triggerType;
    public readonly int Layer = layer;
    public readonly bool WithTriggerRemoveEffect = withTriggerRemoveEffect;

    public bool Equals(RemoveBuffEvent other)
        => Entity == other.Entity && 
           BuffId == other.BuffId && 
           TriggerType == other.TriggerType && 
           Layer == other.Layer && 
           WithTriggerRemoveEffect == other.WithTriggerRemoveEffect;

    public override bool Equals(object? obj)
        => obj is RemoveBuffEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ BuffId;
            hashCode = (hashCode * 397) ^ (int)TriggerType;
            hashCode = (hashCode * 397) ^ Layer;
            hashCode = (hashCode * 397) ^ WithTriggerRemoveEffect.GetHashCode();
            return hashCode;
        }
    }
}