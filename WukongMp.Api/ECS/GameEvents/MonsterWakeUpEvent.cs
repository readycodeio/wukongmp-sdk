using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, OwnershipBased(nameof(Entity))]
internal readonly partial struct MonsterWakeUpEvent(Entity entity)
    : IEquatable<MonsterWakeUpEvent>
{
    public readonly Entity Entity = entity;

    public bool Equals(MonsterWakeUpEvent other)
        => Entity == other.Entity;

    public override bool Equals(object? obj)
        => obj is MonsterWakeUpEvent other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
}