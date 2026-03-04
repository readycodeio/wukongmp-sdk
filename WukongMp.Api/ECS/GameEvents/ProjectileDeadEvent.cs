using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct ProjectileDeadEvent(
    Entity entity,
    string projectileClassName,
    EBGUBulletDestroyReason reason) : IEquatable<ProjectileDeadEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly string ProjectileClassName = projectileClassName;
    public readonly EBGUBulletDestroyReason Reason = reason;

    public bool Equals(ProjectileDeadEvent other)
        => Entity == other.Entity && ProjectileClassName == other.ProjectileClassName && Reason == other.Reason;

    public override bool Equals(object? obj)
        => obj is ProjectileDeadEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ ProjectileClassName.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Reason;
            return hashCode;
        }
    }
}