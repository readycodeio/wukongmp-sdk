using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, MasterClientManaged]
internal readonly partial struct ImmobilizeBreakEvent(Entity entity) 
    : IEquatable<ImmobilizeBreakEvent>
{
    public readonly Entity Entity = entity;

    public bool Equals(ImmobilizeBreakEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is ImmobilizeBreakEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}