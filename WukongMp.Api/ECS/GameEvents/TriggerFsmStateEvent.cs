using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Entity))]
internal readonly partial struct TriggerFsmStateEvent(
    Entity entity,
    string fsmStateName) : IEquatable<TriggerFsmStateEvent>
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