using b1;
using ReadyM.Api.Multiplayer;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct PlayMovieData(int sequenceID, bool disablePlayerControl, bool disableMovementInput, bool disableLookAtInput, bool hidePlayer, bool hideHud, string overlapBoxGuid, ESequenceBlendInMatchPositionType matchType)
{
    public readonly int SequenceId = sequenceID;
    public readonly bool DisablePlayerControl = disablePlayerControl;
    public readonly bool DisableMovementInput = disableMovementInput;
    public readonly bool DisableLookAtInput = disableLookAtInput;
    public readonly bool HidePlayer = hidePlayer;
    public readonly bool HideHud = hideHud;
    public readonly string OverlapBoxGuid = overlapBoxGuid;
    public readonly ESequenceBlendInMatchPositionType MatchType = matchType;
}