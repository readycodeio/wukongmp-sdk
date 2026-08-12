using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct RestAtShrineEvent(
    Entity entity,
    int rebirthPointId) : IEquatable<RestAtShrineEvent>, IOwnershipBased
{
    public readonly Entity Entity = entity;
    public readonly int RebirthPointId = rebirthPointId;

    public bool Equals(RestAtShrineEvent other)
        => Entity == other.Entity && RebirthPointId == other.RebirthPointId;

    public override bool Equals(object? obj)
        => obj is RestAtShrineEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Entity.GetHashCode() * 397) ^ RebirthPointId;
        }
    }
}