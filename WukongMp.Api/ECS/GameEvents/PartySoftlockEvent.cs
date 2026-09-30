using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, AlwaysPropagates]
internal readonly partial struct PartySoftlockEvent(Entity entity, int birthPointId) 
    : IEquatable<PartySoftlockEvent>
{
    public readonly Entity Entity = entity;
    public readonly int BirthPointId = birthPointId;

    public bool Equals(PartySoftlockEvent other)
        => BirthPointId == other.BirthPointId;

    public override bool Equals(object? obj)
        => obj is PartySoftlockEvent other && Equals(other);

    public override int GetHashCode()
        => BirthPointId;
}