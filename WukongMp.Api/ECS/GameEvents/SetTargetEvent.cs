using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Character))]
internal readonly partial struct SetTargetEvent(
    Entity character,
    Entity target,
    bool clearTarget) : IEquatable<SetTargetEvent>
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