using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct ExitPhantomRushEvent(Entity entity) : IEquatable<ExitPhantomRushEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;

    public bool Equals(ExitPhantomRushEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is ExitPhantomRushEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}