using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Entity))]
internal readonly partial struct StopJumpEvent(Entity entity) : IEquatable<StopJumpEvent>
{
    public readonly Entity Entity = entity;

    public bool Equals(StopJumpEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is StopJumpEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}