using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Mapping;
using UnrealEngine.Runtime;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct BroadcastUnitSpawnEvent(
    Entity entity,
    string? unitName,
    string guid,
    FVector location) : IEquatable<BroadcastUnitSpawnEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly string? UnitName = unitName;
    public readonly string Guid = guid;
    public readonly FVector Location = location;

    public bool Equals(BroadcastUnitSpawnEvent other)
        => (
            Entity == other.Entity && 
            UnitName == other.UnitName && 
            Guid == other.Guid && 
            Location == other.Location
        );

    public override bool Equals(object? obj)
        => obj is BroadcastUnitSpawnEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = UnitName?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ Guid.GetHashCode();
            hashCode = (hashCode * 397) ^ Location.GetHashCode();
            return hashCode;
        }
    }
}