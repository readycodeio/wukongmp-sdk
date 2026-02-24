using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct TamerSkillInteractEvent(
    Entity entity,
    int skillId) : IEquatable<TamerSkillInteractEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly int SkillId = skillId;

    public bool Equals(TamerSkillInteractEvent other)
        => Entity == other.Entity && SkillId == other.SkillId;

    public override bool Equals(object? obj)
        => obj is TamerSkillInteractEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Entity.GetHashCode() * 397) ^ SkillId;
        }
    }
}