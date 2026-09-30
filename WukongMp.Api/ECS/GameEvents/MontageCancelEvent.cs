using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Entity))]
internal readonly partial struct MontageCancelEvent(Entity entity) : IEquatable<MontageCancelEvent>
{
    public readonly Entity Entity = entity;

    public bool Equals(MontageCancelEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is MontageCancelEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}