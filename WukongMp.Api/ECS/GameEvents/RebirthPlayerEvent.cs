using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, AlwaysPropagates]
internal readonly partial struct RebirthPlayerEvent(Entity entity, bool teleport) 
    : IEquatable<RebirthPlayerEvent>
{
    public readonly Entity Entity = entity;
    public readonly bool Teleport = teleport;

    public bool Equals(RebirthPlayerEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is RebirthPlayerEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}