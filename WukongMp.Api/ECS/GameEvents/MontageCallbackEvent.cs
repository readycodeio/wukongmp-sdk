using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct MontageCallbackEvent(
    Entity entity,
    string fullMontagePath,
    float position,
    bool reset) : IEquatable<MontageCallbackEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly string FullMontagePath = fullMontagePath;
    public readonly float Position = position;
    public readonly bool Reset = reset;

    public bool Equals(MontageCallbackEvent other)
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        => Entity == other.Entity && FullMontagePath == other.FullMontagePath && Position == other.Position && Reset == other.Reset;

    public override bool Equals(object? obj)
    {
        return obj is MontageCallbackEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ FullMontagePath.GetHashCode();
            hashCode = (hashCode * 397) ^ Position.GetHashCode();
            hashCode = (hashCode * 397) ^ Reset.GetHashCode();
            return hashCode;
        }
    }
}