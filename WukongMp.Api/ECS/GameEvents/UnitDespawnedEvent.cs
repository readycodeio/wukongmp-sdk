using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct UnitDespawnedEvent(
    Entity entity,
    PlayerId playerId) : IEquatable<UnitDespawnedEvent>, IAlwaysPropagates
{
    public readonly Entity Entity = entity;
    public readonly PlayerId PlayerId = playerId;

    public bool Equals(UnitDespawnedEvent other)
        => Entity == other.Entity && PlayerId == other.PlayerId;

    public override bool Equals(object? obj)
        => obj is UnitDespawnedEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Entity.GetHashCode() * 397) ^ PlayerId.GetHashCode();
        }
    }
}