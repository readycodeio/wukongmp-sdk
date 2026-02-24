using System;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct ProjectileMoveModeEvent(
    Entity entity,
    string projectileClassName,
    EBulletOrMagicFieldMoveModeType moveMode) : IEquatable<ProjectileMoveModeEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly string ProjectileClassName = projectileClassName;
    public readonly EBulletOrMagicFieldMoveModeType MoveMode = moveMode;

    public bool Equals(ProjectileMoveModeEvent other)
        => Entity == other.Entity && ProjectileClassName == other.ProjectileClassName && MoveMode == other.MoveMode;

    public override bool Equals(object? obj)
        => obj is ProjectileMoveModeEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ ProjectileClassName.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)MoveMode;
            return hashCode;
        }
    }
}