using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct StopBaneEffectEvent(Entity entity, EAbnormalStateType stateType)
    : IEquatable<StopBaneEffectEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly EAbnormalStateType StateType = stateType;

    public bool Equals(StopBaneEffectEvent other)
        => Entity.Equals(other.Entity) && StateType == other.StateType;

    public override bool Equals(object? obj)
        => obj is StopBaneEffectEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)StateType;
            return hashCode;
        }
    }
}