using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct TriggerMagicallyChangeEvent(
    Entity entity,
    string configPathName,
    int skillId,
    int recoverSkillId,
    int curVigorSkillId,
    ECastReason_MagicallyChange castReason) : IEquatable<TriggerMagicallyChangeEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly string ConfigPathName = configPathName;
    public readonly int SkillId = skillId;
    public readonly int RecoverSkillId = recoverSkillId;
    public readonly int CurVigorSkillId = curVigorSkillId;
    public readonly ECastReason_MagicallyChange CastReason = castReason;

    public bool Equals(TriggerMagicallyChangeEvent other)
        => (
            Entity == other.Entity && 
            ConfigPathName == other.ConfigPathName && 
            SkillId == other.SkillId && 
            RecoverSkillId == other.RecoverSkillId && 
            CurVigorSkillId == other.CurVigorSkillId && 
            CastReason == other.CastReason
        );

    public override bool Equals(object? obj)
        => obj is TriggerMagicallyChangeEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ ConfigPathName.GetHashCode();
            hashCode = (hashCode * 397) ^ SkillId;
            hashCode = (hashCode * 397) ^ RecoverSkillId;
            hashCode = (hashCode * 397) ^ CurVigorSkillId;
            hashCode = (hashCode * 397) ^ (int)CastReason;
            return hashCode;
        }
    }
}