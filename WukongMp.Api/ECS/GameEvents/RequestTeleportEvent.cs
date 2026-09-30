using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;
using UnrealEngine.Runtime;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, AlwaysPropagates]
internal readonly partial struct RequestTeleportEvent(
    Entity entity,
    FVector location,
    FRotator rotation
) : IEquatable<RequestTeleportEvent>
{
    public readonly Entity Entity = entity;
    public readonly FVector Location = location;
    public readonly FRotator Rotation = rotation;

    public bool Equals(RequestTeleportEvent other)
        => Entity == other.Entity && Location == other.Location && Rotation == other.Rotation;

    public override bool Equals(object? obj)
    {
        return obj is RequestTeleportEvent @event && Equals(@event);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ Location.GetHashCode();
            hashCode = (hashCode * 397) ^ Rotation.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(RequestTeleportEvent left, RequestTeleportEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RequestTeleportEvent left, RequestTeleportEvent right)
    {
        return !left.Equals(right);
    }
}