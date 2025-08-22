using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct PlayMovieData(int sequenceID, bool disablePlayerControl, bool disableMovementInput, bool disableLookAtInput, bool hidePlayer, bool hideHud, string overlapBoxGuid, ESequenceBlendInMatchPositionType matchType)
    : INetSerializable
{
    public int SequenceId = sequenceID;
    public bool DisablePlayerControl = disablePlayerControl;
    public bool DisableMovementInput = disableMovementInput;
    public bool DisableLookAtInput = disableLookAtInput;
    public bool HidePlayer = hidePlayer;
    public bool HideHud = hideHud;
    public string OverlapBoxGuid = overlapBoxGuid;
    public ESequenceBlendInMatchPositionType MatchType = matchType;
}
