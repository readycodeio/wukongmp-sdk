using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct SetTargetEvent(
    Entity character,
    Entity target,
    bool clearTarget) : IEquatable<SetTargetEvent>, IOwnershipManaged
{
    public readonly Entity Character = character;
    public readonly Entity Target = target;
    public readonly bool ClearTarget = clearTarget;

    public bool Equals(SetTargetEvent other)
        => Character == other.Character && Target == other.Target && ClearTarget == other.ClearTarget;

    public override bool Equals(object? obj)
        => obj is SetTargetEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Character.GetHashCode();
            hashCode = (hashCode * 397) ^ Target.GetHashCode();
            hashCode = (hashCode * 397) ^ ClearTarget.GetHashCode();
            return hashCode;
        }
    }
}