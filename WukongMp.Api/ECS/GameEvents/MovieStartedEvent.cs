using System;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

// NOTE(api): This is propagated to the server-side only
[DeriveIGameEvent, AlwaysPropagatesToEcsOnly]
internal readonly partial struct MovieStartedEvent(
    int sequenceId,
    AreaId areaId) : IEquatable<MovieStartedEvent>
{
    public readonly int SequenceId = sequenceId;
    public readonly AreaId AreaId = areaId;

    public bool Equals(MovieStartedEvent other)
        => SequenceId == other.SequenceId && AreaId.Equals(other.AreaId);

    public override bool Equals(object? obj)
        => obj is MovieStartedEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (SequenceId * 397) ^ AreaId.GetHashCode();
        }
    }
}