using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct PlayBaneEffectEvent(
    Entity entity,
    EAbnormalStateType stateType,
    EAbnromalDispActionType actionType) : IEquatable<PlayBaneEffectEvent>, IOwnershipBased
{
    public readonly Entity Entity = entity;
    public readonly EAbnormalStateType StateType = stateType;
    public readonly EAbnromalDispActionType ActionType = actionType;

    public bool Equals(PlayBaneEffectEvent other)
        => Entity.Equals(other.Entity) && StateType == other.StateType && ActionType == other.ActionType;

    public override bool Equals(object? obj)
        => obj is PlayBaneEffectEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)StateType;
            hashCode = (hashCode * 397) ^ (int)ActionType;
            return hashCode;
        }
    }
}