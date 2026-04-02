using System;
using b1;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

// NOTE(api): Despite being related to movies, this is a client-RPC related event
internal readonly struct PlayMovieRequestEvent(
    int sequenceId,
    bool disablePlayerControl,
    bool disableMovementInput,
    bool disableLookAtInput,
    bool hidePlayer,
    bool hideHud,
    string overlapBoxGuid,
    ESequenceBlendInMatchPositionType matchType) : IEquatable<PlayMovieRequestEvent>, IAlwaysPropagates
{
    public readonly int SequenceId = sequenceId;
    public readonly bool DisablePlayerControl = disablePlayerControl;
    public readonly bool DisableMovementInput = disableMovementInput;
    public readonly bool DisableLookAtInput = disableLookAtInput;
    public readonly bool HidePlayer = hidePlayer;
    public readonly bool HideHud = hideHud;
    public readonly string OverlapBoxGuid = overlapBoxGuid;
    public readonly ESequenceBlendInMatchPositionType MatchType = matchType;

    public bool Equals(PlayMovieRequestEvent other)
        => (
            SequenceId == other.SequenceId && 
            DisablePlayerControl == other.DisablePlayerControl && 
            DisableMovementInput == other.DisableMovementInput && 
            DisableLookAtInput == other.DisableLookAtInput && 
            HidePlayer == other.HidePlayer && 
            HideHud == other.HideHud && 
            OverlapBoxGuid == other.OverlapBoxGuid && 
            MatchType == other.MatchType
        );

    public override bool Equals(object? obj)
        => obj is PlayMovieRequestEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SequenceId;
            hashCode = (hashCode * 397) ^ DisablePlayerControl.GetHashCode();
            hashCode = (hashCode * 397) ^ DisableMovementInput.GetHashCode();
            hashCode = (hashCode * 397) ^ DisableLookAtInput.GetHashCode();
            hashCode = (hashCode * 397) ^ HidePlayer.GetHashCode();
            hashCode = (hashCode * 397) ^ HideHud.GetHashCode();
            hashCode = (hashCode * 397) ^ OverlapBoxGuid.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)MatchType;
            return hashCode;
        }
    }
}