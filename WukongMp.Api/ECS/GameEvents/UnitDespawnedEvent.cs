using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, AlwaysPropagates]
internal readonly partial struct UnitDespawnedEvent(
    Entity entity,
    PlayerId playerId) : IEquatable<UnitDespawnedEvent>
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