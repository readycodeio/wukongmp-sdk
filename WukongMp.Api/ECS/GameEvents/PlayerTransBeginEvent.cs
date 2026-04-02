using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct PlayerTransBeginEvent(
    Entity entity,
    int unitResId,
    int unitBornSkillId,
    bool enableBlendViewTarget,
    EPlayerTransBeginType transBeginType) : IEquatable<PlayerTransBeginEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly int UnitResId = unitResId;
    public readonly int UnitBornSkillId = unitBornSkillId;
    public readonly bool EnableBlendViewTarget = enableBlendViewTarget;
    public readonly EPlayerTransBeginType TransBeginType = transBeginType;

    public bool Equals(PlayerTransBeginEvent other)
        => (
            Entity == other.Entity &&
            UnitResId == other.UnitResId && 
            UnitBornSkillId == other.UnitBornSkillId && 
            EnableBlendViewTarget == other.EnableBlendViewTarget && 
            TransBeginType == other.TransBeginType
        );

    public override bool Equals(object? obj)
        => obj is PlayerTransBeginEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ UnitResId;
            hashCode = (hashCode * 397) ^ UnitBornSkillId;
            hashCode = (hashCode * 397) ^ EnableBlendViewTarget.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)TransBeginType;
            return hashCode;
        }
    }
}