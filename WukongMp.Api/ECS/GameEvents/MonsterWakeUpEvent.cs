using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct MonsterWakeUpEvent(Entity entity)
    : IEquatable<MonsterWakeUpEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;

    public bool Equals(MonsterWakeUpEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is MonsterWakeUpEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}