using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct ProjectileSwitchEvent(
    Entity entity,
    string projectileClassName,
    int bulletSwitchId,
    int switchIdx) : IEquatable<ProjectileSwitchEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly string ProjectileClassName = projectileClassName;
    public readonly int BulletSwitchId = bulletSwitchId;
    public readonly int SwitchIdx = switchIdx;

    public bool Equals(ProjectileSwitchEvent other)
        => Entity == other.Entity && 
           ProjectileClassName == other.ProjectileClassName && 
           BulletSwitchId == other.BulletSwitchId && 
           SwitchIdx == other.SwitchIdx;

    public override bool Equals(object? obj)
        => obj is ProjectileSwitchEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ ProjectileClassName.GetHashCode();
            hashCode = (hashCode * 397) ^ BulletSwitchId;
            hashCode = (hashCode * 397) ^ SwitchIdx;
            return hashCode;
        }
    }
}