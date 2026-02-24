using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct MontageCancelEvent(Entity entity) : IEquatable<MontageCancelEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;

    public bool Equals(MontageCancelEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is MontageCancelEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}