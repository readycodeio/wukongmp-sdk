using System;
using b1;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct MagicFieldDeadEvent(
    string className,
    EBGUBulletDestroyReason reason
)
    : IEquatable<MagicFieldDeadEvent>, IAlwaysPropagates
{
    public readonly string ClassName = className;
    public readonly EBGUBulletDestroyReason Reason = reason;

    public bool Equals(MagicFieldDeadEvent other)
        => ClassName == other.ClassName && Reason == other.Reason;

    public override bool Equals(object? obj)
        => obj is MagicFieldDeadEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (ClassName.GetHashCode() * 397) ^ (int)Reason;
        }
    }
}