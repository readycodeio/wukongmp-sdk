using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct AddBuffEvent(Entity entity, int buffId, float duration)
    : IEquatable<AddBuffEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly int BuffId = buffId;
    public readonly float Duration = duration;
    
    public bool Equals(AddBuffEvent other)
        => Entity == other.Entity && BuffId == other.BuffId && Duration.Equals(other.Duration);

    public override bool Equals(object? obj)
        => obj is AddBuffEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ BuffId;
            hashCode = (hashCode * 397) ^ Duration.GetHashCode();
            return hashCode;
        }
    }
}