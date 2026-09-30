using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Entity))]
internal readonly partial struct AfterRebirthEvent(Entity entity) : IEquatable<AfterRebirthEvent>
{
    public readonly Entity Entity = entity;

    public bool Equals(AfterRebirthEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is AfterRebirthEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}