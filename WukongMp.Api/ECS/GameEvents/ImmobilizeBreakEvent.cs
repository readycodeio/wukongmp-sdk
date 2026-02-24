using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct ImmobilizeBreakEvent(Entity entity) 
    : IEquatable<ImmobilizeBreakEvent>, IMasterClientManaged
{
    public readonly Entity Entity = entity;

    public bool Equals(ImmobilizeBreakEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is ImmobilizeBreakEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}