using System;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

// NOTE(api): This is propagated to the server-side only
public readonly struct SkipMovieEvent(
    int sequenceId,
    int waitingPlayers = 0,
    int allPlayers = 0) : IEquatable<SkipMovieEvent>, IAlwaysPropagates
{
    public readonly int SequenceId = sequenceId;
    public readonly int WaitingPlayers = waitingPlayers;
    public readonly int AllPlayers = allPlayers;

    public bool Equals(SkipMovieEvent other)
        => SequenceId == other.SequenceId && WaitingPlayers == other.WaitingPlayers && AllPlayers == other.AllPlayers;

    public override bool Equals(object? obj)
        => obj is SkipMovieEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SequenceId;
            hashCode = (hashCode * 397) ^ WaitingPlayers;
            hashCode = (hashCode * 397) ^ AllPlayers;
            return hashCode;
        }
    }
}