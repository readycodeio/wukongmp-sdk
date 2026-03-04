using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct PhantomRushEvent(Entity entity, ESkillDirection direction)
    : IEquatable<PhantomRushEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly ESkillDirection Direction = direction;

    public bool Equals(PhantomRushEvent other)
        => Entity == other.Entity && Direction == other.Direction;

    public override bool Equals(object? obj)
        => obj is PhantomRushEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked { return (Entity.GetHashCode() * 397) ^ (int)Direction; }
    }
}