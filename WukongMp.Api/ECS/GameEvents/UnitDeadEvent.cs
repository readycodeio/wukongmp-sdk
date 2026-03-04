using System;
using b1;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct UnitDeadEvent(
    Entity entity,
    EDeadReason deadReason,
    int dmgId,
    int stiffLevel,
    bool isDotDmg,
    EAbnormalStateType abnormalType) : IEquatable<UnitDeadEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly EDeadReason DeadReason = deadReason;
    public readonly int DmgId = dmgId;
    public readonly int StiffLevel = stiffLevel;
    public readonly bool IsDotDmg = isDotDmg;
    public readonly EAbnormalStateType AbnormalType = abnormalType;

    public bool Equals(UnitDeadEvent other)
        => (
            Entity == other.Entity &&
            DeadReason == other.DeadReason && 
            DmgId == other.DmgId && 
            StiffLevel == other.StiffLevel && 
            IsDotDmg == other.IsDotDmg && 
            AbnormalType == other.AbnormalType
        );

    public override bool Equals(object? obj)
        => obj is UnitDeadEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)DeadReason;
            hashCode = (hashCode * 397) ^ DmgId;
            hashCode = (hashCode * 397) ^ StiffLevel;
            hashCode = (hashCode * 397) ^ IsDotDmg.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)AbnormalType;
            return hashCode;
        }
    }
}