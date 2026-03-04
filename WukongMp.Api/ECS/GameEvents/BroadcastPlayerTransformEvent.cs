using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.Mapping;
using UnrealEngine.Runtime;

namespace WukongMp.Api.ECS.GameEvents;

// NOTE(api): Only manually called
// FIXME(api): Rename to RequestTeleport or something similar
// FIXME(api): Make it work with tamers as well, not just main character
public readonly struct BroadcastPlayerTransformEvent(
    Entity entity, 
    FVector location, 
    FRotator rotation) : IEquatable<BroadcastPlayerTransformEvent>, IAlwaysPropagates
{
    public readonly Entity Entity = entity;
    public readonly FVector Location = location;
    public readonly FRotator Rotation = rotation;

    public bool Equals(BroadcastPlayerTransformEvent other)
        => Entity == other.Entity && Location == other.Location && Rotation == other.Rotation;

    public override bool Equals(object? obj)
        => obj is BroadcastPlayerTransformEvent other && Equals(other);

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
}
