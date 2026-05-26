using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct ProjectileTargetEvent(
    Entity character,
    string projectileName,
    Entity target,
    string socketName) : IEquatable<ProjectileTargetEvent>, IOwnershipManaged
{
    public readonly Entity Character = character;
    public readonly string ProjectileName = projectileName;
    public readonly Entity Target = target;
    public readonly string SocketName = socketName;

    public bool Equals(ProjectileTargetEvent other)
        => (
            Character == other.Character && 
            ProjectileName == other.ProjectileName && 
            Target == other.Target && 
            SocketName == other.SocketName
        );

    public override bool Equals(object? obj)
        => obj is ProjectileTargetEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Character.GetHashCode();
            hashCode = (hashCode * 397) ^ ProjectileName.GetHashCode();
            hashCode = (hashCode * 397) ^ Target.GetHashCode();
            hashCode = (hashCode * 397) ^ SocketName.GetHashCode();
            return hashCode;
        }
    }
}