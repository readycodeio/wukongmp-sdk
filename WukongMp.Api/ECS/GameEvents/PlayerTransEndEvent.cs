using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct PlayerTransEndEvent(
    Entity entity,
    int unitResId,
    int unitBornSkillId,
    bool enableBlendViewTarget,
    EPlayerTransEndType transEndType) : IEquatable<PlayerTransEndEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly int UnitResId = unitResId;
    public readonly int UnitBornSkillId = unitBornSkillId;
    public readonly bool EnableBlendViewTarget = enableBlendViewTarget;
    public readonly EPlayerTransEndType TransEndType = transEndType;

    public bool Equals(PlayerTransEndEvent other)
        => (
            Entity == other.Entity &&
            UnitResId == other.UnitResId &&
            UnitBornSkillId == other.UnitBornSkillId &&
            EnableBlendViewTarget == other.EnableBlendViewTarget &&
            TransEndType == other.TransEndType
        );

    public override bool Equals(object? obj)
        => obj is PlayerTransEndEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ UnitResId;
            hashCode = (hashCode * 397) ^ UnitBornSkillId;
            hashCode = (hashCode * 397) ^ EnableBlendViewTarget.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)TransEndType;
            return hashCode;
        }
    }
}