using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, AlwaysPropagates]
internal readonly partial struct TeleportFinishEvent(Entity entity) 
    : IEquatable<TeleportFinishEvent>
{
    public readonly Entity Entity = entity;

    public bool Equals(TeleportFinishEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is TeleportFinishEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}