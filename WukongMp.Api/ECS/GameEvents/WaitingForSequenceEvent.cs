using System;
using ReadyM.Api.Mapping.Events;
using UnrealEngine.Runtime;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, AlwaysPropagates]
internal readonly partial struct WaitingForSequenceEvent(
    int sequenceId,
    FVector sequenceLocation) : IEquatable<WaitingForSequenceEvent>
{
    public readonly int SequenceId = sequenceId;
    public readonly FVector SequenceLocation = sequenceLocation;

    public bool Equals(WaitingForSequenceEvent other)
        => SequenceId == other.SequenceId && SequenceLocation == other.SequenceLocation;

    public override bool Equals(object? obj)
        => obj is WaitingForSequenceEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (SequenceId * 397) ^ SequenceLocation.GetHashCode();
        }
    }
}