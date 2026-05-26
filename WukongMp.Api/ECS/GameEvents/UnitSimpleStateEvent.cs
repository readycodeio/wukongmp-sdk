using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct UnitSimpleStateEvent(
    Entity entity,
    EBGUSimpleState simpleState,
    bool isRemove) : IEquatable<UnitSimpleStateEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly EBGUSimpleState SimpleState = simpleState;
    public readonly bool IsRemove = isRemove;

    public bool Equals(UnitSimpleStateEvent other)
        => Entity == other.Entity && SimpleState == other.SimpleState && IsRemove == other.IsRemove;

    public override bool Equals(object? obj)
        => obj is UnitSimpleStateEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)SimpleState;
            hashCode = (hashCode * 397) ^ IsRemove.GetHashCode();
            return hashCode;
        }
    }
}