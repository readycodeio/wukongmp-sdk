using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct TriggerFsmStateEvent(
    Entity entity,
    string fsmStateName) : IEquatable<TriggerFsmStateEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly string FsmStateName = fsmStateName;

    public bool Equals(TriggerFsmStateEvent other)
        => Entity == other.Entity && FsmStateName == other.FsmStateName;

    public override bool Equals(object? obj)
        => obj is TriggerFsmStateEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Entity.GetHashCode() * 397) ^ FsmStateName.GetHashCode();
        }
    }
}