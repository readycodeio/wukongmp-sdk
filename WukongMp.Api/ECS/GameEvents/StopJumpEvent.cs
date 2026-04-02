using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct StopJumpEvent(Entity entity) : IEquatable<StopJumpEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;

    public bool Equals(StopJumpEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is StopJumpEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}