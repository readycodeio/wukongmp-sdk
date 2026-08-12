using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct ResetMagicallyChangeEvent(
    Entity entity,
    EResetReason_MagicallyChange reason) : IEquatable<ResetMagicallyChangeEvent>, IOwnershipBased
{
    public readonly Entity Entity = entity;
    public readonly EResetReason_MagicallyChange Reason = reason;

    public bool Equals(ResetMagicallyChangeEvent other)
        => Entity == other.Entity && Reason == other.Reason;

    public override bool Equals(object? obj)
        => obj is ResetMagicallyChangeEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Entity.GetHashCode() * 397) ^ (int)Reason;
        }
    }
}