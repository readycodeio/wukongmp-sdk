using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct PartySoftlockEvent(Entity entity, int birthPointId) 
    : IEquatable<PartySoftlockEvent>, IAlwaysPropagates
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