using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct UnitStateTriggerEvent(
    Entity entity,
    EBUStateTrigger trigger,
    float time,
    bool needForceUpdate) : IEquatable<UnitStateTriggerEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly EBUStateTrigger Trigger = trigger;
    public readonly float Time = time;
    public readonly bool NeedForceUpdate = needForceUpdate;

    public bool Equals(UnitStateTriggerEvent other)
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        => Entity == other.Entity && Trigger == other.Trigger && Time == other.Time && NeedForceUpdate == other.NeedForceUpdate;

    public override bool Equals(object? obj)
        => obj is UnitStateTriggerEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Trigger;
            hashCode = (hashCode * 397) ^ Time.GetHashCode();
            hashCode = (hashCode * 397) ^ NeedForceUpdate.GetHashCode();
            return hashCode;
        }
    }
}