using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Entity))]
internal readonly partial struct IronBodyStartEvent(Entity entity) : IEquatable<IronBodyStartEvent>
{
    public readonly Entity Entity = entity;

    public bool Equals(IronBodyStartEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is IronBodyStartEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}