using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct IronBodyStartEvent(Entity entity) : IEquatable<IronBodyStartEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;

    public bool Equals(IronBodyStartEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is IronBodyStartEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}