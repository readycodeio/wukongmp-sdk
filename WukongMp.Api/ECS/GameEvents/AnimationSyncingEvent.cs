using System;
using Friflo.Engine.ECS;

namespace WukongMp.Api.ECS.GameEvents;

// NOTE(api): This seems unused. The original implementation never calls SendAnimationSyncing
public readonly struct AnimationSyncingEvent(Entity host, Entity guest, string fullMontagePath)
    : IEquatable<AnimationSyncingEvent>
{
    public readonly Entity Host = host;
    public readonly Entity Guest = guest;
    public readonly string FullMontagePath = fullMontagePath;

    public bool Equals(AnimationSyncingEvent other)
        => Host == other.Host && Guest == other.Guest && FullMontagePath == other.FullMontagePath;

    public override bool Equals(object? obj)
        => obj is AnimationSyncingEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Host.GetHashCode();
            hashCode = (hashCode * 397) ^ Guest.GetHashCode();
            hashCode = (hashCode * 397) ^ FullMontagePath.GetHashCode();
            return hashCode;
        }
    }
}