using System;
using ReadyM.Relay.Common.Mapping;
using UnrealEngine.Runtime;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct RequestSpawnUnitsEvent(
    string unitName, 
    int count, 
    int teamId, 
    FVector location) : IEquatable<RequestSpawnUnitsEvent>, IAlwaysPropagates
{
    public readonly string UnitName = unitName;
    public readonly int Count = count;
    public readonly int TeamId = teamId;
    public readonly FVector Location = location;

    public bool Equals(RequestSpawnUnitsEvent other)
        => UnitName == other.UnitName && Count == other.Count && TeamId == other.TeamId && Location == other.Location;

    public override bool Equals(object? obj)
        => obj is RequestSpawnUnitsEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = UnitName.GetHashCode();
            hashCode = (hashCode * 397) ^ Count;
            hashCode = (hashCode * 397) ^ TeamId;
            hashCode = (hashCode * 397) ^ Location.GetHashCode();
            return hashCode;
        }
    }
}
