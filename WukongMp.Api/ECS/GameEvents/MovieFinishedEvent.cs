using System;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

// NOTE(api): This is propagated to the server-side only
public readonly struct MovieFinishedEvent(
    int sequenceId,
    AreaId areaId) : IEquatable<MovieFinishedEvent>, IAlwaysPropagatesToEcsOnly
{
    public readonly int SequenceId = sequenceId;
    public readonly AreaId AreaId = areaId;

    public bool Equals(MovieFinishedEvent other)
        => SequenceId == other.SequenceId && AreaId.Equals(other.AreaId);

    public override bool Equals(object? obj)
        => obj is MovieFinishedEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (SequenceId * 397) ^ AreaId.GetHashCode();
        }
    }
}