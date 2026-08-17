using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct UnitSpawnedEvent(Entity entity, PlayerId playerId) : IEquatable<UnitSpawnedEvent>, IAlwaysPropagates
{
    public readonly Entity Entity = entity;
    public readonly PlayerId PlayerId = playerId;

    public bool Equals(UnitSpawnedEvent other)
        => Entity == other.Entity && PlayerId.Equals(other.PlayerId);

    public override bool Equals(object? obj)
        => obj is UnitSpawnedEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Entity.GetHashCode() * 397) ^ PlayerId.GetHashCode();
        }
    }
}