using System;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct RemoveAllBuffsEvent(
    Entity entity,
    EBuffEffectTriggerType triggerType,
    bool withTriggerRemoveEffect) : IEquatable<RemoveAllBuffsEvent>, IOwnershipManaged
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