using System;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Entity))]
internal readonly partial struct RemoveAllBuffsEvent(
    Entity entity,
    EBuffEffectTriggerType triggerType,
    bool withTriggerRemoveEffect) : IEquatable<RemoveAllBuffsEvent>
{
    public readonly Entity Entity = entity;
    public readonly EBuffEffectTriggerType TriggerType = triggerType;
    public readonly bool WithTriggerRemoveEffect = withTriggerRemoveEffect;

    public bool Equals(RemoveAllBuffsEvent other)
        => Entity == other.Entity && TriggerType == other.TriggerType && WithTriggerRemoveEffect == other.WithTriggerRemoveEffect;

    public override bool Equals(object? obj)
        => obj is RemoveAllBuffsEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)TriggerType;
            hashCode = (hashCode * 397) ^ WithTriggerRemoveEffect.GetHashCode();
            return hashCode;
        }
    }
}