using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct TeleportFinishEvent(Entity entity) 
    : IEquatable<TeleportFinishEvent>, IAlwaysPropagates
{
    public readonly Entity Entity = entity;

    public bool Equals(TeleportFinishEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is TeleportFinishEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}