using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Mapping;
using UnrealEngine.Runtime;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct StartJumpEvent(
    Entity entity,
    ESkillDirection startJumpDir,
    FVector2D inputVector) : IEquatable<StartJumpEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly ESkillDirection StartJumpDir = startJumpDir;
    public readonly FVector2D InputVector = inputVector;

    public bool Equals(StartJumpEvent other)
        => Entity == other.Entity && StartJumpDir == other.StartJumpDir && InputVector.Equals(other.InputVector);

    public override bool Equals(object? obj)
        => obj is StartJumpEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)StartJumpDir;
            hashCode = (hashCode * 397) ^ InputVector.GetHashCode();
            return hashCode;
        }
    }
}