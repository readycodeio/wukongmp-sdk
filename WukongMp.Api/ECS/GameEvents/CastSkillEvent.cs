using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct CastSkillEvent(
    Entity entity,
    int skillId,
    ECastSkillSourceType skillType) : IEquatable<CastSkillEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly int SkillId = skillId;
    public readonly ECastSkillSourceType SkillType = skillType;

    public bool Equals(CastSkillEvent other)
        => Entity == other.Entity && SkillId == other.SkillId && SkillType == other.SkillType;

    public override bool Equals(object? obj)
        => obj is CastSkillEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ SkillId;
            hashCode = (hashCode * 397) ^ (int)SkillType;
            return hashCode;
        }
    }
}