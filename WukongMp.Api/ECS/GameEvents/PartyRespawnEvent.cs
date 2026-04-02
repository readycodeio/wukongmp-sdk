using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct PartyRespawnEvent(
    Entity entity,
    int birthShrineId) : IEquatable<PartyRespawnEvent>, IAlwaysPropagates
{
    public readonly Entity Entity = entity;
    public readonly int BirthShrineId = birthShrineId;

    public bool Equals(PartyRespawnEvent other)
        => Entity == other.Entity && BirthShrineId == other.BirthShrineId;

    public override bool Equals(object? obj)
        => obj is PartyRespawnEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Entity.GetHashCode() * 397) ^ BirthShrineId;
        }
    }
}