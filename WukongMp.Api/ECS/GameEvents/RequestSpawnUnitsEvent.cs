using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using UnrealEngine.Runtime;
using WukongMp.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct RequestSpawnUnitsEvent(
    Entity requester,
    string unitName, 
    int count, 
    int teamId, 
    FVector location) : IEquatable<RequestSpawnUnitsEvent>, IRunOnMasterClientOnly
{
    public readonly Entity Requester = requester;
    public readonly string UnitName = unitName;
    public readonly int Count = count;
    public readonly int TeamId = teamId;
    public readonly FVector Location = location;

    public bool Equals(RequestSpawnUnitsEvent other)
        => Requester == other.Requester && UnitName == other.UnitName && Count == other.Count && TeamId == other.TeamId && Location == other.Location;

    public override bool Equals(object? obj)
        => obj is RequestSpawnUnitsEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Requester.GetHashCode();
            hashCode = (hashCode * 397) ^ (UnitName != null ? UnitName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Count;
            hashCode = (hashCode * 397) ^ TeamId;
            hashCode = (hashCode * 397) ^ Location.GetHashCode();
            return hashCode;
        }
    }
}
