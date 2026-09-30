using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Entity))]
internal readonly partial struct ExitPhantomRushEvent(Entity entity) : IEquatable<ExitPhantomRushEvent>
{
    public readonly Entity Entity = entity;

    public bool Equals(ExitPhantomRushEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is ExitPhantomRushEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}