using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct AfterRebirthEvent(Entity entity) : IEquatable<AfterRebirthEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;

    public bool Equals(AfterRebirthEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is AfterRebirthEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}