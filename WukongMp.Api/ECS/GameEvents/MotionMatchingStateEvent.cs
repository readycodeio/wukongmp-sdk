using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct MotionMatchingStateEvent(Entity entity, EState_MM state)
    : IEquatable<MotionMatchingStateEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly EState_MM State = state;
    
    public bool Equals(MotionMatchingStateEvent other)
        => Entity == other.Entity && State == other.State;

    public override bool Equals(object? obj)
        => obj is MotionMatchingStateEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Entity.GetHashCode() * 397) ^ (int)State;
        }
    }
}