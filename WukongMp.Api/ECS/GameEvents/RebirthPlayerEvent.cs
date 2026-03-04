using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct RebirthPlayerEvent(Entity entity, bool teleport) 
    : IEquatable<RebirthPlayerEvent>, IAlwaysPropagates
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